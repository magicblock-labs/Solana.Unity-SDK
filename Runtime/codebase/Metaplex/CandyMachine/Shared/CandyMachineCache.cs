using CandyMachineV2;
using Newtonsoft.Json;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Solana.Unity.SDK.Metaplex 
{
    public class CandyMachineCache 
    {

        #region Types

        public class CacheItem
        {

            #region Properties

            public string name;
            public string imageHash;
            public string imageLink;
            public string metadataHash;
            public string metadataLink;
            public bool onChain;
            public string animationHash;
            public string animationLink;

            #endregion

            #region Constructors

            public CacheItem(
                string name,
                string imageHash,
                string imageLink,
                string metadataHash,
                string metadataLink,
                bool onChain,
                string animationHash,
                string animationLink
            )
            {
                this.name = name;
                this.imageHash = imageHash;
                this.imageLink = imageLink;
                this.metadataHash = metadataHash;
                this.metadataLink = metadataLink;
                this.onChain = onChain;
                this.animationHash = animationHash;
                this.animationLink = animationLink;
            }

            #endregion

            #region Public

            /// <summary>
            /// Converts this item cache into a <see cref="ConfigLine"/> to be used for upload operations.
            /// </summary>
            /// <returns>The mapped <see cref="ConfigLine"/> or null if this item is not yet uploaded.</returns>
            public ConfigLine ToConfigLine()
            {
                if (onChain) {
                    return new() {
                        Name = name,
                        Uri = metadataLink
                    };
                }
                return null;
            }

            #endregion
        }

        public class CacheInfo
        {

            #region Properties

            public PublicKey CandyMachine { get; private set; }
            public PublicKey CandyGuard { get; private set; }
            public PublicKey Creator { get; private set; }
            public PublicKey CollectionMint { get; set; }

            #endregion

        }

        #endregion

        #region Properties

        public CacheInfo Info { get; private set; }
        public Dictionary<string, CacheItem> Items { get; private set; }
        public string FilePath { get; private set; }

        #endregion

        #region Constructors

        public CandyMachineCache(string filePath)
        {
            Info = new CacheInfo();
            Items = new Dictionary<string, CacheItem>();
            FilePath = filePath;
        }

        #endregion

        #region Public

        public void SyncFile()
        {

        }

        #endregion

        #region Static

        public static CandyMachineCache LoadFromPath(string cachePath, bool create = false)
        {
            if (File.Exists(cachePath)) 
            {
                Debug.Log(string.Format("Loading cache from path {0}...", cachePath));
                using StreamReader reader = new(cachePath);
                var cacheJson = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<CandyMachineCache>(cacheJson);
            }

            if (create) 
            {
                Debug.Log(string.Format("Creating cache at path {0}...", cachePath));
                return new(cachePath);
            }

            Debug.LogError(string.Format("Cache file not found at {0}.", cachePath));
            return null;
        }

        #endregion
    }
}
