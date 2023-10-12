using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal class BundlrUploader : MetaplexParallelAssetUploader
    {

        #region Constants

        /// <summary>
        /// The number os retries to fetch the Bundlr balance (MAX_RETRY * DELAY_UNTIL_RETRY ms limit).
        /// </summary>
        const int MAX_RETRY = 120;

        /// <summary>
        /// Time (ms) to wait until next try.
        /// </summary>
        const int DELAY_UNTIL_RETRY = 1000;

        /// <summary>
        /// Size of Bundlr transaction header.
        /// </summary>
        const ulong HEADER_SIZE = 2_000;

        /// <summary>
        /// Minimum file size for cost calculation.
        /// </summary>
        const ulong MINIMUM_SIZE = 80_000;

        const int MOCK_URI_SIZE = 100;

        /// <summary>
        /// URI format for an Arweave file.
        /// </summary>
        const string ARWEAVE_URI_FORMAT = "https://arweave.net/{0}";

        /// <summary>
        /// Parameter format for an Arweave file extension, used for animation & image files.
        /// </summary>
        const string ARWEAVE_EXT_FORMAT = "?ext={0}";

        /// <summary>
        /// Bundlr devnet endpoint.
        /// </summary>
        const string BUNDLR_DEVNET = "https://devnet.bundlr.network";

        /// <summary>
        /// Bundlr mainnet endpoint.
        /// </summary>
        const string BUNDLR_MAINNET = "https://node1.bundlr.network";

        const string DEVNET_GENESIS_HASH = "EtWTRABZaYq6iMfeYKouRu166VU2xqa1wcaWoxPkrZBG";

        const string MAINNET_GENESIS_HASH = "5eykt4UsFv8P8NJdTREpY1vzqKqZKvdpKuc147dw2N9d";

        const float FUND_PADDING = 1.3f;

        #endregion

        #region Fields

        private BundlrClient bundlrClient;
        private readonly IRpcClient rpcClient;

        #endregion

        #region Constructor

        internal BundlrUploader(IRpcClient rpcClient)
        {
            this.rpcClient = rpcClient;
        }

        #endregion

        #region MetaplexParallelAssetUploader

        public override async Task Prepare(
            Account payer,
            CandyMachineController.UploadQueue uploadQueue,
            Dictionary<int, CandyMachineCache.CacheItem> assetPairs
        )
        {
            var cluster = await rpcClient.GetGenesisHashAsync();
            var node = cluster.Result switch {
                MAINNET_GENESIS_HASH => BUNDLR_MAINNET,
                DEVNET_GENESIS_HASH => BUNDLR_DEVNET,
                _ => throw new InvalidOperationException("Bundlr is only supported on devnet or mainnet")
            };
            bundlrClient = new(node, payer);
            var bundlrAddress = await bundlrClient.GetBundlrSolanaAddress();
            ulong uploadSize = 0;

            foreach (var index in uploadQueue.imagesToUpload)
            {
                if (assetPairs.TryGetValue(index, out var assetPair))
                {
                    var fileSize = new FileInfo(assetPair.imageLink).Length;
                    uploadSize += HEADER_SIZE + (ulong)MathF.Max(MINIMUM_SIZE, fileSize);
                }
            }

            foreach (var index in uploadQueue.animationsToUpload)
            {
                if (assetPairs.TryGetValue(index, out var assetPair))
                {
                    var fileSize = new FileInfo(assetPair.animationLink).Length;
                    uploadSize += HEADER_SIZE + (ulong)MathF.Max(MINIMUM_SIZE, fileSize);
                }
            }

            foreach (var index in uploadQueue.metadataToUpload)
            {
                if (assetPairs.TryGetValue(index, out var assetPair))
                {
                    var mockUri = new string('x', MOCK_URI_SIZE);
                    var updatedMetadata = IMetaplexAssetUploader.GetUpdatedMetadata(
                        assetPair.metadataLink,
                        mockUri,
                        assetPair.animationLink == string.Empty ? string.Empty : mockUri
                    );
                    var metadataSize = Encoding.UTF8.GetByteCount(updatedMetadata);
                    uploadSize += HEADER_SIZE + (ulong)MathF.Max(MINIMUM_SIZE, metadataSize);
                }
            }

            Debug.LogFormat("Total upload size is: {0}", uploadSize);
            var lamportsFee = await bundlrClient.GetBundlrFee(uploadSize);
            var balance = await bundlrClient.GetBundlrBalance(payer);
            Debug.LogFormat("Bundlr balance: {0}. Required balance: {1}", balance, lamportsFee);

            if (lamportsFee > balance) 
            {
                Debug.Log("Attempting to fund Bundlr address.");
                var fundsRequired = (ulong)Mathf.CeilToInt(FUND_PADDING * (lamportsFee - balance));
                Debug.LogFormat("Additional funds required: {0}", fundsRequired);
                var funded = await bundlrClient.FundBundlrAddress(payer, rpcClient, bundlrAddress, fundsRequired);
                if (!funded) 
                {
                    throw new Exception("Insufficient Funds");
                }
                balance = await bundlrClient.GetBundlrBalance(payer);
                if (balance < lamportsFee) 
                {
                    throw new Exception(string.Format("No Bundlr balance found for address: {0}", payer.PublicKey));
                }
            }
        }

        protected override async Task<(int, string)> UploadAsset(LocalMetaplexAsset asset)
        {
            var data = asset.Type switch {
                LocalMetaplexAsset.AssetType.Metadata => Encoding.UTF8.GetBytes(asset.Content),
                _ => File.ReadAllBytes(asset.Content),
            };
            var tx = new BundlrUploadTransaction(data, asset.ContentType);
            var assetID = await bundlrClient.SendTransaction(tx);
            if (assetID == null) 
            {
                return (asset.AssetId, null);
            }
            var assetLink = string.Format(ARWEAVE_URI_FORMAT, assetID);
            var extension = asset.Type switch {
                LocalMetaplexAsset.AssetType.Metadata => string.Empty,
                _ => string.Format(ARWEAVE_EXT_FORMAT, asset.ContentType.Split("/")[1])
            };
            return (asset.AssetId, string.Concat(assetLink, extension));
        }

        #endregion
    }
}
