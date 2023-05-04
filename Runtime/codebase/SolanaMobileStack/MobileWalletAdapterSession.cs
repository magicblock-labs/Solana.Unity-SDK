using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class MobileWalletAdapterSession
{
    public readonly AsymmetricCipherKeyPair KeyPair;
    public ECPublicKeyParameters PublicKey => (ECPublicKeyParameters)KeyPair.Public;
    public ECPrivateKeyParameters PrivateKey => (ECPrivateKeyParameters)KeyPair.Private;

    public byte[] PublicKeyBytes => EcdsaSignatures.EncodeP256PublicKey(PublicKey);
    public byte[] PrivateKeyBytes => PrivateKey.D.ToByteArray();
    public string AssociationToken => Base64UrlEncode(PublicKeyBytes);

    private AsymmetricCipherKeyPair _privateEphemeralKey;
    private byte[] _encryptionKey;
    private int _mSeqNumberTx = 0;
    private int _mSeqNumberRx = 0;

    // CONSTANTS 
    private const int AesIvLengthBytes = 12;
    private const int AesTagLengthBytes = 16;
    private const int SeqNumLengthBytes = 4;

    public MobileWalletAdapterSession()
    {
        // Create a new EC keypair generator
        var gen = new ECKeyPairGenerator();

        // Use the P-256 curve
        var curve = SecNamedCurves.GetByName("secp256r1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        gen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));

        // Generate the keypair
        KeyPair = gen.GenerateKeyPair();
    }

    public byte[] CreateHelloReq()
    {
        // Generate the ephemeral P-256 EC keypair
        var gen = new ECKeyPairGenerator();
        var curve = SecNamedCurves.GetByName("secp256r1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        gen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
        if(_privateEphemeralKey == null)
            _privateEphemeralKey = gen.GenerateKeyPair();
        return BuildHelloReq(KeyPair, (ECPublicKeyParameters)_privateEphemeralKey.Public);
    }

    private static byte[] BuildHelloReq(AsymmetricCipherKeyPair associationKey, ECPublicKeyParameters ourPublicKey) {
        byte[] ourPublicKeyEncoded = EcdsaSignatures.EncodeP256PublicKey(ourPublicKey);
        byte[] sig;
        try
        {
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, associationKey.Private);
            signer.BlockUpdate(ourPublicKeyEncoded, 0, ourPublicKeyEncoded.Length);
            sig = signer.GenerateSignature();
        } catch (CryptoException e) {
            throw new InvalidOperationException("Failed signing HELLO_REQ public key payload", e);
        }

        byte[] p1363Sig;
        try {
            p1363Sig = EcdsaSignatures.ConvertEcp256SignatureDeRtoP1363(sig, 0);
        } catch (ArgumentException e) {
            throw new InvalidOperationException("Error converting DER ECDSA signature to P1363", e);
        }

        var message = new byte[ourPublicKeyEncoded.Length + p1363Sig.Length];
        Array.Copy(ourPublicKeyEncoded, message, ourPublicKeyEncoded.Length);
        Array.Copy(p1363Sig, 0, message, ourPublicKeyEncoded.Length, p1363Sig.Length);
        return message;
    }

    private static ECPublicKeyParameters ParseHelloRsp(byte[] message) 
    {
        ECPublicKeyParameters otherPublicKey;
        try 
        {
            otherPublicKey = EcdsaSignatures.DecodeP256PublicKey(message);
        } 
        catch (Exception e) 
        {
            throw new Exception("Failed creating EC public key from HELLO_RSP", e);
        }

        return otherPublicKey;
    }

    public byte[] EncryptSessionPayload(byte[] payload)
    {
        if (_encryptionKey == null)
        {
            const string e = "Cannot encrypt, no session key has been established";
            Debug.LogError(e);
            throw new InvalidOperationException(e);
        }
        _mSeqNumberTx++;
        var seqNum = BitConverter.GetBytes(_mSeqNumberTx).Reverse().ToArray();

        try
        {
            var iv = new byte[AesIvLengthBytes];
            new SecureRandom().NextBytes(iv);

            var keyParam = new KeyParameter(_encryptionKey);

            var cipher = CipherUtilities.GetCipher("AES/GCM/NoPadding");
            var parameters = new AeadParameters(
                keyParam, AesTagLengthBytes * 8, iv, seqNum
            );
            cipher.Init(true, parameters);

            var cipherText = new byte[cipher.GetOutputSize(payload.Length)];
            var len = cipher.ProcessBytes(payload, 0, payload.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);

            using var combinedStream = new MemoryStream();
            using (var binaryWriter = new BinaryWriter(combinedStream))
            {
                binaryWriter.Write(seqNum);
                binaryWriter.Write(iv);
                binaryWriter.Write(cipherText);
            }
            return combinedStream.ToArray();
        }
        catch (InvalidCipherTextException e)
        {
            Debug.LogError("Error encrypting session payload" + e.Message);
            throw new InvalidOperationException("Error encrypting session payload", e);
        }
    }

    public byte[] DecryptSessionPayload(byte[] payload)
    {
        if (_encryptionKey == null)
        {
            const string e = "Cannot encrypt, no session key has been established";
            Debug.LogError(e);
            throw new InvalidOperationException(e);
        }

        var seqNumByte = new ArraySegment<byte>(payload, 0, SeqNumLengthBytes).Reverse().ToArray();
        var seqNum = BitConverter.ToUInt32(seqNumByte, 0);

        if (seqNum != _mSeqNumberRx + 1)
        {
            const string e = "Encrypted messages has invalid sequence number";
            Debug.LogError(e);
            throw new InvalidOperationException(e);
        }
        _mSeqNumberRx = (int)seqNum;
    
        try
        {
            var keyParam = new KeyParameter(_encryptionKey);
            var iv = new ArraySegment<byte>( payload, SeqNumLengthBytes, AesIvLengthBytes).ToArray();
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(keyParam, AesTagLengthBytes * 8, iv, associatedText: seqNumByte);
            cipher.Init(false, parameters);
            var toDecipher = new ArraySegment<byte>( payload, SeqNumLengthBytes + AesIvLengthBytes, payload.Length - SeqNumLengthBytes - AesIvLengthBytes).ToArray();
            var decipherText = new byte[cipher.GetOutputSize(toDecipher.Length)];
            int len = cipher.ProcessBytes(payload, SeqNumLengthBytes + AesIvLengthBytes, toDecipher.Length, decipherText, 0);
            try
            {
                cipher.DoFinal(decipherText, len);
            }
            catch (InvalidCipherTextException e)
            {
                // Mac check fails with BouncyCastle, but message is correctly decrypted
                if (!e.Message.Equals("mac check in GCM failed"))
                {
                    throw new InvalidOperationException("Error decrypting session payload", e);
                }
            }
            return decipherText;
        }
        catch (InvalidCipherTextException e)
        {
            Debug.LogError(e.Message);
            throw new InvalidOperationException("Error decrypting session payload", e);
        }
    }

    public ECPublicKeyParameters GenerateSessionEcdhSecret(byte[] otherPublicKey)
    {
        var publicKey = ParseHelloRsp(otherPublicKey);
        return GenerateSessionEcdhSecret(publicKey);
    }

    private ECPublicKeyParameters GenerateSessionEcdhSecret(ECPublicKeyParameters otherPublicKey)
    {
        var keyAgreement = new ECDHBasicAgreement();
        keyAgreement.Init((ECPrivateKeyParameters)_privateEphemeralKey.Private);

        var ecdhSecret = keyAgreement.CalculateAgreement(otherPublicKey);
        _encryptionKey = CreateEncryptionKey(ecdhSecret.ToByteArrayUnsigned(), PublicKey);
        return otherPublicKey;
    }

    private static byte[] CreateEncryptionKey(byte[] ecdhSecret, ECPublicKeyParameters associationPublicKey) 
    {
        var salt = EcdsaSignatures.EncodeP256PublicKey(associationPublicKey);
        var aes128KeyMaterial = HkdfSHA256L16(ecdhSecret, salt);
        return aes128KeyMaterial;
    }

    private static byte[] HkdfSHA256L16(byte[] ikm, byte[] salt)
    {
        HMac hmac = new HMac(new Sha256Digest());
        hmac.Init(new KeyParameter(salt));
        hmac.BlockUpdate(ikm, 0, ikm.Length);
        byte[] prk = new byte[hmac.GetMacSize()];
        hmac.DoFinal(prk, 0);
        hmac.Init(new KeyParameter(prk));
        hmac.BlockUpdate(new byte[] { 0x01 }, 0, 1);
        byte[] result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);
        byte[] output = new byte[16];
        Array.Copy(result, output, 16);
        return output;
    }
    
    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Split('=')[0]; // Remove any trailing '='s
        output = output.Replace('+', '-'); // 62nd char of encoding
        output = output.Replace('/', '_'); // 63rd char of encoding
        return output;
    }

}
