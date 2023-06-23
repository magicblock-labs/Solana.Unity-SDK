using Solana.Unity.Wallet;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal static class MetaplexEditorUtility 
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

        internal static readonly GUIStyle answerFieldStyle = new() {
            alignment = TextAnchor.MiddleCenter,
            padding = SolanaEditorUtility.standardPadding,
            margin = wizardQuestionMargin
        };

        #endregion

        #region Internal

        internal static void CandyMachineField(
            CandyMachineCache cache,
            CandyMachineConfiguration config,
            string keyPair,
            string rpcUrl
        )
        {
            EditorGUILayout.BeginHorizontal(candyMachineFieldStyle);
            CollectionImage(124);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            CandyMachineDetails(cache);
            CandyMachineControls(cache, config, keyPair, rpcUrl);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region UI Components

        private static void CollectionImage(int height)
        {
            Texture2D defaultIcon = (Texture2D)Resources.Load("DefaultCollectionIcon");
            if (GUILayout.Button(defaultIcon, collectionButtonStyle)) 
            {
                Debug.Log("Collection Clicked");
            }
        }

        private static void CandyMachineControls(
            CandyMachineCache cache, 
            CandyMachineConfiguration config,
            string keyPair,
            string rpcUrl
        )
        {
            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Edit Config", settingsButtonStyle)) 
                {
                    Debug.Log("Settings Clicked");
                }
                if (cache.Info.CandyMachine != null && cache.Info.CandyMachine != string.Empty) 
                {
                    CandyMachineControlGrid(
                        cache.Info.CandyMachine, 
                        cache.Info.CandyGuard, 
                        keyPair, 
                        rpcUrl
                    );
                } 
                else 
                {
                    CandyMachineSetupControls(cache, config, keyPair, rpcUrl);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void CandyMachineControlGrid(
            string candyMachineKey, 
            string candyGuardKey, 
            string keypair,
            string rpcUrl
        ) 
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Freeze", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                    if (GUILayout.Button("Upload", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Withdraw", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                    if (GUILayout.Button("Sign", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Mint", settingsButtonStyle)) 
                    {
                        CandyMachineController.MintToken(
                            new(candyMachineKey), 
                            new(candyGuardKey),
                            keypair,
                            rpcUrl
                        );
                    }
                    if (GUILayout.Button("Reveal", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void CandyMachineSetupControls(
            CandyMachineCache cache, 
            CandyMachineConfiguration config,
            string keyPair,
            string rpcUrl
        )
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Deploy", settingsButtonStyle))
                {
                    CandyMachineController.InitializeCandyMachine(
                        config,
                        cache,
                        "TEST TEST 1",
                        "",
                        keyPair,
                        rpcUrl
                    );
                }
                // TODO: Enable once Bundlr uploader is fixed.
                /*if (GUILayout.Button("Upload", settingsButtonStyle)) 
                {
                    CandyMachineController.UploadCandyMachineAssets(cache, config, keyPair, rpcUrl);
                }*/
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void CandyMachineDetails(CandyMachineCache cache)
        {
            EditorGUILayout.BeginVertical();
            {
                if (cache.Info.CandyMachine != null) {
                    SolanaEditorUtility.StaticTextProperty("Address", cache.Info.CandyMachine);
                    SolanaEditorUtility.StaticTextProperty("Authority", cache.Info.Creator);
                    SolanaEditorUtility.StaticTextProperty("Cache", cache.FilePath);
                }
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

    }
}
