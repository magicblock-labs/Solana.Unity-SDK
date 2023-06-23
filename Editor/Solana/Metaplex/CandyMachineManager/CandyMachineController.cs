using Newtonsoft.Json;
using Solana.Unity.Metaplex.Utilities.Json;
using Solana.Unity.Rpc;
using Solana.Unity.SDK.Metaplex;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal class CandyMachineController
    {

        #region Types

        public struct UploadQueue
        {
            internal List<int> imagesToUpload;
            internal List<int> animationsToUpload;
            internal List<int> metadataToUpload;
        }

        #endregion

        #region Initialize

        internal static async void InitializeCandyMachine(
            CandyMachineConfiguration config,
            CandyMachineCache cache,
            string collectionNFTName,
            string collectionMetadataUrl,
            string keypair,
            string rpcUrl
        )
        {
            Debug.Log("Initializing CandyMachine...");
            var configData = config.ToCandyMachineData(cache);
            var candyMachineAccount = new Account();
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, "", SeedMode.Bip39);

            // Create collection NFT.

            var collectionMint = new Account();
            var collectionTxId = await CandyMachineCommands.CreateCollection(
                wallet.Account,
                collectionMint,
                new() { 
                    name = collectionNFTName,
                    symbol = config.symbol,
                    sellerFeeBasisPoints = 0,
                    uri = collectionMetadataUrl,
                    creators = config.creators.Select(c => new Unity.Metaplex.NFT.Library.Creator(new (c.address), c.share, true)).ToList(),
                },
                rpcClient
            );
            Debug.LogFormat("Minted Collection NFT - Transaction ID: {0}", collectionTxId);
            
            // Initialize CandyMachine account.

            var initTx = await CandyMachineCommands.InitializeCandyMachine(
                wallet.Account,
                candyMachineAccount,
                collectionMint,
                configData,
                rpcClient
            );
            Debug.LogFormat("Initializing CandyMachine - Transaction ID: {0}", initTx);

            cache.Info.CandyMachine = candyMachineAccount.PublicKey;
            cache.Info.CollectionMint = collectionMint.PublicKey;
            cache.Info.Creator = wallet.Account.PublicKey;
            cache.SyncFile();
            Debug.LogFormat("Initialized CandyMachine: {0}", candyMachineAccount.PublicKey);
        }

        #endregion

        #region Upload

        internal static async void UploadCandyMachineAssets(
            CandyMachineCache cache, 
            CandyMachineConfiguration config,
            string keypair,
            string rpcUrl
        )
        {
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var uploader = MetaplexUploaderFactory.New(
                rpcClient,
                IMetaplexAssetUploader.UploadMethod.Bundlr
            );
            var assetPairs = await GetCandyMachineAssetsAsync();
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, "", SeedMode.Bip39);

            if (assetPairs != null) 
            {
                var uploadQueue = await ValidateCache(cache, config, assetPairs);
                if (uploadQueue.HasValue) 
                {
                    Debug.LogFormat("Found {0} assets. Beginning upload.", assetPairs.Count);
                    if (uploadQueue.Value.imagesToUpload.Count > uploadQueue.Value.metadataToUpload.Count) 
                    {
                        Debug.LogError("There are more images files to upload than metadata. This should not happen.");
                        return;
                    }
                    var requiresUpload = uploadQueue.Value.imagesToUpload.Count > 0 ||
                        uploadQueue.Value.animationsToUpload.Count > 0 ||
                        uploadQueue.Value.metadataToUpload.Count > 0;

                    if (requiresUpload) 
                    {
                        await uploader.Prepare(wallet.Account, uploadQueue.Value, assetPairs);
                        Debug.Log("Uploading Image/Animation Assets...");

                        // Upload image assets.
                        await UploadAssetList(
                            rpcUrl,
                            uploadQueue.Value.imagesToUpload,
                            assetPairs,
                            cache,
                            LocalMetaplexAsset.AssetType.Image,
                            uploader
                        );

                        // Upload animation assets.
                        await UploadAssetList(
                            rpcUrl,
                            uploadQueue.Value.animationsToUpload,
                            assetPairs,
                            cache,
                            LocalMetaplexAsset.AssetType.Animation,
                            uploader
                        );

                        var metadataToRemove = new List<int>();
                        // Update metadata indices for any failed asset uploads.
                        foreach (var index in uploadQueue.Value.metadataToUpload) 
                        {
                            var cacheItem = cache.Items[index];
                            if (cacheItem.imageLink == string.Empty || cacheItem.animationLink == string.Empty) 
                            {
                                // Image or animation didn't upload.
                                metadataToRemove.Add(index);
                            }
                        }
                        foreach (var index in metadataToRemove) 
                        {
                            uploadQueue.Value.metadataToUpload.Remove(index);
                        }
                        Debug.Log("Asset upload complete. Uploading Metadata...");
                        await UploadAssetList(
                            rpcUrl,
                            uploadQueue.Value.metadataToUpload,
                            assetPairs,
                            cache,
                            LocalMetaplexAsset.AssetType.Metadata,
                            uploader
                        );
                        Debug.Log("Upload completed. Beginning sanity check...");
                    } else {
                        Debug.Log("No assets need uploading, skipping...");
                    }

                    var successfulUploadCount = 0;
                    foreach (var (index, cacheItem) in cache.Items) 
                    {
                        var assetPair = assetPairs[index];
                        var animationIsMissing = assetPair.animationLink != string.Empty &&
                            assetPair.animationLink == string.Empty;
                        var uploadFailed = cacheItem.imageLink == string.Empty || cacheItem.metadataLink == string.Empty || animationIsMissing;
                        if (!uploadFailed) 
                        {
                            successfulUploadCount++;
                        }
                    }
                    Debug.LogFormat("Successfully uploaded {0} assets.", successfulUploadCount);
                    var failCount = assetPairs.Count - successfulUploadCount;
                    if (failCount > 0) Debug.LogErrorFormat("Failed to upload {0} files.", failCount);
                }
            }
        }

        private static async Task UploadAssetList(
            string rpcUrl,
            List<int> indices,
            Dictionary<int, CandyMachineCache.CacheItem> assetPairs,
            CandyMachineCache cache,
            LocalMetaplexAsset.AssetType assetType,
            IMetaplexAssetUploader uploader
        )
        {
            Stack<LocalMetaplexAsset> assets = new();
            foreach (var index in indices) 
            {
                if (assetPairs.TryGetValue(index, out var assetPair)) 
                {
                    string filePath = assetType switch {
                        LocalMetaplexAsset.AssetType.Metadata => assetPair.metadataLink,
                        LocalMetaplexAsset.AssetType.Animation => assetPair.animationLink,
                        LocalMetaplexAsset.AssetType.Image => assetPair.imageLink,
                        _ => string.Empty
                    };
                    var extension = Path.GetExtension(filePath).Replace(".", string.Empty);
                    string contentType = assetType switch {
                        LocalMetaplexAsset.AssetType.Metadata => "application/json",
                        LocalMetaplexAsset.AssetType.Animation => string.Format("video/{0}", extension),
                        LocalMetaplexAsset.AssetType.Image => string.Format("image/{0}", extension),
                        _ => string.Empty
                    };
                    if (cache.Items.TryGetValue(index, out var cacheItem)) 
                    {
                        /* Replaces the media link without modifying the original file to avoid changing 
                        the hash of the metadata file. */
                        var content = assetType switch {
                            LocalMetaplexAsset.AssetType.Metadata => IMetaplexAssetUploader.GetUpdatedMetadata(
                                filePath,
                                cacheItem.imageLink,
                                cacheItem.animationLink
                            ),
                            _ => filePath
                        };
                        assets.Push(new LocalMetaplexAsset {
                            AssetId = index,
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            Content = content,
                            ContentType = contentType,
                            Type = assetType
                        });
                    }
                }
            }
            await uploader.Upload(rpcUrl, cache, assetType, assets);
        }

        private static async Task<UploadQueue?> ValidateCache(
            CandyMachineCache cache,
            CandyMachineConfiguration config,
            Dictionary<int, CandyMachineCache.CacheItem> assetPairs
        )
        {
            var uploadQueue = new UploadQueue {
                imagesToUpload = new(),
                animationsToUpload = new(),
                metadataToUpload = new(),
            };

            foreach (var (index, assetPair) in assetPairs) 
            {
                EditorUtility.DisplayProgressBar("Candy Machine Manager", "Validating cache...", index / assetPairs.Count);
                var metadataIsValid = await Task.Run(() => {
                    string metadataJson = File.ReadAllText(assetPair.metadataLink);
                    var metadata = JsonConvert.DeserializeObject<MetaplexTokenStandard>(metadataJson);
                    var imageIsUploaded = AssetIsUploaded(metadata.default_image);
                    var animationIsUploaded = AssetIsUploaded(metadata.animation_url);

                    if (cache.Items.TryGetValue(index, out var cacheItem))
                    {
                        var imageChanged = cacheItem.imageHash != assetPair.imageHash ||
                            cacheItem.imageLink == string.Empty && !imageIsUploaded;

                        var animationChanged = cacheItem.animationHash != assetPair.animationHash ||
                            cacheItem.animationLink == string.Empty && !animationIsUploaded;

                        var metadataChanged = cacheItem.metadataHash != assetPair.metadataHash ||
                            cacheItem.metadataLink == string.Empty;

                        if (imageChanged) 
                        {
                            cacheItem.imageHash = assetPair.imageHash;
                            cacheItem.imageLink = string.Empty;
                            uploadQueue.imagesToUpload.Add(index);
                        }

                        if (animationChanged) 
                        {
                            cacheItem.animationHash = assetPair.animationHash;
                            cacheItem.animationLink = string.Empty;
                            uploadQueue.animationsToUpload.Add(index);
                        }

                        if (metadataChanged || imageChanged || animationChanged)
                        {
                            cacheItem.metadataHash = assetPair.metadataHash;
                            cacheItem.metadataLink = string.Empty;
                            cacheItem.onChain = false;

                            // Only need to upload metadata.
                            uploadQueue.metadataToUpload.Add(index);
                        }

                        cache.Items[index] = cacheItem;
                    } else {
                        if (!imageIsUploaded) 
                        {
                            uploadQueue.imagesToUpload.Add(index);
                        }

                        if (metadata.animation_url != null && !animationIsUploaded) 
                        {
                            uploadQueue.animationsToUpload.Add(index);
                        }
                        uploadQueue.metadataToUpload.Add(index);
                        cache.Items.Add(index, assetPair);
                    }
                    if (metadata.symbol != null && metadata.symbol != config.symbol) 
                    {
                        Debug.LogErrorFormat("Symbol for asset {0}, does not match config file.", index);
                        return false;
                    } else if (metadata.seller_fee_basis_points != config.sellerFeeBasisPoints) {
                        Debug.LogErrorFormat("Seller fee basis points for asset {0}, does not match config file.", index);
                        return false;
                    }
                    return true;
                });
                if (!metadataIsValid) return null;
            }
            EditorUtility.ClearProgressBar();
            return uploadQueue;
        }

        private static async Task<Dictionary<int, CandyMachineCache.CacheItem>> GetCandyMachineAssetsAsync()
        {
            var assetPairs = new Dictionary<int, CandyMachineCache.CacheItem>();
            var validImageTypes = new string[] { ".jpg", ".jpeg", ".gif", ".png" };
            var validAnimationTypes = new string[] { ".mp3", ".mp4", ".mov", ".webm", ".glb" };
            var assetFolderPath = EditorUtility.OpenFolderPanel("Select asset folder", string.Empty, string.Empty);

            var filePaths = Directory.EnumerateFiles(assetFolderPath);
            var fileIndex = 0;

            bool assetsAreValid = await ValidateAssetNames(filePaths);
            if (!assetsAreValid) return null;

            // Encode assets
            var metadataFiles = filePaths.Where((path) => path.EndsWith(".json"));
            var fileCount = metadataFiles.Count();
            foreach (var metadataFile in metadataFiles) {
                EditorUtility.DisplayProgressBar("CandyMachine Manager", "Loading assets...", fileIndex / (float)fileCount);
                // Run as task so Unity UI thread remains unblocked.
                var valid = await Task.Run(() => {
                    var fileName = Path.GetFileNameWithoutExtension(metadataFile);
                    int index;
                    if (fileName == "collection") 
                    {
                        index = -1;
                    } else {
                        index = int.Parse(fileName);
                    }
                    var assetFiles = filePaths.Where(filePath => Path.GetFileNameWithoutExtension(filePath) == fileName);
                    var imageFiles = assetFiles.Where(filePath => validImageTypes.Contains(Path.GetExtension(filePath)));
                    if (imageFiles.Count() != 1) 
                    {
                        Debug.LogErrorFormat("Couldn't find image file at index {0}.", index);
                        return false;
                    }
                    var animationFiles = assetFiles.Where(filePath => validAnimationTypes.Contains(Path.GetExtension(filePath)));
                    var metadataJson = File.ReadAllText(metadataFile);
                    var metadata = JsonConvert.DeserializeObject<MetaplexTokenStandard>(metadataJson);

                    var imageBytes = Encoding.Default.GetBytes(imageFiles.ElementAt(0));
                    var imageHash = BitConverter.ToString(imageBytes).Replace("-", "");

                    var metadataBytes = Encoding.Default.GetBytes(metadataFile);
                    var metadataHash = BitConverter.ToString(metadataBytes).Replace("-", "");

                    string animationFile = null;
                    if (animationFiles.Count() == 1) 
                    {
                        animationFile = animationFiles.ElementAt(0);
                    }
                    var animationBytes = animationFile != null ? Encoding.Default.GetBytes(animationFiles.ElementAt(0)) : null;
                    var animationHash = animationFile != null ? BitConverter.ToString(animationBytes).Replace("-", "") : null;

                    var assetPair = new CandyMachineCache.CacheItem(
                        metadata.name,
                        imageHash,
                        imageFiles.ElementAt(0),
                        metadataHash,
                        metadataFile,
                        false,
                        animationHash,
                        animationFile
                    );

                    assetPairs.Add(index, assetPair);
                    fileIndex++;
                    return true;
                });
                if (!valid) return null;
            }

            EditorUtility.ClearProgressBar();
            return assetPairs;
        }

        private static async Task<bool> ValidateAssetNames(IEnumerable<string> filePaths)
        {
            var fileCount = filePaths.Count();
            int fileIndex = 0;
            // Check file names have valid indices.
            foreach (var path in filePaths) 
            {
                EditorUtility.DisplayProgressBar("CandyMachine Manager", "Validating file names...", fileIndex / (float)fileCount);
                // Run as task so Unity UI thread remains unblocked.
                var fileIsValid = await Task.Run(() => {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    fileIndex++;
                    if (fileName != "collection") {
                        return int.TryParse(fileName, out _);
                    }
                    return true;
                });
                if (!fileIsValid) 
                {
                    Debug.LogErrorFormat("Couldn't parse file {0} as a valid index.", path);
                    return false;
                }
            }
            EditorUtility.ClearProgressBar();
            return true;
        }

        private static bool AssetIsUploaded(string path)
        {
            Uri.TryCreate(path, UriKind.Absolute, out var uri);
            return uri?.IsFile == false;
        }

        #endregion

        #region Mint

        internal static async void MintToken(
            PublicKey candyMachineKey,
            PublicKey candyGuardKey,
            string keypair,
            string rpcUrl
        )
        {
            Debug.Log("Minting Token...");
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, "", SeedMode.Bip39);
            var mintAccount = new Account();
            var txId = await CandyMachineCommands.MintOneToken(
                wallet.Account,
                mintAccount,
                candyMachineKey, 
                candyGuardKey, 
                new() {
                    NftGate = new() { Mint = new("GcCuUs735yuBSvzfNvvPTfUP67mYMvCeLJf6mK55NsfC") }
                }, 
                rpcClient
            );
            if (txId != null) 
            {
                Debug.LogFormat("Mint transaction: {0}", txId);
                Debug.LogFormat("Minted NFT - Address: {0}", mintAccount);
            } 
            else 
            {
                Debug.LogError("Mint transaction failed.");
            }
        }

        #endregion
    }
}
