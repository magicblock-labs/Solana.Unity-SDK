using System;
using System.Collections.Generic;
using Solana.Unity.Metaplex.NFT.Library;
using UnityEngine;
using Solana.Unity.SDK.Utility;

namespace Solana.Unity.SDK.Nft
{
    [Serializable]
    public class Attributes {
        public string trait_type;
        public string value;
    }

    [Serializable]
    public class MetaplexJsonData {
        public string name;
        public string description;
        public string previewUrl;
        public string animation_url;
        public string image;
        public Attributes[] attributes;
        public Properties properties;

    }

    [Serializable]
    public class MetaplexData
    {
        public string name;
        public string symbol;
        public string url;
        public int seller_fee_basis_points;
        public CreatorData[] creators;
        public MetaplexJsonData json;
    }

    [Serializable]
    public class CreatorData
    {
        public string address;
        public bool verified;
        public int share;
    }

    [Serializable]
    public class File
    {
        public string uri;
        public string type;
    }

    [Serializable]
    public class Properties
    {
        public File[] files;
        public string category;
        public CreatorData[] creators;
    }

    [Serializable]
    public class Metaplex
    {
        public iNftFile<Texture2D> nftImage;
        public readonly MetadataAccount data;

        public Metaplex(MetadataAccount metadataAccount)
        {
            data = metadataAccount;
        }
    }
}