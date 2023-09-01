using Newtonsoft.Json;
using Solana.Unity.SDK.Metaplex;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [Serializable]
    internal class CandyMachineGuards
    {

        #region Fields

        [SerializeField, JsonProperty("default")]
        internal CandyMachineGuardSet defaultGuards;

        [SerializeField, JsonProperty]
        internal CandyMachineGuardGroup[] groups;

        #endregion

    }
}
