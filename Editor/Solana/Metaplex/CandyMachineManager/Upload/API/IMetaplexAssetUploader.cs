using Newtonsoft.Json;
using Solana.Unity.Metaplex.Utilities.Json;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Solana.Unity.SDK.Editor
{
    internal interface IMetaplexAssetUploader 
    {

        #region Types

        internal enum UploadMethod
        {
            Bundlr
        }

        #endregion

        #region Static

        internal static string GetUpdatedMetadata(string metadataFilePath, string imageLink, string animationLink)
        {
            var metadataJson = File.ReadAllText(metadataFilePath);
            var metadata = JsonConvert.DeserializeObject<MetaplexTokenStandard>(metadataJson);
            metadata.properties.files = metadata.properties.files.Select((file) => {
                if (file.uri == metadata.default_image) 
                {
                    file.uri = imageLink;
                }
                var hasAnimation = animationLink != string.Empty && metadata.animation_url != string.Empty;
                if (hasAnimation && file.uri == metadata.animation_url) 
                {
                    file.uri = animationLink;
                }
                return file;
            }).ToList();
            metadata.default_image = imageLink;
            metadata.animation_url = animationLink;
            return JsonConvert.SerializeObject(metadata);
        }

        #endregion

        #region Public

        public Task Prepare(
            Account payer,
            CandyMachineController.UploadQueue uploadQueue,
            Dictionary<int, CandyMachineCache.CacheItem> assetPairs
        );

        public Task Upload(
            string rpcUrl,
            CandyMachineConfiguration config,
            CandyMachineCache cache,
            LocalMetaplexAsset.AssetType assetType,
            Stack<LocalMetaplexAsset> assets
        );

        #endregion
    }
}
