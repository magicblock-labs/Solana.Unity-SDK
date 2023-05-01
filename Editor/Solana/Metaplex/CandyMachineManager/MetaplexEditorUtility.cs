using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor 
{
    public static class MetaplexEditorUtility 
    {

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

        public static void CandyMachineField(string json) {
            EditorGUILayout.BeginHorizontal(candyMachineFieldStyle);
            CollectionImage(124);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            CandyMachineInfo();
            CandyMachineControls();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private static void CollectionImage(int height) {
            Texture2D defaultIcon = (Texture2D)Resources.Load("DefaultCollectionIcon");
            if (GUILayout.Button(defaultIcon, collectionButtonStyle)) {
                Debug.Log("Collection Clicked");
            }
        }

        private static void CandyMachineControls() {
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Settings", settingsButtonStyle)) {
                Debug.Log("Settings Clicked");
            }
            CandyMachineControlGrid();
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

        private static void CandyMachineInfo() {
            EditorGUILayout.BeginVertical();
            {
                SolanaEditorUtility.StaticTextProperty("Address", "Some Candy Machine Address");
                SolanaEditorUtility.StaticTextProperty("Cluster", "mainnet-beta");
                SolanaEditorUtility.StaticTextProperty("Authority", "Some Solana Address");
            }
            EditorGUILayout.EndVertical();
        }
    }
}
