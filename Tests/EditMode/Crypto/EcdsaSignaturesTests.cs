using NUnit.Framework;
using System;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Crypto
{
    /// <summary>
    /// Edit mode tests for the EcdsaSignatures helpers.
    /// These only exercise the crypto conversion logic, so they don't need Android.
    /// </summary>
    public class EcdsaSignaturesTests
    {
       
        // Helpers
        private static ECPublicKeyParameters GenerateP256PublicKey()
        {
            var gen = new ECKeyPairGenerator();
            var curve = SecNamedCurves.GetByName("secp256r1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            gen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
            var keyPair = gen.GenerateKeyPair();
            return (ECPublicKeyParameters)keyPair.Public;
        }

        private static byte[] GenerateValidP1363Signature()
        {
            // Use a real signature so the round-trip test is working with realistic input.
            var session = new MobileWalletAdapterSession();
            var helloReq = session.CreateHelloReq();
            // HELLO_REQ is [65-byte public key | 64-byte P1363 signature].
            var p1363 = new byte[64];
            Array.Copy(helloReq, 65, p1363, 0, 64);
            return p1363;
        }

       
        // DER <-> P1363 round-trip
        [Test]
        public void ConvertDerToP1363_ThenBackToDer_RoundTrip_PreservesSignature()
        {
            // Start with a valid P1363 signature.
            byte[] originalP1363 = GenerateValidP1363Signature();

            // Convert to DER and back again.
            byte[] der = EcdsaSignatures.ConvertEcp256SignatureP1363ToDer(originalP1363, 0);
            byte[] roundTrippedP1363 = EcdsaSignatures.ConvertEcp256SignatureDeRtoP1363(der, 0);

            // We should end up with the exact same 64-byte signature.
            Assert.AreEqual(64, roundTrippedP1363.Length,
                "P1363 signature must always be exactly 64 bytes");
            Assert.AreEqual(originalP1363, roundTrippedP1363,
                "Round-tripped P1363 signature must be byte-for-byte identical to the original");
        }

       
        // DER input validation
        [Test]
        public void ConvertDerToP1363_ThrowsArgumentException_WhenBufferTooShort()
        {
            // Too short to even contain the DER prefix.
            var tooShort = new byte[1] { 0x30 };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                EcdsaSignatures.ConvertEcp256SignatureDeRtoP1363(tooShort, 0));

            StringAssert.Contains("too short", ex.Message,
                "Exception message should mention buffer is too short");
        }

        [Test]
        public void ConvertDerToP1363_ThrowsArgumentException_WhenTypeByte_IsWrong()
        {
            // Same shape, but the DER type byte is wrong.
            var wrongType = new byte[] { 0x31, 0x44, 0x02, 0x20 };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                EcdsaSignatures.ConvertEcp256SignatureDeRtoP1363(wrongType, 0));

            StringAssert.Contains("invalid type", ex.Message,
                "Exception message should mention invalid type");
        }

       
        // P-256 public key round-trip
        [Test]
        public void EncodeDecodeP256PublicKey_RoundTrip_PreservesCoordinates()
        {
            // Arrange
            var originalKey = GenerateP256PublicKey();

            // Act
            byte[] encoded = EcdsaSignatures.EncodeP256PublicKey(originalKey);
            ECPublicKeyParameters decoded = EcdsaSignatures.DecodeP256PublicKey(encoded);

            // Uncompressed point format is 0x04 + 32-byte X + 32-byte Y.
            Assert.AreEqual(65, encoded.Length,
                "Encoded public key must be 65 bytes (uncompressed point format)");
            Assert.AreEqual((byte)0x04, encoded[0],
                "First byte must be 0x04 for uncompressed EC point");

            var originalX = originalKey.Q.AffineXCoord.ToBigInteger();
            var originalY = originalKey.Q.AffineYCoord.ToBigInteger();
            var decodedX = decoded.Q.AffineXCoord.ToBigInteger();
            var decodedY = decoded.Q.AffineYCoord.ToBigInteger();

            Assert.AreEqual(originalX, decodedX, "X coordinate must survive encode/decode");
            Assert.AreEqual(originalY, decodedY, "Y coordinate must survive encode/decode");
        }

        [Test]
        public void DecodeP256PublicKey_ThrowsArgumentException_WhenInputTooShort()
        {
            // Definitely shorter than a valid uncompressed key.
            var tooShort = new byte[10];
            tooShort[0] = 0x04;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                EcdsaSignatures.DecodeP256PublicKey(tooShort));
        }

        [Test]
        public void DecodeP256PublicKey_ThrowsArgumentException_WhenPrefixByte_IsWrong()
        {
            // Right length, wrong point prefix.
            var wrongPrefix = new byte[65];
            wrongPrefix[0] = 0x02;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                EcdsaSignatures.DecodeP256PublicKey(wrongPrefix));
        }
    }
}
