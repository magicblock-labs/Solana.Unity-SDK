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

        #region Properties

        [JsonProperty, SerializeField]
        [SetupQuestion("How many NFTs will be in your CandyMachine?"), Tooltip("The number of NFTs in your CandyMachine.")]
        private int amount;

        [JsonProperty, SerializeField]
        [SetupQuestion("What is the symbol of your collection? Leave empty for no symbol."), Tooltip("The symbol of this collection.")]
        private string symbol;       
        
        [JsonProperty, SerializeField]
        [SetupQuestion("What is the seller fee basis points?"), Tooltip("The seller fee basis points charged when a token from this collection is traded.")]
        private int sellerFeeBasisPoints;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want to use a sequential mint index generation? We recommend you choose no."), Tooltip("Whether tokens should mint sequentially.")]
        private bool sequential;

        [JsonProperty, SerializeField]
        [SetupQuestion("Enter the list of Creators below, total royalty share must add to 100."), Tooltip("The creators of this collection, and their seller fee basis points share in %.")]
        private CandyMachineCreator[] creators;

        [JsonProperty, SerializeField]
        [SetupQuestion("Do you want your NFTs to remain mutable? We HIGHLY recommend you choose yes."), Tooltip("Whether you will be able to update token metadata after minting.")]
        private bool isMutable;

        [JsonProperty, SerializeField]
        private CandyMachineGuards guards;

        #endregion

        #region Public

        public CandyMachineData ToCandyMachineData()
        {
            return new() {
                Uuid = null,
                Symbol = symbol,
                SellerFeeBasisPoints = (ushort)sellerFeeBasisPoints,
                MaxSupply = (ulong)amount,
                IsMutable = isMutable,
                Creators = creators.Select(creator => {
                    return creator.ToCandyMachineCreator();
                }).ToArray(),
                HiddenSettings = null,
                ItemsAvailable = (ulong)amount
            };
        }

        #endregion
    }
}
