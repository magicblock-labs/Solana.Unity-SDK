namespace Solana.Unity.SDK.Editor
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

        public int AssetId { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public AssetType Type { get; set; }
        public string ContentType { get; set; }

        #endregion
    }
}
