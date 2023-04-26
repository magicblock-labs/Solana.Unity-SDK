namespace Solana.Unity.SDK.Metaplex
{
    public struct LocalMetaplexAsset
    {

        #region Types

        public enum AssetType
        {
            Image,
            Metadata,
            Animation
        }

        #endregion

        #region Properties

        public string AssetId { get; private set; }
        public string Name { get; private set; }
        public string Content { get; private set; }
        public AssetType Type { get; private set; }
        public string ContentType { get; private set; }

        #endregion
    }
}
