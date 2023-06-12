using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [Serializable]
    internal class CandyMachineGuards
    {
        [SerializeField, JsonProperty("default")]
        internal CandyMachineGuardSet defaultGuards;

        [SerializeField, JsonProperty]
        internal CandyMachineGuardGroup[] groups;
    }
}
