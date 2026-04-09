using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Crypto
{
    /// <summary>
    /// Edit mode tests for MobileWalletAdapterSession.
    /// These cover the session's pure crypto and encoding behavior without Android.
    /// </summary>
    public class MobileWalletAdapterSessionTests
    {
       
        // AssociationToken format
        [Test]
        public void AssociationToken_IsValidBase64Url_NoStandardBase64Characters()
        {
            // Arrange
            var session = new MobileWalletAdapterSession();

            // Act
            string token = session.AssociationToken;

            // Base64Url output should avoid the characters that break URLs.
            Assert.IsNotNull(token, "AssociationToken must not be null");
            Assert.IsNotEmpty(token, "AssociationToken must not be empty");
            Assert.IsFalse(token.Contains('+'),
                "AssociationToken must not contain '+' (standard Base64 char, breaks URI)");
            Assert.IsFalse(token.Contains('/'),
                "AssociationToken must not contain '/' (standard Base64 char, breaks URI)");
            Assert.IsFalse(token.Contains('='),
                "AssociationToken must not contain '=' padding (breaks URI)");
        }

        [Test]
        public void AssociationToken_OnlyContains_ValidBase64UrlCharacters()
        {
            // Arrange
            var session = new MobileWalletAdapterSession();

            // Act
            string token = session.AssociationToken;

            // Only URL-safe Base64 characters should be present.
            var validBase64Url = new Regex(@"^[A-Za-z0-9\-_]+$");
            Assert.IsTrue(validBase64Url.IsMatch(token),
                $"AssociationToken '{token}' contains characters outside Base64Url alphabet");
        }

        [Test]
        public void AssociationToken_IsDerivedFrom_PublicKeyBytes()
        {
            // Arrange
            var session = new MobileWalletAdapterSession();

            // Recreate the token from the raw public key bytes.
            byte[] pubKeyBytes = session.PublicKeyBytes;
            string expected = Convert.ToBase64String(pubKeyBytes)
                .Split('=')[0]
                .Replace('+', '-')
                .Replace('/', '_');

            // Assert
            Assert.AreEqual(expected, session.AssociationToken,
                "AssociationToken must be the Base64Url encoding of PublicKeyBytes");
        }

       
        // Error paths before ECDH is set up
        [Test]
        public void EncryptSessionPayload_ThrowsInvalidOperationException_WhenNoSessionKeyEstablished()
        {
            // Fresh session; no shared key has been negotiated yet.
            var session = new MobileWalletAdapterSession();
            var payload = new byte[] { 0x01, 0x02, 0x03 };

            // The production code logs before it throws, so the test needs to allow that.
            LogAssert.Expect(LogType.Error, "Cannot encrypt, no session key has been established");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                session.EncryptSessionPayload(payload));

            StringAssert.Contains("no session key has been established", ex.Message,
                "Exception message must mention that no session key has been established");
        }

        [Test]
        public void DecryptSessionPayload_ThrowsInvalidOperationException_WhenNoSessionKeyEstablished()
        {
            // Fresh session; no shared key has been negotiated yet.
            var session = new MobileWalletAdapterSession();
            var payload = new byte[64]; // arbitrary non-empty payload

            // The production code logs before it throws, so the test needs to allow that.
            LogAssert.Expect(LogType.Error, "Cannot decrypt, no session key has been established");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                session.DecryptSessionPayload(payload));
        }

       
        // Public key shape
        [Test]
        public void PublicKeyBytes_IsUncompressedEcPoint_65Bytes()
        {
            // Arrange
            var session = new MobileWalletAdapterSession();

            // Act
            byte[] pubKeyBytes = session.PublicKeyBytes;

            // Assert
            Assert.AreEqual(65, pubKeyBytes.Length,
                "PublicKeyBytes must be 65 bytes (uncompressed EC point: 0x04 || X || Y)");
            Assert.AreEqual((byte)0x04, pubKeyBytes[0],
                "First byte of PublicKeyBytes must be 0x04 (uncompressed point marker)");
        }

        [Test]
        public void TwoSessions_HaveDifferent_AssociationTokens()
        {
            // Each session should generate its own keypair.
            var session1 = new MobileWalletAdapterSession();
            var session2 = new MobileWalletAdapterSession();

            // Assert
            Assert.AreNotEqual(session1.AssociationToken, session2.AssociationToken,
                "Two independent sessions must produce different AssociationTokens");
        }
    }
}
