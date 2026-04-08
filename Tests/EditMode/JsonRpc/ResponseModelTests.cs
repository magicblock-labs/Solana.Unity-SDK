using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.JsonRpc
{
    /// <summary>
    /// Edit mode tests for the simple Response&lt;T&gt; flags and nested error model.
    /// These are small checks, but a regression here would affect every caller.
    /// </summary>
    public class ResponseModelTests
    {
       
        // Success and failure flags
        [Test]
        public void WasSuccessful_IsTrue_WhenError_IsNull()
        {
            // Arrange
            var response = new Response<object>
            {
                JsonRpc = "2.0",
                Id = 1,
                Result = new object(),
                Error = null
            };

            // Assert
            Assert.IsTrue(response.WasSuccessful,
                "WasSuccessful must be true when Error is null");
        }

       
        // Failure flag
        [Test]
        public void Failed_IsTrue_WhenError_IsNotNull()
        {
            // Arrange
            var response = new Response<object>
            {
                JsonRpc = "2.0",
                Id = 1,
                Result = null,
                Error = new Response<object>.ResponseError
                {
                    Code = -32600,
                    Message = "Invalid Request"
                }
            };

            // Assert
            Assert.IsTrue(response.Failed,
                "Failed must be true when Error is not null");
        }

       
        // The flags should never contradict each other
        [Test]
        public void WasSuccessful_And_Failed_AreNeverBothTrue()
        {
            // Success case
            var successResponse = new Response<object> { Error = null };
            // Failure case
            var failResponse = new Response<object>
            {
                Error = new Response<object>.ResponseError { Code = -1, Message = "err" }
            };

            // These flags should be mutually exclusive.
            Assert.IsFalse(successResponse.WasSuccessful && successResponse.Failed,
                "WasSuccessful and Failed must not both be true (success response)");
            Assert.IsFalse(failResponse.WasSuccessful && failResponse.Failed,
                "WasSuccessful and Failed must not both be true (fail response)");
        }

       
        // A few extra sanity checks
        [Test]
        public void WasSuccessful_IsFalse_WhenError_IsNotNull()
        {
            // Arrange
            var response = new Response<object>
            {
                Error = new Response<object>.ResponseError { Code = -32601, Message = "Method not found" }
            };

            // Assert
            Assert.IsFalse(response.WasSuccessful,
                "WasSuccessful must be false when Error is not null");
        }

        [Test]
        public void Failed_IsFalse_WhenError_IsNull()
        {
            // Arrange
            var response = new Response<object> { Error = null };

            // Assert
            Assert.IsFalse(response.Failed,
                "Failed must be false when Error is null");
        }

       
        // ResponseError properties
        [Test]
        public void ResponseError_Properties_AreSetCorrectly()
        {
            // Arrange
            var error = new Response<object>.ResponseError
            {
                Code = -32700,
                Message = "Parse error"
            };

            // Assert
            Assert.AreEqual(-32700, error.Code);
            Assert.AreEqual("Parse error", error.Message);
        }

       
        // Generic behavior
        [Test]
        public void Response_WorksWith_StringResult()
        {
            // Arrange
            var response = new Response<string>
            {
                JsonRpc = "2.0",
                Id = 42,
                Result = "ok",
                Error = null
            };

            // Assert
            Assert.IsTrue(response.WasSuccessful);
            Assert.AreEqual("ok", response.Result);
            Assert.AreEqual(42, response.Id);
        }
    }
}
