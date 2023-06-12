using CandyMachineV2;
using Solana.Unity.SDK.Metaplex;
using Solana.Unity.Wallet;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor 
{
    internal static class MetaplexEditorUtility 
    {

        #region Types

        internal struct CandyMachineParams
        {
            internal bool isInitialized;
            internal CandyMachineCache cache;
            internal RpcCluster cluster;
        }

        #endregion

        #region Styling Constants

        private static readonly RectOffset candyMachineFieldPadding = new(10, 10, 5, 5);
        private static readonly RectOffset candyMachineFieldMargin = new(0, 0, 10, 10);

        private static readonly RectOffset wizardQuestionMargin = new(5, 5, 10, 10);

        #endregion

        #region GUIStyles

        private static readonly GUIStyle collectionButtonStyle = new(GUI.skin.button) {
            stretchWidth = false
        };

        private static readonly GUIStyle settingsButtonStyle = new(GUI.skin.button) {
            margin = SolanaEditorUtility.standardMargin
        };

        private static readonly GUIStyle candyMachineFieldStyle = new(GUI.skin.window) {
            stretchHeight = false,
            padding = candyMachineFieldPadding,
            margin = candyMachineFieldMargin
        };

        public static readonly GUIStyle answerFieldStyle = new() {
            alignment = TextAnchor.MiddleCenter,
            padding = SolanaEditorUtility.standardPadding,
            margin = wizardQuestionMargin
        };

        #endregion

        #region Internal

        internal static void CandyMachineField(CandyMachineParams info) {
            EditorGUILayout.BeginHorizontal(candyMachineFieldStyle);
            CollectionImage(124);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            CandyMachineDetails(info);
            CandyMachineControls(info);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region UI Components

        private static void CollectionImage(int height) {
            Texture2D defaultIcon = (Texture2D)Resources.Load("DefaultCollectionIcon");
            if (GUILayout.Button(defaultIcon, collectionButtonStyle)) {
                Debug.Log("Collection Clicked");
            }
        }

        private static void CandyMachineControls(CandyMachineParams info) {
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Edit Config", settingsButtonStyle)) 
            {
                Debug.Log("Settings Clicked");
            }
            if (info.isInitialized) 
            {
                CandyMachineControlGrid();
            } else {
                CandyMachineSetupControls();
            }
            EditorGUILayout.EndVertical();
        }

        private static void CandyMachineControlGrid() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Freeze", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            if (GUILayout.Button("Upload", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Withdraw", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            if (GUILayout.Button("Sign", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Mint", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            if (GUILayout.Button("Reveal", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private static void CandyMachineSetupControls()
        {
            if (GUILayout.Button("Initialize CandyMachine", settingsButtonStyle)) {
                InitializeCandyMachine();
            }
            if (GUILayout.Button("Upload Assets", settingsButtonStyle)) {
                UploadCandyMachineAssets();
            }
        }

        private static void CandyMachineDetails(CandyMachineParams info) {
            EditorGUILayout.BeginVertical();
            {
                if (info.cache != null) 
                {
                    SolanaEditorUtility.StaticTextProperty("Address", info.cache.Info.CandyMachine);
                    SolanaEditorUtility.StaticTextProperty("Authority", info.cache.Info.Creator);
                }
                SolanaEditorUtility.StaticTextProperty("Cluster", info.cluster.ToString());
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Private

        private static async void InitializeCandyMachine()
        {
            Debug.Log("Initializing CandyMachine...");
            EditorUtility.DisplayProgressBar("Candy Machine Manager", "Initializing CandyMachine...", 0.5f);
            var candyMachineAccount = new Account();
            /*await CandyMachineCommands.CreateCollection(
                candyMachineAccount
            );*/
            // await CandyMachineCommands.InitializeCandyMachine();
            await Task.Delay(5000);
            EditorUtility.ClearProgressBar();
        }

        private static async void UploadCandyMachineAssets()
        {

        }

        #endregion
    }
}
