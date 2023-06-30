using Newtonsoft.Json;
using Solana.Unity.Extensions;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Metaplex.Utilities.Json;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK.Metaplex;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        #region Constants

        private const string ZERO_INDEX_NAME_PATTERN = "ID";
        private const string ONE_INDEX_NAME_PATTERN = "ID+1";
        private const char NAME_PATTERN_DESIGNATOR = '$';

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
            cache.SyncFile(config.cacheFilePath);
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
                            config,
                            cache,
                            LocalMetaplexAsset.AssetType.Image,
                            uploader
                        );

                        // Upload animation assets.
                        await UploadAssetList(
                            rpcUrl,
                            uploadQueue.Value.animationsToUpload,
                            assetPairs,
                            config,
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
                            config,
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
            CandyMachineConfiguration config,
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
            await uploader.Upload(rpcUrl, config, cache, assetType, assets);
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
            CandyMachineConfiguration config,
            string keypair,
            string rpcUrl,
            string guardGroup
        )
        {
            Debug.Log("Minting Token...");
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, string.Empty, SeedMode.Bip39);
            var mintAccount = new Account();
            var mintSettings = await GetMintSettings(guardGroup, config, wallet, rpcClient);
            string txId;
            if (candyGuardKey != null) 
            {
                txId = await CandyMachineCommands.MintOneTokenWithGuards(
                    wallet.Account,
                    wallet.Account,
                    mintAccount,
                    wallet.Account,
                    candyMachineKey,
                    candyGuardKey,
                    mintSettings,
                    rpcClient
                );
            }
            else 
            {
                txId = await CandyMachineCommands.MintOneToken(
                    wallet.Account,
                    wallet.Account,
                    mintAccount,
                    wallet.Account,
                    candyMachineKey,
                    rpcClient
                );
            }
            
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

        #region Guards

        internal static async void UpdateGuards(
            PublicKey candyGuardKey,
            PublicKey candyMachineKey,
            CandyMachineConfiguration config,
            CandyMachineCache cache,
            string keypair,
            string rpcUrl
        )
        {
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, string.Empty, SeedMode.Bip39);
            var guardData = new GuardData() {
                Default = config.guards.defaultGuards.FormattedSet,
                Groups = config.guards.groups.Select(group => group.FormattedGroup).ToArray()
            };
            string updateTx;
            var candyGuard = candyGuardKey;
            if (candyGuardKey != null) 
            {
                Debug.Log("Updating Guards...");
                updateTx = await CandyMachineCommands.AddGuards(
                    wallet.Account,
                    candyGuardKey,
                    guardData,
                    rpcClient
                );
            }
            else 
            {
                Debug.Log("Initializing Guards...");
                (updateTx, candyGuard) = await CandyMachineCommands.InitializeGuards(
                    wallet.Account,
                    guardData,
                    rpcClient
                );
                cache.Info.CandyGuard = candyGuard;
                cache.SyncFile(config.cacheFilePath);
            }
            if (updateTx == null) {
                Debug.LogError("Failed to update guard account.");
                return;
            }

            Debug.Log("Wrapping CandyMachine...");
            var wrapTx = await CandyMachineCommands.WrapCandyMachine(
                wallet.Account,
                candyGuard,
                candyMachineKey,
                rpcClient
            );
            if (wrapTx == null) 
            {
                Debug.LogError("Failed to update guard account.");
            }
            else 
            {
                Debug.LogFormat("Wrap transaction: {0}", wrapTx);
                Debug.Log("The candy guard is now the mint authority of the candy machine.");
            }
        }

        private static async Task<CandyGuardMintSettings> GetMintSettings(
            string guardGroup,
            CandyMachineConfiguration config,
            Wallet.Wallet minter,
            IRpcClient rpcClient
        )
        {
            var tokenWallet = await TokenWallet.LoadAsync(rpcClient, new TokenMintResolver(), minter.Account);
            var balances = tokenWallet.Balances();
            var tokenAccounts = new List<MetadataAccount>();
            foreach (var balance in balances) 
            {
                var account = await MetadataAccount.GetAccount(rpcClient, new PublicKey(balance.TokenMint));
                tokenAccounts.Add(account);
            }

            var settings = config.guards?.defaultGuards?.GetMintSettings(null, tokenAccounts.ToArray());
            if (guardGroup != null) 
            {
                var groups = config.guards.groups.Where(group => group.label == guardGroup).ToArray();
                if (groups.Length > 0) 
                {
                    var groupSettings = groups[0].GetMintSettings(tokenAccounts.ToArray());
                    settings.OverrideWith(groupSettings);
                }
                else 
                {
                    Debug.LogErrorFormat("Couldn't find guard group {0}.", guardGroup);
                }
            }
            return settings;
        }

        #endregion

        #region Reveal

        internal static async void Reveal(
            CandyMachineCache cache,
            HiddenSettings hiddenSettings,
            PublicKey candyMachineKey,
            string keypair,
            string rpcUrl,
            bool skipPreflight = false
        )
        {
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, string.Empty, SeedMode.Bip39);
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            if (hiddenSettings == null)
            {
                Debug.LogError("HiddenSettings must be present for a reveal.");
                return;
            }
            Debug.Log("Getting metadata accounts...");
            var creator = CandyMachineCommands.GetCandyMachineCreator(candyMachineKey);
            var metadataKeys = await GetCreatorMetadataAccounts(creator, 0, rpcClient);
            if (metadataKeys == null || metadataKeys.Count == 0) 
            {
                Debug.LogErrorFormat("No minted NFTs found for CandyMachine {0}", candyMachineKey);
                return;
            }
            Debug.LogFormat("Found {0} metadata accounts.", metadataKeys.Count);
            var accIndex = 0;
            var accountInfo = new List<AccountInfo>();
            while (accIndex < metadataKeys.Count) 
            {
                EditorUtility.DisplayProgressBar("Getting NFT Accounts...", string.Empty, accIndex / (float)metadataKeys.Count);
                var chunkLength = Mathf.Min(100, metadataKeys.Count - accIndex);
                var keys = metadataKeys.GetRange(accIndex, chunkLength).Select(pair => pair.PublicKey).ToList();
                var accounts = await rpcClient.GetMultipleAccountsAsync(keys);
                if (accounts.Result != null) 
                {
                    accountInfo.AddRange(accounts.Result.Value);
                }
                accIndex = chunkLength;
            }
            EditorUtility.ClearProgressBar();

            var namePatterns = hiddenSettings.Name.Split(NAME_PATTERN_DESIGNATOR);
            if (namePatterns.Length < 3) 
            {
                Debug.LogError("No name pattern set in hidden settings");
                return;
            }
            var indexPattern = namePatterns[1] switch {
                ZERO_INDEX_NAME_PATTERN => 0,
                ONE_INDEX_NAME_PATTERN => 1,
                _ => -1
            };
            if (indexPattern == -1) 
            {
                Debug.LogError("Invalid name pattern in hidden settings");
                return;
            }
            var nftLookup = cache.Items
                .Where((nft) => nft.Key != -1 && !nft.Value.onChain)
                // Use index pattern to increment key.
                .ToDictionary(pair => pair.Key + indexPattern, pair => pair.Value);
            var namePrefix = namePatterns[0];
            var nameSuffix = namePatterns[2];
            var revealErrors = await RevealMetadata(
                accountInfo,
                namePrefix,
                nameSuffix,
                nftLookup,
                wallet.Account,
                skipPreflight,
                rpcClient
            );
            if (revealErrors)
            {
                Debug.LogError("Some reveals failed, re-run the command to finish reveal.");
            } 
            else
            {
                Debug.Log("Reveal Complete!");
            }
            return;
        }

        private static async Task<bool> RevealMetadata(
            List<AccountInfo> accountInfo,
            string namePrefix,
            string nameSuffix,
            Dictionary<int, CandyMachineCache.CacheItem> nftLookup,
            Account updateAuthority,
            bool skipPreflight,
            IRpcClient rpcClient

        )
        {
            var nameRegex = new Regex(string.Format("{0}([0-9]+){1}", namePrefix, nameSuffix));
            var revealErrors = false;
            for (int i = 0; i < accountInfo.Count; i++) {
                var account = accountInfo[i];
                EditorUtility.DisplayProgressBar("Updating Metadatas...", string.Empty, i / (float)accountInfo.Count);
                var metadataAccount = await MetadataAccount.BuildMetadataAccount(account);
                if (!nameRegex.IsMatch(metadataAccount.metadata.name)) {
                    Debug.LogErrorFormat("Couldn't parse name for NFT: {0}", metadataAccount.metadata.name);
                    continue;
                }
                var index = GetNftIndex(metadataAccount.metadata.name, namePrefix, nameSuffix);
                var metadataKey = PDALookup.FindMetadataPDA(new(metadataAccount.mint));
                if (nftLookup.TryGetValue(index, out var cacheItem)) {
                    var newUri = cacheItem.metadataLink;
                    var newName = cacheItem.name;
                    var txResult = await UpdateMetadataValue(
                        metadataKey,
                        updateAuthority,
                        metadataAccount,
                        newUri,
                        newName,
                        rpcClient,
                        skipPreflight
                    );
                    revealErrors = txResult == null;
                }
            }
            EditorUtility.ClearProgressBar();
            return revealErrors;
        }

        private static async Task<string> UpdateMetadataValue(
            PublicKey metadataKey,
            Account updateAuthority,
            MetadataAccount metadataAccount,
            string uri,
            string name,
            IRpcClient rpcClient,
            bool skipPreflight
        )
        {
            if (uri != metadataAccount.metadata.uri) 
            {
                metadataAccount.metadata.uri = uri;
                metadataAccount.metadata.name = name;
                var newMetadata = new Metadata() {
                    collection = metadataAccount.metadata.collectionLink,
                    creators = metadataAccount.metadata.creators.ToList(),
                    name = name,
                    programmableConfig = metadataAccount.metadata.programmableConfig,
                    sellerFeeBasisPoints = metadataAccount.metadata.sellerFeeBasisPoints,
                    symbol = metadataAccount.metadata.symbol,
                    uri = uri,
                    uses = metadataAccount.metadata.uses
                };
                var metadataIx = MetadataProgram.UpdateMetadataAccount(
                    metadataKey,
                    updateAuthority,
                    null,
                    newMetadata,
                    null
                );
                var blockHash = await rpcClient.GetRecentBlockHashAsync();
                var tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(updateAuthority)
                    .AddInstruction(metadataIx)
                    .Build(updateAuthority);
                var result = await rpcClient.SendTransactionAsync(tx, skipPreflight);
                return result.Result;
            }
            return null;
        }

        private static async Task<List<AccountKeyPair>> GetCreatorMetadataAccounts(
            PublicKey creator,
            int position,
            IRpcClient rpcClient
        )
        {
            if (position > 4) 
            {
                Debug.LogError("CM Creator position cannot be greater than 4.");
                return null;
            }
            var rpcFilter = new MemCmp() {
                Offset = 1 +  // key
                    32 + // update auth
                    32 + // mint
                    4 +  // name string length
                    (int)CandyMachineCommands.MAX_NAME_LEN + // name
                    4 + // uri string length
                    (int)CandyMachineCommands.MAX_URI_LEN + // uri*
                    4 + // symbol string length
                    (int)CandyMachineCommands.MAX_SYMBOL_LEN + // symbol
                    2 + // seller fee basis points
                    1 + // whether or not there is a creators vec
                    4 + // creators
                    position * // index for each creator
                    (
                        32 + // address
                        1 + // verified
                        1 // share
                    ),
                Bytes = creator
            };
            var accounts = await rpcClient.GetProgramAccountsAsync(
                MetadataProgram.ProgramIdKey,
                memCmpList: new List<MemCmp>() { rpcFilter }
            );
            return accounts.Result;
        }

        private static int GetNftIndex(string name, string prefix, string suffix)
        {
            var newName = name;
            if (prefix != string.Empty) 
            {
                newName = newName.Replace(prefix, "");
            }
            if (suffix != string.Empty) 
            {
                newName = newName.Replace(suffix, "");
            }
            return int.Parse(newName);
        }

        #endregion

        #region Withdraw

        internal static async void Withdraw(
            PublicKey candyMachineKey,
            PublicKey candyGuardKey,
            string keypair,
            string rpcUrl
        )
        {
            Debug.Log("Beginning Withdrawal...");
            var rpcClient = ClientFactory.GetClient(rpcUrl);
            var keyPairJson = File.ReadAllText(keypair);
            var keyPairBytes = JsonConvert.DeserializeObject<byte[]>(keyPairJson);
            var wallet = new Wallet.Wallet(keyPairBytes, string.Empty, SeedMode.Bip39);
            var cmWithdrawTx = await CandyMachineCommands.Withdraw(wallet.Account, candyMachineKey, rpcClient);
            if (cmWithdrawTx == null) {
                Debug.LogError("Withdrawal failed!");
            }
            else 
            {
                Debug.Log("Withdraw Completed!");
            }
            if (candyGuardKey != null) 
            {
                var cgWithdrawTx = await CandyMachineCommands.WithdrawGuards(wallet.Account, candyGuardKey, rpcClient);
                if (cgWithdrawTx == null) {
                    Debug.LogError("Guard Withdrawal failed!");
                }
                else {
                    Debug.Log("Guard Withdraw Completed!");
                }
            }
        }

        #endregion
    }
}
