using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solana.Unity.SDK.Metaplex 
{
    public interface IMetaplexAssetUploader 
    {

        #region Public

        public Task Prepare();

        public Task Upload(
            Account keypair,
            string rpcUrl,
            CandyMachineCache cache,
            LocalMetaplexAsset.AssetType assetType,
            Stack<LocalMetaplexAsset> assets
        );

        #endregion
    }
}
