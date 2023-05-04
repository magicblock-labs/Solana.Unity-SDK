using System;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using UnityEngine.Assertions;

// ReSharper disable once CheckNamespace

public static class EcdsaSignatures
{
    
    // CONSTANTS 
    private const int EncodedPublicKeyLengthBytes = 65;
    private const int P256DerSignaturePrefixLen = 2; // 0x30 || 1-byte length
    private const byte P256DerSignaturePrefixType = (byte)0x30;
    private const int P256DerSignatureComponentPrefixLen = 2; // 0x02 || 1-byte length
    private const byte P256DerSignatureComponentPrefixType = (byte)0x02;
    private const int P256DerSignatureComponentMinLen = 1;
    private const int P256DerSignatureComponentMaxLen = 33;
    private const int P256P1363ComponentLen = 32;
    private const int P256P1363SignatureLen = 64;
    
    
    /// <summary>
    /// Utils to encode a P256 public key into a byte array
    /// </summary>
    /// <param name="ecPublicKey"></param>
    /// <returns></returns>
    public static byte[] EncodeP256PublicKey(ECPublicKeyParameters ecPublicKey) {
        var w = ecPublicKey.Q;
        var x = w.AffineXCoord.GetEncoded();
        var y = w.AffineYCoord.GetEncoded();
        var encodedPublicKey = new byte[EncodedPublicKeyLengthBytes];
        encodedPublicKey[0] = 0x04;
        int xLen = Math.Min(x.Length, 32);
        int yLen = Math.Min(y.Length, 32);
        Array.Copy(x, x.Length - xLen, encodedPublicKey, 33 - xLen, xLen);
        Array.Copy(y, y.Length - yLen, encodedPublicKey, 65 - yLen, yLen);
        return encodedPublicKey;
    }
    
    /// <summary>
    /// Convert a DER-encoded ECDSA signature to the P1363 format
    /// </summary>
    /// <param name="derSignature"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] ConvertEcp256SignatureDeRtoP1363(byte[] derSignature, int offset) {
        if ((offset + P256DerSignaturePrefixLen) > derSignature.Length) {
            throw new ArgumentException("DER signature buffer too short to define sequence");
        }

        byte derType = derSignature[offset];
        if (derType != P256DerSignaturePrefixType) {
            throw new ArgumentException("DER signature has invalid type");
        }

        int derSeqLen = derSignature[offset + 1];

        byte[] p1363Signature = new byte[P256P1363SignatureLen];
        int sOff = UnpackDerIntegerToP1363Component(derSignature,
            offset + P256DerSignaturePrefixLen, p1363Signature, 0);
        int totalOff = UnpackDerIntegerToP1363Component(derSignature, sOff, p1363Signature,
            P256P1363ComponentLen);

        if ((offset + P256DerSignaturePrefixLen + derSeqLen) != totalOff) {
            throw new ArgumentException("Invalid DER signature length");
        }

        return p1363Signature;
    }
    
    /// <summary>
    /// Convert a P1363-encoded ECDSA signature to the DER format
    /// </summary>
    /// <param name="p1363Signature"></param>
    /// <param name="p1363Offset"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static byte[] ConvertEcp256SignatureP1363ToDer(byte[] p1363Signature, int p1363Offset)
    {
        if ((p1363Offset + P256P1363SignatureLen) > p1363Signature.Length)
        {
            throw new Exception("Invalid P1363 signature length");
        }

        int rDerIntLen = CalculateDerIntLengthOfP1363Component(p1363Signature, p1363Offset);
        int sDerIntLen = CalculateDerIntLengthOfP1363Component(p1363Signature, p1363Offset + P256P1363ComponentLen);

        byte[] derSignature = new byte[P256DerSignaturePrefixLen +
                                       2 * P256DerSignatureComponentPrefixLen + rDerIntLen + sDerIntLen];
        derSignature[0] = P256DerSignaturePrefixType;
        derSignature[1] = (byte)(2 * P256DerSignatureComponentPrefixLen + rDerIntLen + sDerIntLen);
        int sOff = PackP1363ComponentToDerInteger(p1363Signature, p1363Offset, rDerIntLen,
            derSignature, P256DerSignaturePrefixLen);
        int totalLen = PackP1363ComponentToDerInteger(p1363Signature,
            p1363Offset + P256P1363ComponentLen, sDerIntLen, derSignature, sOff);
        Assert.IsTrue(totalLen == derSignature.Length);
        return derSignature;
    }
    
    /// <summary>
    /// Pack a P1363-encoded ECDSA signature component into a DER-encoded integer
    /// </summary>
    /// <param name="p1363Signature"></param>
    /// <param name="p1363Offset"></param>
    /// <param name="p1363ComponentDerIntLength"></param>
    /// <param name="derSignature"></param>
    /// <param name="derOffset"></param>
    /// <returns></returns>
    private static int PackP1363ComponentToDerInteger(
        byte[] p1363Signature,
        int p1363Offset,
        int p1363ComponentDerIntLength,
        byte[] derSignature,
        int derOffset) {
        
        Assert.IsTrue(p1363Offset > 0);
        Assert.IsTrue(p1363ComponentDerIntLength is > 1 and <= P256P1363ComponentLen + 1);
        
        derSignature[derOffset] = P256DerSignatureComponentPrefixType;
        derSignature[derOffset + 1] = (byte)p1363ComponentDerIntLength;

        int leadingBytes = Math.Max(0, p1363ComponentDerIntLength - P256P1363ComponentLen);
        int copyLen = Math.Min(p1363ComponentDerIntLength, P256P1363ComponentLen);
        Array.Fill(derSignature, (byte) 0, derOffset + P256DerSignatureComponentPrefixLen, 
            leadingBytes) ;
        Array.Copy(p1363Signature, p1363Offset + P256P1363ComponentLen - copyLen,
            derSignature, derOffset + P256DerSignatureComponentPrefixLen + leadingBytes,
            copyLen);

        return derOffset + P256DerSignatureComponentPrefixLen + p1363ComponentDerIntLength;
    }
    
    /// <summary>
    /// Calculate the length of a DER-encoded integer from a P1363-encoded ECDSA signature component
    /// </summary>
    /// <param name="p1363Signature"></param>
    /// <param name="p1363Offset"></param>
    /// <returns></returns>
    private static int CalculateDerIntLengthOfP1363Component(byte[] p1363Signature, int p1363Offset) {
        byte val = p1363Signature[p1363Offset];
        if (val > 127) {
            return P256P1363ComponentLen + 1;
        }
        return P256P1363ComponentLen;
    }

    /// <summary>
    /// Unpack a DER-encoded integer into a P1363-encoded ECDSA signature component
    /// </summary>
    /// <param name="derSignature"></param>
    /// <param name="derOffset"></param>
    /// <param name="p1363Signature"></param>
    /// <param name="p1363Offset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private static int UnpackDerIntegerToP1363Component(byte[] derSignature, int derOffset, byte[] p1363Signature, int p1363Offset) {
        if ((derOffset + P256DerSignatureComponentPrefixLen) > derSignature.Length) {
            throw new ArgumentOutOfRangeException("DER signature buffer too short to define component");
        }

        var componentDerType = derSignature[derOffset];
        int componentLen = derSignature[derOffset + 1];

        if (componentDerType != P256DerSignatureComponentPrefixType ||
            componentLen < P256DerSignatureComponentMinLen ||
            componentLen > P256DerSignatureComponentMaxLen) {
            throw new ArgumentException("DER signature component not well formed");
        }

        if ((derOffset + P256DerSignatureComponentPrefixLen + componentLen) >
            derSignature.Length) {
            throw new ArgumentException("DER signature component exceeds buffer length");
        }

        var copyLen = Math.Min(componentLen, P256P1363ComponentLen);
        
        var srcOffset = derOffset + P256DerSignatureComponentPrefixLen +
            componentLen - copyLen;
        var dstOffset = p1363Offset + P256P1363ComponentLen - copyLen;
        Array.Fill(p1363Signature, (byte)0, p1363Offset, P256P1363ComponentLen - copyLen);
        Array.Copy(derSignature, srcOffset, p1363Signature, dstOffset, copyLen);
        return derOffset + P256DerSignatureComponentPrefixLen + componentLen;
    }

    
    /// <summary>
    /// Decode a DER-encoded ECDSA signature into a P1363-encoded ECDSA signature
    /// </summary>
    /// <param name="encodedPublicKey"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ECPublicKeyParameters DecodeP256PublicKey(byte[] encodedPublicKey)
    {
        if (encodedPublicKey.Length < EncodedPublicKeyLengthBytes || encodedPublicKey[0] != 0x04)
        {
            throw new ArgumentException("input is not an EC P-256 public key");
        }

        byte[] x = new byte[32];
        byte[] y = new byte[32];
        Array.Copy(encodedPublicKey, 1, x, 0, 32);
        Array.Copy(encodedPublicKey, 33, y, 0, 32);

        X9ECParameters ecP = SecNamedCurves.GetByName("secp256r1");
        ECDomainParameters ecSpec = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H);
        BigInteger xBig = new BigInteger(1, x);
        BigInteger yBig = new BigInteger(1, y);

        ECPoint w = ecP.Curve.CreatePoint(xBig, yBig);
        return new ECPublicKeyParameters(w, ecSpec);
    }
}