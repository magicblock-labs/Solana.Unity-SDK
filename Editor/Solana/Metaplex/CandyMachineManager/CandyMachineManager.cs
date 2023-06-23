using Newtonsoft.Json;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor 
{
    public class CandyMachineManager : EditorWindow 
    {

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

        (CandyMachineConfiguration config, CandyMachineCache cache)[] candyMachines;

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
            // Then we apply them to this window
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
                var cachePath = Path.Combine(configLocation, config.name + "-cache") + ".json";
                var cacheJson = AssetDatabase.LoadAssetAtPath<TextAsset>(cachePath);
                if (cacheJson != null) 
                {
                    var cache = JsonConvert.DeserializeObject<CandyMachineCache>(cacheJson.text);
                    return (config, cache);
                }
                var newCache = new CandyMachineCache(cachePath);
                newCache.SyncFile();
                return (config, newCache);
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
                    foreach (var (config, cache) in candyMachines) 
                    {
                        MetaplexEditorUtility.CandyMachineField(
                            cache,
                            config,
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
