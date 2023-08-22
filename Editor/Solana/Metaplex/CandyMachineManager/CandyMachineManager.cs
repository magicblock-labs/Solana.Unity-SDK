using Newtonsoft.Json;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Rpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using static Solana.Unity.SDK.Editor.MetaplexEditorUtility;

namespace Solana.Unity.SDK.Editor 
{
    public class CandyMachineManager : EditorWindow 
    {

        #region Static

        private static readonly string DEFAULT_KEYPAIR = "/.config/solana/id.json";
        private static readonly string DEFAULT_CONFIG_LOCATION = "Assets/CandyMachine/Configs";
        private static readonly string DEFAULT_RPC = "https://api.devnet.solana.com";

        #endregion

        #region Fields

        [SerializeField]
        private string configLocation;

        [SerializeField]
        private string rpc;

        [SerializeField]
        private string keypairLocation;

        [SerializeField]
        private bool showCandyMachines = false;

        private Vector2 scrollViewPosition = Vector2.zero;

        private List<CandyMachine> candyMachines;

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
                CandyMachineSetupWizard.OpenNew(configLocation, FetchCandyMachines);
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
            if (rpc == null) 
            {
                rpc = DEFAULT_RPC;
            }

            if (configLocation == null) 
            {
                var configPath = Path.GetFullPath(DEFAULT_CONFIG_LOCATION, Application.dataPath);
                if (!Directory.Exists(configPath)) { 
                    Directory.CreateDirectory(configPath);
                }
                configLocation = DEFAULT_CONFIG_LOCATION;
            }
            if (keypairLocation == null || keypairLocation == string.Empty)
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + DEFAULT_KEYPAIR;
                keypairLocation = path;
            }
            FetchCandyMachines();
        }

        private void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(typeof(CandyMachineManager).Name, data);
        }

        #endregion

        #region Private

        private async void FetchCandyMachines()
        {
            if (configLocation == null)
            {
                Debug.LogError("Could not fetch CandyMachines, please set a config location");
                return;
            }
            Debug.Log(string.Format("Fetching CandyMachines from {0}.", configLocation));
            var configGUIDS = AssetDatabase.FindAssets("t: candyMachineConfiguration", new[] { configLocation });
            candyMachines = new();
            for (int i = 0; i < configGUIDS.Length; i++) 
            {
                var progress = i / (float)configGUIDS.Length;
                EditorUtility.DisplayProgressBar("Refreshing CandyMachines...", string.Empty, progress);
                var guid = configGUIDS[i];
                var configPath = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<CandyMachineConfiguration>(configPath);
                CandyMachineCache cache = null;
                RenderTexture collectionIcon = null;
                if (config.cacheFilePath != string.Empty && config.cacheFilePath != null) {
                    var cacheJson = File.ReadAllText(config.cacheFilePath);
                    cache = JsonConvert.DeserializeObject<CandyMachineCache>(cacheJson);
                    collectionIcon = await LoadCollectionIcon(cache, rpc);
                }
                candyMachines.Add(new(config, cache, new(), collectionIcon));
            }
            EditorUtility.ClearProgressBar();
        }

        private async Task<RenderTexture> LoadCollectionIcon(CandyMachineCache cache, string rpc)
        {
            if (cache.Info.CollectionMint != null && cache.Info.CollectionMint != string.Empty) {
                var rpcClient = ClientFactory.GetClient(rpc);
                var metadata = await MetadataAccount.GetAccount(rpcClient, new(cache.Info.CollectionMint));
                using var webClient = new WebClient();
                if (metadata.offchainData?.default_image == null) return null;
                var imageBytes = await webClient.DownloadDataTaskAsync(metadata.offchainData.default_image);
                var icon = new Texture2D(124, 124);
                RenderTexture collectionIcon = new(icon.width, icon.height, 0);
                icon.LoadImage(imageBytes);
                Graphics.Blit(icon, collectionIcon);
                return collectionIcon;
            }
            return null;
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
                        CandyMachineField(
                            candyMachine,
                            keypairLocation,
                            rpc,
                            FetchCandyMachines
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
