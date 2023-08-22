using Newtonsoft.Json;
using Solana.Unity.Metaplex.Candymachine.Types;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    [Serializable, CreateAssetMenu(menuName = "Config"), JsonObject(MemberSerialization.OptIn)]
    internal class CandyMachineConfiguration : ConfigurableObject
    {

        #region Properties

        internal override bool IsValidConfiguration {
            get {
                return creators != null;
            }
        }

        #endregion

        #region Fields

        [JsonProperty, SerializeField]
        [SetupQuestion("How many NFTs will be in your CandyMachine?"), Tooltip("The number of NFTs in your CandyMachine.")]
        internal int number;

        [JsonProperty, SerializeField]
        [SetupQuestion("What is the symbol of your collection? Leave empty for no symbol."), Tooltip("The symbol of this collection.")]
        internal string symbol;

        [JsonProperty, SerializeField]
        [SetupQuestion("What is the seller fee basis points?"), Tooltip("The seller fee basis points charged when a token from this collection is traded.")]
        internal int sellerFeeBasisPoints;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want to use a sequential mint index generation? We recommend you choose no."), Tooltip("Whether tokens should mint sequentially.")]
        internal bool isSequential;

        [JsonProperty, SerializeField]
        [SetupQuestion("Enter your hidden settings, leave disabled if you don't wish to use hidden settings."), Tooltip("The hidden settings for this collection.")]
        internal CandyMachineHiddenSettings hiddenSettings;

        [JsonProperty, SerializeField]
        [SetupQuestion("Enter the list of Creators below, total royalty share must add to 100."), Tooltip("The creators of this collection, and their seller fee basis points share in %.")]
        internal CandyMachineCreator[] creators;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want your NFTs to remain mutable? We HIGHLY recommend you choose yes."), Tooltip("Whether you will be able to update token metadata after minting.")]
        internal bool isMutable = true;

        [JsonProperty, SerializeField]
        [SetupQuestion("Add your default guards and guard groups below, leave empty for no guards."), Tooltip("The guard groups for this CandyMachine.")]
        internal CandyMachineGuards guards;

        [SerializeField]
        internal string cacheFilePath;

        #endregion

        #region Internal

        internal CandyMachineData ToCandyMachineData(CandyMachineCache cache)
        {
            return new() {
                Symbol = symbol,
                SellerFeeBasisPoints = (ushort)sellerFeeBasisPoints,
                MaxSupply = 0,
                IsMutable = isMutable,
                Creators = creators.Select(creator => {
                    return creator.ToCandyMachineCreator();
                }).ToArray(),
                HiddenSettings = hiddenSettings.ToCandyMachineHiddenSettings(),
                ConfigLineSettings = GetConfigLineSettings(cache, isSequential),
                ItemsAvailable = (ulong)number
            };
        }

        internal override void LoadFromJson(string json)
        {
            base.LoadFromJson(json);
            if (guards != null) {
                guards.defaultGuards.SetGuardsEnabled();
                foreach (var group in guards.groups) {
                    group.guards.SetGuardsEnabled();
                }
            }
        }

        #endregion

        #region Private

        private ConfigLineSettings GetConfigLineSettings(CandyMachineCache cache, bool isSequential)
        {
            var namePair = new string[] { string.Empty, string.Empty, string.Empty };
            var uriPair = new string[] { string.Empty, string.Empty, string.Empty };

            foreach (var (_, item) in cache.Items) 
            {
                ComparePrefixPair(item.name, namePair);
                ComparePrefixPair(item.metadataLink, uriPair);
            }
            var namePrefix = GetCommonPrefix(namePair[0], namePair[1]);
            var uriPrefix = GetCommonPrefix(uriPair[0], uriPair[1]);

            return new ConfigLineSettings() { 
                IsSequential = isSequential,
                NameLength = (uint)(namePair[2].Length - namePrefix.Length),
                PrefixName = namePrefix,
                PrefixUri = uriPrefix,
                UriLength = (uint)(uriPair[2].Length - uriPrefix.Length)
            };
        }

        private void ComparePrefixPair(string value, string[] pair)
        {
            if (pair[0] == string.Empty || value.CompareTo(pair[0]) == -1) 
            {
                pair[0] = value;
            }

            if (value.CompareTo(pair[1]) == 1) {
                pair[1] = value;
            }

            if (value.Length > pair[2].Length) {
                pair[2] = value;
            }
        }

        private string GetCommonPrefix(string value1, string value2)
        {
            var bytes1 = Encoding.UTF8.GetBytes(value1);
            var bytes2 = Encoding.UTF8.GetBytes(value2);
            var index = 0;

            while (index < bytes1.Length && index < bytes2.Length && bytes1[index] == bytes2[index]) {
                index++;
            };

            return value1.Substring(0, index);
        }

        #endregion

        #region ShouldSerialize

        public bool ShouldSerializehiddenSettings() => hiddenSettings.useHiddenSettings;

        #endregion
    }
}
