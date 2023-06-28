using Newtonsoft.Json;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor 
{
    public class CandyMachineManager : EditorWindow 
    {

        #region Types

        private struct CandyMachine
        {
            internal CandyMachineConfiguration config;
            internal CandyMachineCache cache;
            internal MetaplexEditorUtility.CandyMachineState state;

            internal CandyMachine(
                CandyMachineConfiguration config, 
                CandyMachineCache cache, 
                MetaplexEditorUtility.CandyMachineState state
            )
            {
                this.config = config;
                this.cache = cache;
                this.state = state;
            }
        }

        #endregion

        #region Properties

        [SerializeField]
        private string configLocation;

        [SerializeField]
        private string rpc;

        [SerializeField]
        private string keypairLocation;

        [SerializeField]
        private bool showCandyMachines = false;

        Vector2 scrollViewPosition = Vector2.zero;

        CandyMachine[] candyMachines;

        #endregion

        #region MenuItem

        [MenuItem("Solana/Metaplex/Candy Machine")]
        public static void ShowWindow() {
            GetWindow(typeof(CandyMachineManager), false, "Candy Machine Manager");
        }

        #endregion

        #region Unity Messages

        private void OnGUI() 
        {
            keypairLocation = SolanaEditorUtility.FileSelectField(
                "Keypair",
                keypairLocation,
                false,
                "Select a valid Keypair",
                "json"
            );
            configLocation = SolanaEditorUtility.FileSelectField(
                "Config Location",
                configLocation,
                true,
                "Select a config folder"
            );
            rpc = SolanaEditorUtility.RPCField(rpc);
            CandyMachineScrollView();
            if (GUILayout.Button("Create new Candy Machine")) 
            {
                CandyMachineSetupWizard.OpenNew(configLocation);
            }
            if (GUILayout.Button("Import Candy Machine")) 
            {
                ImportConfig();
                FetchCandyMachines();
            }
            if (GUILayout.Button("Refresh Candy Machines")) 
            {
                FetchCandyMachines();
            }
        }

        private void OnEnable()
        {
            var data = EditorPrefs.GetString(typeof(CandyMachineManager).Name, JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);
            FetchCandyMachines();
        }

        private void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(typeof(CandyMachineManager).Name, data);
        }

        #endregion

        #region Private

        private void FetchCandyMachines()
        {
            if (configLocation == null)
            {
                Debug.LogError("Could not fetch CandyMachines, please set a config location");
                return;
            }
            Debug.Log(string.Format("Fetching CandyMachines from {0}.", configLocation));
            var configGUIDS = AssetDatabase.FindAssets("t: candyMachineConfiguration", new[] { configLocation });
            candyMachines = configGUIDS.Select(guid => {
                var configPath = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<CandyMachineConfiguration>(configPath);
                CandyMachineCache cache = null;
                if (config.cacheFilePath != string.Empty) 
                {
                    var cacheJson = File.ReadAllText(config.cacheFilePath);
                    cache = JsonConvert.DeserializeObject<CandyMachineCache>(cacheJson);
                }
                return new CandyMachine(config, cache, new());
            }).ToArray();
        }

        private void ImportConfig()
        {
            if (configLocation == null) 
            {
                Debug.LogError("Select a config location before importing CandyMachines.");
                return;
            }
            var filePath = EditorUtility.OpenFilePanel("Import CandyMachine Config", string.Empty, "json");
            Debug.Log(string.Format("Importing config from {0}.", "filepath"));
            string json = File.ReadAllText(filePath);
            var config = CreateInstance<CandyMachineConfiguration>();
            config.LoadFromJson(json);
            var savePath = Path.Combine(
                configLocation,
                Path.GetFileNameWithoutExtension(filePath) + ".asset"
            );
            AssetDatabase.CreateAsset(config, savePath);
        }

        private void CandyMachineScrollView() 
        {
            showCandyMachines = EditorGUILayout.BeginFoldoutHeaderGroup(showCandyMachines, "Existing Candy Machines");
            if (showCandyMachines) 
            {
                scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition, SolanaEditorUtility.scrollViewStyle);
                if (candyMachines != null) 
                {
                    foreach (var candyMachine in candyMachines) 
                    {
                        MetaplexEditorUtility.CandyMachineField(
                            candyMachine.state,
                            candyMachine.cache,
                            candyMachine.config,
                            keypairLocation,
                            rpc
                        );
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion
    }
}
