using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [Serializable]
    internal class CandyMachineCreator
    {
        [SerializeField, JsonProperty]
        internal string publicKey;

        [SerializeField, JsonProperty]
        internal byte share;

        internal Unity.Metaplex.Candymachine.Types.Creator ToCandyMachineCreator()
        {
            return new() {
                Address = new(publicKey),
                Share = share
            };
        }
    }
}
