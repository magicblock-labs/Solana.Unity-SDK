using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solana.Unity.SDK.Metaplex 
{
    public interface IMetaplexAssetUploader 
    {

        #region Public

        public Task Prepare();

        public Task Upload(
            CandyMachineDetails config,
            CandyMachineCache cache,
            LocalMetaplexAsset.AssetType assetType,
            Stack<LocalMetaplexAsset> assets
        );

        #endregion
    }
}
