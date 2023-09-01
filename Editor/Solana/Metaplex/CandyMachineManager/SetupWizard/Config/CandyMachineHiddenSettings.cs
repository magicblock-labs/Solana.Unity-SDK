using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    /// <summary>
    /// A serializable representation of the HiddenSettings struct for a CandyMachine 
    /// configuration.
    /// 
    /// Used for creating configurations with a setup wizard.
    /// </summary>
    [Serializable]
    internal class CandyMachineHiddenSettings
    {

        [SerializeField]
        internal bool useHiddenSettings;

        [SerializeField, JsonProperty, ShowWhen("useHiddenSettings")]
        private string name;

        [SerializeField, JsonProperty, ShowWhen("useHiddenSettings")]
        private string uri;

        [SerializeField, JsonProperty, ShowWhen("useHiddenSettings")]
        private string base64Hash;

        internal Unity.Metaplex.Candymachine.Types.HiddenSettings ToCandyMachineHiddenSettings()
        {
            if (!useHiddenSettings) return null;
            return new() {
                Name = name,
                Uri = uri,
                Hash = Convert.FromBase64String(base64Hash)
            };
        }
    }
}
