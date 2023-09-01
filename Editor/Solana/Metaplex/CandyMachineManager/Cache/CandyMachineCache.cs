using Newtonsoft.Json;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class CandyMachineCache 
    {

        #region Types

        public class CacheItem
        {

            #region Fields

            [JsonProperty]
            public string name;

            [JsonProperty("image_hash")]
            public string imageHash;

            [JsonProperty("image_link")]
            public string imageLink;

            [JsonProperty("metadata_hash")]
            public string metadataHash;

            [JsonProperty("metadata_link")]
            public string metadataLink;

            [JsonProperty]
            public bool onChain;

            [JsonProperty("animation_hash")]
            public string animationHash;

            [JsonProperty("animation_link")]
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

        [JsonObject(MemberSerialization.OptIn)]
        public class CacheInfo
        {

            #region Properties

            [JsonProperty("candyMachine")]
            public string CandyMachine { get; set; }

            [JsonProperty("candyGuard")]
            public string CandyGuard { get; set; }

            [JsonProperty("candyMachineCreator")]
            public string Creator { get; set; }

            [JsonProperty("collectionMint")]
            public string CollectionMint { get; set; }

            public PublicKey CandyGuardKey {
                get {
                    if (CandyGuard == null || CandyGuard == string.Empty) {
                        return null;
                    }
                    return new(CandyGuard);
                }
            }

            public PublicKey CandyMachineKey {
                get {
                    if (CandyMachine == null || CandyMachine == string.Empty) {
                        return null;
                    }
                    return new(CandyMachine);
                }
            }

            public PublicKey CollectionMintKey {
                get {
                    if (CollectionMint == null || CollectionMint == string.Empty) {
                        return null;
                    }
                    return new(CollectionMint);
                }
            }

            #endregion

        }

        #endregion

        #region Properties

        [JsonProperty("program")]
        public CacheInfo Info { get; set; }

        [JsonProperty("items")]
        public Dictionary<int, CacheItem> Items { get; set; }

        #endregion

        #region Constructors

        public CandyMachineCache()
        {
            Info = new CacheInfo();
            Items = new Dictionary<int, CacheItem>();
        }

        #endregion

        #region Public

        public void SyncFile(string path)
        {
            Debug.Log("Syncing Cache file.");
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(path, json);
            Debug.Log("Cache file saved.");
        }

        #endregion
    }
}
