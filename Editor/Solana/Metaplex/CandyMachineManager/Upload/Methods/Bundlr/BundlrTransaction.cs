using Chaos.NaCl;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Wallet;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable IDE0052 // Remove unread private members

namespace Solana.Unity.SDK.Editor
{

    // TODO: Fix serialization.
    internal class BundlrUploadTransaction
    {

        #region Constants

        /// <summary>
        /// The enumeration index of the Solana signature type in Bundlr.
        /// </summary>
        private const int SOLANA_SIG_TYPE_INDEX = 4;

        #endregion

        #region Transaction Tags

        private const string CONTENT_TYPE_TAG = "Content-Type";

        [Serializable]
        private struct Tag {
            private readonly string name;
            private readonly string value;

            internal Tag(string name, string value)
            {
                this.name = name;
                this.value = value;
            }
        }

        #endregion

        #region Properties

        private byte[] EncodedTags {
            get {
                var bf = new BinaryFormatter();
                using var ms = new MemoryStream();
                bf.Serialize(ms, tags);
                return ms.ToArray();
            }
        }

        #endregion

        #region Fields

        private readonly byte[] data;
        private readonly Tag[] tags;
        private byte[] signature;
        private byte[] owner;
        private readonly byte[] anchor = new byte[32];

        #endregion

        #region Constructors

        internal BundlrUploadTransaction(byte[] data, string contentType)
        {
            this.data = data;
            tags = new[] { new Tag(CONTENT_TYPE_TAG, contentType) };
            var anchorGenerator = RandomNumberGenerator.Create();
            anchorGenerator.GetBytes(anchor);
        }

        #endregion

        #region Internal 

        internal void Sign(Account signer)
        {
            owner = signer.PublicKey.KeyBytes;
            var message = GetMessage();
            signature = signer.PrivateKey.Sign(message);
        }

        internal byte[] Serialize()
        {
            var encodedTags = EncodedTags;
            var length = 2 +
                (ulong)Ed25519.SignatureSizeInBytes +
                (ulong)Ed25519.PublicKeySizeInBytes +
                34 +
                16 +
                (ulong)encodedTags.Length +
                (ulong)data.Length;
            var bytes = new byte[length];
            var offset = 0;
            bytes.WriteU16(SOLANA_SIG_TYPE_INDEX, offset);
            offset += 2;
            bytes.WriteSpan(signature, offset);
            offset += Ed25519.SignatureSizeInBytes;
            bytes.WriteSpan(owner, offset);
            offset += owner.Length;
            bytes.WriteU8(0, offset);
            offset++;
            bytes.WriteU8(1, offset);
            offset++;
            bytes.WriteSpan(anchor, offset);
            offset += anchor.Length;
            bytes.WriteU64((ulong)tags.Length, offset);
            offset += 8;
            bytes.WriteU64((ulong)encodedTags.Length, offset);
            offset += 8;
            bytes.WriteSpan(encodedTags, offset);
            offset += encodedTags.Length;
            bytes.WriteSpan(data, offset);

            return bytes;
        }

        #endregion

        #region Private

        private byte[] GetMessage()
        {
            return DeepHashChunk(new byte[][] {
                Encoding.ASCII.GetBytes("dataitem"),
                Encoding.ASCII.GetBytes("1"),
                BitConverter.GetBytes(SOLANA_SIG_TYPE_INDEX),
                owner,
                new byte[] {},
                anchor,
                EncodedTags,
                data
            });
        }

        private byte[] DeepHashChunk(byte[][] chunks)
        {
            var tagKey = Encoding.ASCII.GetBytes("list");
            var tagValue = Encoding.ASCII.GetBytes(chunks.Length.ToString());
            var tag = new byte[tagKey.Length + tagValue.Length];
            tagKey.CopyTo(tag, 0);
            tagValue.CopyTo(tag, tagKey.Length);
            var acc = Sha384Hash(tag);

            return DeepHashChunksSync(chunks, acc);
        }

        private byte[] DeepHashChunksSync(byte[][] chunks, byte[] acc)
        {
            if (chunks.Length == 0) { return acc; }
            var newChunks = new ArraySegment<byte[]>(chunks, 1, chunks.Length - 1).ToArray();
            var deepHash = DeepHashChunk(newChunks);
            var hashPair = new byte[deepHash.Length + acc.Length];
            acc.CopyTo(hashPair, 0);
            deepHash.CopyTo(hashPair, acc.Length);
            var newAcc = Sha384Hash(hashPair);
    
            return DeepHashChunksSync(newChunks, newAcc);
        }

        private byte[] Sha384Hash(byte[] data)
        {
            var sha384 = new SHA384Managed();
            return sha384.ComputeHash(data);
        }

        #endregion

    }
}