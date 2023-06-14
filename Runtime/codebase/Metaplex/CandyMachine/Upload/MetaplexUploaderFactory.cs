namespace Solana.Unity.SDK.Metaplex
{
    public static class MetaplexUploaderFactory
    {

        #region Types

        public enum CandyMachineUploadMethod
        {
            Bundlr
        }

        #endregion

        #region Public

        public static IMetaplexAssetUploader New(CandyMachineUploadMethod uploadMethod)
        {
            return uploadMethod switch {
                CandyMachineUploadMethod.Bundlr => new BundlrUploader(),
                _ => new BundlrUploader(),
            };
        }

        #endregion
    }
}
