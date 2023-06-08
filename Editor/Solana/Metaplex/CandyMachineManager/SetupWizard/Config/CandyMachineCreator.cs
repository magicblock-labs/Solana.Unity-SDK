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
        private string publicKey;

        [SerializeField, JsonProperty]
        private byte share;

        internal Unity.Metaplex.Candymachine.Types.Creator ToCandyMachineCreator()
        {
            return new() {
                Address = new(publicKey),
                Verified = true,
                PercentageShare = share
            };
        }
    }
}
