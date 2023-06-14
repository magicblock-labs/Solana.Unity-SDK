using Newtonsoft.Json;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Solana.Unity.SDK.Metaplex 
{
    [Serializable]
    public class CandyMachineCache 
    {

        #region Types

        public class CacheItem
        {

            #region Fields

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

            public PublicKey CandyMachine { get; set; }
            public PublicKey CandyGuard { get; set; }
            public PublicKey Creator { get; set; }
            public PublicKey CollectionMint { get; set; }

            #endregion

        }

        #endregion

        #region Properties

        public CacheInfo Info { get; set; }
        public Dictionary<int, CacheItem> Items { get; set; }
        public string FilePath { get; set; }

        #endregion

        #region Constructors

        public CandyMachineCache(string filePath)
        {
            Info = new CacheInfo();
            Items = new Dictionary<int, CacheItem>();
            FilePath = filePath;
        }

        #endregion

        #region Public

        public void SyncFile()
        {
            Debug.Log("Syncing Cache file.");
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(FilePath, json);
            Debug.Log("Cache file saved.");
        }

        #endregion
    }
}
