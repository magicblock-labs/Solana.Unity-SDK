using Solana.Unity.Rpc;
using Solana.Unity.Wallet;

namespace Solana.Unity.SDK.Editor
{
    internal static class MetaplexUploaderFactory
    {

        #region Public

        internal static IMetaplexAssetUploader New(
            IRpcClient client, 
            IMetaplexAssetUploader.UploadMethod uploadMethod
        )
        {
            return uploadMethod switch {
                IMetaplexAssetUploader.UploadMethod.Bundlr => new BundlrUploader(client),
                _ => throw new System.Exception("No upload method chosen."),
            };
        }

        #endregion
    }
}
