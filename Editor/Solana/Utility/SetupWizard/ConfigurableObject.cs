using Newtonsoft.Json;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal abstract class ConfigurableObject : ScriptableObject
    {

        #region Properties

        internal abstract bool IsValidConfiguration { get; }

        #endregion

        #region Internal

        internal virtual void LoadFromJson(string json)
        {
            JsonConvert.PopulateObject(json, this);
        }

        #endregion

    }
}
