using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal abstract class MetaplexParallelAssetUploader : IMetaplexAssetUploader
    {

        #region Properties

        private const int PARALLEL_LIMIT = 45;

        #endregion

        #region IMetaplexAssetUploader

        public abstract Task Prepare(
            Account payer,
            CandyMachineController.UploadQueue uploadQueue,
            Dictionary<int, CandyMachineCache.CacheItem> assetPairs
        );

        public virtual async Task Upload(
            string rpcUrl,
            CandyMachineConfiguration config,
            CandyMachineCache cache,
            LocalMetaplexAsset.AssetType assetType,
            Stack<LocalMetaplexAsset> assets
        )
        {
            List<Task<(int, string)>> uploads = new();
            
            while (assets.Count > 0 && uploads.Count < PARALLEL_LIMIT)
            {
                var task = UploadAsset(assets.Pop());
                uploads.Add(task);
            }

            while (uploads.Count > 0)
            {
                var completed = await Task.WhenAny(uploads);
                uploads.Remove(completed);
                var (id, link) = completed.Result;
                if (link != null) 
                {
                    switch (assetType) {
                        case LocalMetaplexAsset.AssetType.Metadata:
                            cache.Items[id].metadataLink = link;
                            break;
                        case LocalMetaplexAsset.AssetType.Animation:
                            cache.Items[id].animationLink = link;
                            break;
                        case LocalMetaplexAsset.AssetType.Image:
                            cache.Items[id].imageLink = link;
                            break;
                    }
                    Debug.Log("Asset uploaded.");
                } 
                else 
                {
                    Debug.LogErrorFormat("Asset {0} failed to upload.", id);
                }

                if (assets.Count > 0 && PARALLEL_LIMIT - uploads.Count > PARALLEL_LIMIT / 2) 
                {
                    while (assets.Count > 0 && uploads.Count < PARALLEL_LIMIT) {
                        var task = UploadAsset(assets.Pop());
                        uploads.Add(task);
                    }
                }
            }

            cache.SyncFile(config.cacheFilePath);
            return;
        }

        #endregion

        #region Protected

        protected abstract Task<(int, string)> UploadAsset(LocalMetaplexAsset asset);

        #endregion
    }
}
