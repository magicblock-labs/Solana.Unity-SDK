using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    [CustomEditor(typeof(CandyMachineConfiguration))]
    public class CandyMachineConfigurationEditor : UnityEditor.Editor
    {
        #region Properties

        private CandyMachineConfiguration config;

        #endregion

        #region Unity Messages

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Export as JSON")) 
            {
                var filePath = EditorUtility.SaveFilePanel(
                    "Export to JSON", 
                    Application.dataPath,
                    "config",
                    "json"
                );
                var json = JsonConvert.SerializeObject(config);
                File.WriteAllText(filePath, json);
            }
        }

        private void OnEnable()
        {
            config = (CandyMachineConfiguration)target;
        }

        #endregion
    }
}
