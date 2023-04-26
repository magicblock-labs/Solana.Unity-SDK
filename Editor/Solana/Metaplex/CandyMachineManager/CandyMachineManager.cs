using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor 
{
    public class CandyMachineManager : EditorWindow 
    {

        #region Properties

        string configLocationPath;
        string rpc;
        Vector2 scrollViewPosition = Vector2.zero;
        bool showCandyMachines = false;

        #endregion

        #region MenuItem

        [MenuItem("Solana/Metaplex/Candy Machine")]
        public static void ShowWindow() {
            GetWindow(typeof(CandyMachineManager));
        }

        #endregion

        #region Unity Messages

        private void OnGUI() {
            // Layout:
            SolanaEditorUtility.FileSelectField(
                "Keypair",
                "",
                "Select a valid Keypair",
                "json"
            );
            configLocationPath = SolanaEditorUtility.FileSelectField(
                "Config Location",
                configLocationPath,
                "Select a config folder"
            );
            rpc = SolanaEditorUtility.RPCField(rpc);
            CandyMachineScrollView();
            if (GUILayout.Button("Add New Candy Machine")) {
                Debug.Log("Add New Candy Machine.");
            }
        }

        #endregion

        #region Private

        private void CandyMachineScrollView() {
            showCandyMachines = EditorGUILayout.BeginFoldoutHeaderGroup(showCandyMachines, "Existing Candy Machines");
            if (showCandyMachines) {
                scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition, SolanaEditorUtility.scrollViewStyle);
                MetaplexEditorUtility.CandyMachineField("");
                MetaplexEditorUtility.CandyMachineField("");
                MetaplexEditorUtility.CandyMachineField("");
                MetaplexEditorUtility.CandyMachineField("");
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion
    }
}
