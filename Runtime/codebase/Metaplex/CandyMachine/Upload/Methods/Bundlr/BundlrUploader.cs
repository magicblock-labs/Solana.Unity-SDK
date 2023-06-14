using System.Threading.Tasks;
using UnityEngine;

namespace Solana.Unity.SDK.Metaplex
{
    public class BundlrUploader : MetaplexParallelAssetUploader
    {

        #region MetaplexParallelAssetUploader

        public override Task Prepare() 
        {
            throw new System.NotImplementedException();
        }

        protected override Task<(int, string)> UploadAsset(LocalMetaplexAsset asset)
        {
            return Task.Run(delegate { Debug.Log("Asset Uploaded"); return (asset.AssetId, ""); });
        }

        #endregion
    }
}
