using NUnit.Framework;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.JsonRpc
{
    /// <summary>
    /// Edit mode tests for <see cref="CapabilitiesResult"/> wire format.
    /// The MWA spec dictates the snake_case JSON property names, and these
    /// tests pin them so a rename on the C# side cannot silently break the
    /// deserializer. Every numeric and version field is also nullable so
    /// absence is represented as null, never a default value.
    /// </summary>
    [Category("Lifecycle")]
    public class CapabilitiesResultTests
    {
        
        // Snake_case property names
        [Test]
        public void Deserialize_MaxTransactionsPerRequest_FromSnakeCaseJson()
        {
            // Arrange
            const string json = "{\"max_transactions_per_request\":10}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.MaxTransactionsPerRequest,
                "max_transactions_per_request must deserialize to MaxTransactionsPerRequest");
        }

        [Test]
        public void Deserialize_MaxMessagesPerRequest_FromSnakeCaseJson()
        {
            // Arrange
            const string json = "{\"max_messages_per_request\":5}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.AreEqual(5, result.MaxMessagesPerRequest,
                "max_messages_per_request must deserialize to MaxMessagesPerRequest");
        }

        [Test]
        public void Deserialize_SupportedTransactionVersions_AsStringArray()
        {
            // Arrange
            const string json = "{\"supported_transaction_versions\":[\"legacy\",\"0\"]}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsNotNull(result.SupportedTransactionVersions,
                "SupportedTransactionVersions must not be null when JSON array is present");
            Assert.AreEqual(2, result.SupportedTransactionVersions.Length);
            Assert.AreEqual("legacy", result.SupportedTransactionVersions[0]);
            Assert.AreEqual("0", result.SupportedTransactionVersions[1]);
        }

        [Test]
        public void Deserialize_SupportsCloneAuthorization_True()
        {
            // Arrange
            const string json = "{\"supports_clone_authorization\":true}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsTrue(result.SupportsCloneAuthorization.HasValue);
            Assert.IsTrue(result.SupportsCloneAuthorization.Value,
                "supports_clone_authorization:true must deserialize to true");
        }

        [Test]
        public void Deserialize_SupportsCloneAuthorization_False()
        {
            // Arrange
            const string json = "{\"supports_clone_authorization\":false}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsTrue(result.SupportsCloneAuthorization.HasValue);
            Assert.IsFalse(result.SupportsCloneAuthorization.Value,
                "supports_clone_authorization:false must deserialize to false");
        }

        
        // Absence handling
        [Test]
        public void AllNullableFields_AreNull_WhenAbsentFromJson()
        {
            // Arrange
            // supports_clone_authorization is bool? so absence must stay null,
            // not implicitly coerce to false.
            const string json = "{}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsNotNull(result, "Empty object must still deserialize to a non-null instance");
            Assert.IsNull(result.MaxTransactionsPerRequest,
                "MaxTransactionsPerRequest must be null when absent");
            Assert.IsNull(result.MaxMessagesPerRequest,
                "MaxMessagesPerRequest must be null when absent");
            Assert.IsNull(result.SupportedTransactionVersions,
                "SupportedTransactionVersions must be null when absent");
            Assert.IsNull(result.SupportsCloneAuthorization,
                "SupportsCloneAuthorization is bool? and must be null (not false) when absent");
        }

        [Test]
        public void EmptyJsonObject_Deserializes_WithoutException()
        {
            const string json = "{}";

            Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<CapabilitiesResult>(json),
                "Empty object must not throw during deserialization");
        }

        [Test]
        public void UnknownJsonFields_AreIgnored_NoException()
        {
            // Forward compatibility: the MWA spec may add new fields that the
            // SDK does not yet know about. Deserialization must tolerate them.
            const string json = "{\"max_transactions_per_request\":3," +
                                "\"future_field_not_yet_modeled\":\"unknown\"," +
                                "\"another_unknown\":42}";

            CapabilitiesResult result = null;
            Assert.DoesNotThrow(() => result = JsonConvert.DeserializeObject<CapabilitiesResult>(json),
                "Unknown fields must be silently ignored by the deserializer");
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.MaxTransactionsPerRequest,
                "Known fields must still deserialize when unknown fields are present");
        }

        
        // Full payload round trip
        [Test]
        public void FullPayload_Deserializes_AllFields()
        {
            // Arrange
            const string json = "{" +
                                "\"supports_clone_authorization\":true," +
                                "\"max_transactions_per_request\":12," +
                                "\"max_messages_per_request\":7," +
                                "\"supported_transaction_versions\":[\"legacy\",\"0\"]" +
                                "}";

            // Act
            var result = JsonConvert.DeserializeObject<CapabilitiesResult>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(true, result.SupportsCloneAuthorization);
            Assert.AreEqual(12, result.MaxTransactionsPerRequest);
            Assert.AreEqual(7, result.MaxMessagesPerRequest);
            Assert.IsNotNull(result.SupportedTransactionVersions);
            Assert.AreEqual(2, result.SupportedTransactionVersions.Length);
        }

        
        // Response<CapabilitiesResult> wrapper - wire-level success/failure
        [Test]
        public void InsideResponseWrapper_WasSuccessful_TrueWhenErrorNull()
        {
            // CapabilitiesResult itself has no error flag, but the generic
            // Response<T> envelope does. Pin that Response<CapabilitiesResult>
            // composes correctly so callers can keep using WasSuccessful.
            var response = new Response<CapabilitiesResult>
            {
                JsonRpc = "2.0",
                Id = 1,
                Result = new CapabilitiesResult { MaxTransactionsPerRequest = 4 },
                Error = null
            };

            Assert.IsTrue(response.WasSuccessful,
                "Response<CapabilitiesResult>.WasSuccessful must be true when Error is null");
            Assert.IsFalse(response.Failed);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(4, response.Result.MaxTransactionsPerRequest);
        }

        [Test]
        public void InsideResponseWrapper_Failed_TrueWhenErrorPresent()
        {
            var response = new Response<CapabilitiesResult>
            {
                JsonRpc = "2.0",
                Id = 1,
                Result = null,
                Error = new Response<CapabilitiesResult>.ResponseError
                {
                    Code = -32601,
                    Message = "Method not found"
                }
            };

            Assert.IsTrue(response.Failed,
                "Response<CapabilitiesResult>.Failed must be true when Error is set");
            Assert.IsFalse(response.WasSuccessful);
        }
    }
}
