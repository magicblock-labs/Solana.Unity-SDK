using Avro;
using Avro.IO;
using Avro.Specific;
using Chaos.NaCl;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Wallet;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Solana.Unity.SDK.Editor
{

    internal class BundlrUploadTransaction
    {

        #region Constants

        /// <summary>
        /// The enumeration index of the Solana signature type in Bundlr.
        /// </summary>
        private const int SOLANA_SIG_TYPE_INDEX = 2;
        private const string DATA_ITEM_BUMP = "dataitem";
        private const string LIST_BUMP = "list";
        private const string BLOB_BUMP = "blob";

        #endregion

        #region Transaction Tags

        private const string CONTENT_TYPE_TAG = "Content-Type";

        #endregion

        #region Properties

        private byte[] EncodedTags {
            get {
                var avroWriter = new SpecificDefaultWriter(ArraySchema.Create(Tag._SCHEMA));
                using var stream = new MemoryStream();
                avroWriter.Write(tags, new BinaryEncoder(stream));
                return stream.ToArray();
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
            tags = new Tag[] { new() { name = CONTENT_TYPE_TAG, value = contentType } };
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
            var length = 2 + // SOLANA_SIG_TYPE_INDEX offset
                (ulong)Ed25519.SignatureSizeInBytes +
                (ulong)Ed25519.PublicKeySizeInBytes +
                2 + // Indicators for target & anchor.
                (ulong)anchor.Length +
                16 + // Tags + Encoded Tags length indicators.
                (ulong)encodedTags.Length +
                (ulong)data.Length;
            var bytes = new byte[length];
            var offset = 0;
            // Signature + Keys
            bytes.WriteU16(SOLANA_SIG_TYPE_INDEX, offset);
            offset += 2;
            bytes.WriteSpan(signature, offset);
            offset += Ed25519.SignatureSizeInBytes;
            bytes.WriteSpan(owner, offset);
            offset += owner.Length;
            // Indicator for target - not required for upload.
            bytes.WriteU8(0, offset);
            offset++;
            // Indicator for anchor + anchor bytes.
            bytes.WriteU8(1, offset);
            offset++;
            bytes.WriteSpan(anchor, offset);
            offset += anchor.Length;
            // Tags
            bytes.WriteU64((ulong)tags.Length, offset);
            offset += 8;
            bytes.WriteU64((ulong)encodedTags.Length, offset);
            offset += 8;
            bytes.WriteSpan(encodedTags, offset);
            offset += encodedTags.Length;
            // Data
            bytes.WriteSpan(data, offset);
            return bytes;
        }

        #endregion

        #region Private

        private byte[] GetMessage()
        {
            return DeepHashChunk(new byte[][] {
                Encoding.UTF8.GetBytes(DATA_ITEM_BUMP),
                Encoding.UTF8.GetBytes("1"),
                Encoding.UTF8.GetBytes(SOLANA_SIG_TYPE_INDEX.ToString()),
                owner,
                new byte[0], // target not required for upload transactions.
                anchor,
                EncodedTags,
                data
            });
        }

        private byte[] DeepHashChunk(byte[][] chunks)
        {
            var tagKey = Encoding.UTF8.GetBytes(LIST_BUMP);
            var tagValue = Encoding.UTF8.GetBytes(chunks.Length.ToString());
            var tag = new byte[tagKey.Length + tagValue.Length];
            tagKey.CopyTo(tag, 0);
            tagValue.CopyTo(tag, tagKey.Length);
            var acc = Sha384Hash(tag);

            return DeepHashChunksSync(chunks, acc);
        }

        private byte[] DeepHashChunk(byte[] chunk)
        {
            var tagKey = Encoding.UTF8.GetBytes(BLOB_BUMP);
            var tagValue = Encoding.UTF8.GetBytes(chunk.Length.ToString());
            var tagBuffer = Sha384Hash(tagKey.Concat(tagValue).ToArray());
            var taggedHash = tagBuffer.Concat(Sha384Hash(chunk)).ToArray();
            return Sha384Hash(taggedHash);
        }

        private byte[] DeepHashChunksSync(byte[][] chunks, byte[] acc)
        {
            if (chunks.Length == 0) { return acc; }
            var newChunks = new ArraySegment<byte[]>(chunks, 1, chunks.Length - 1).ToArray();
            var deepHash = DeepHashChunk(chunks[0]);
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