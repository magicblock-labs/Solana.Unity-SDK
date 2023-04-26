using System.Collections;
using System.Collections.Generic;
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

        public override Task<(string, string)> UploadAsset(LocalMetaplexAsset asset)
        {
            return Task.Run(delegate { Debug.Log("Asset Uploaded"); return ("", ""); });
        }

        #endregion
    }
}
