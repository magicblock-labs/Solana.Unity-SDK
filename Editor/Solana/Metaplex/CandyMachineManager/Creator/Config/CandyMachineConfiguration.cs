using Newtonsoft.Json;
using Solana.Unity.Metaplex.Candymachine.Types;
using System;
using System.Linq;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    [Serializable, CreateAssetMenu(menuName = "Config"), JsonObject(MemberSerialization.OptIn)]
    public class CandyMachineConfiguration : ScriptableObject
    {

        #region Types

        [Serializable]
        private class Creator
        {
            [SerializeField]
            internal string publicKey;

            [SerializeField]
            internal byte share;

            internal Unity.Metaplex.Candymachine.Types.Creator ToCandyMachineCreator()
            {
                return new() {
                    Address = new(publicKey),
                    Share = share
                };
            }
        }

        #endregion

        #region Properties

        [JsonProperty, SerializeField]
        [SetupQuestion("How many NFTs will be in your CandyMachine?")]
        private int amount;

        [JsonProperty, SerializeField]
        [SetupQuestion("What is the symbol of your collection? Leave empty for no symbol.")]
        private string symbol;       
        
        [JsonProperty, SerializeField]
        [SetupQuestion("What is the seller fee basis points?")]
        private int sellerFeeBasisPoints;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want to use a sequential mint index generation? We recommend you choose no.")]
        private bool sequential;

        [JsonProperty, SerializeField]
        [SetupQuestion("Enter the list of Creators below, total royalty share must add to 100.")]
        private Creator[] creators;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want your NFTs to remain mutable? We HIGHLY recommend you choose yes.")]
        private bool isMutable;

        #endregion

        #region Public

        public CandyMachineData ToCandyMachineData()
        {
            return new() {
                Uuid = null,
                Price = 0,
                Symbol = symbol,
                SellerFeeBasisPoints = (ushort)sellerFeeBasisPoints,
                MaxSupply = (ulong)amount,
                IsMutable = isMutable,
                RetainAuthority = true,
                GoLiveDate = null,
                EndSettings = null,
                Creators = creators.Select(creator => {
                    return creator.ToCandyMachineCreator();
                }).ToArray(),
                HiddenSettings = null,
                WhitelistMintSettings = null,
                ItemsAvailable = (ulong)amount,
                Gatekeeper = null
            };
        }

        #endregion
    }
}
