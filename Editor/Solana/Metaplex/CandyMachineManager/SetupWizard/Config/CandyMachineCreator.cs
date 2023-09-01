using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    /// <summary>
    /// A serializable representation of the Creator struct for a CandyMachine 
    /// configuration.
    /// 
    /// Used for creating configurations with a setup wizard.
    /// </summary>
    [Serializable]
    internal class CandyMachineCreator
    {
        [SerializeField, JsonProperty]
        internal string address;

        [SerializeField, JsonProperty]
        internal byte share;

        internal Unity.Metaplex.Candymachine.Types.Creator ToCandyMachineCreator()
        {
            return new() {
                Address = new(address),
                Verified = true,
                PercentageShare = share
            };
        }
    }
}
