using System;
using System.Linq;
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

        #region Types

        internal class CandyMachineState
        {
            internal int guardGroup;
        }

        #endregion

        #region Internal

        internal static CandyMachineState CandyMachineField(
            CandyMachineState state,
            CandyMachineCache cache,
            CandyMachineConfiguration config,
            string keyPair,
            string rpcUrl,
            Action refreshCallback = null
        )
        {
            EditorGUILayout.BeginHorizontal(candyMachineFieldStyle);
            {
                CollectionImage(124);
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.Space();
                    CandyMachineDetails(cache, config);
                    CandyMachineControls(
                        state, 
                        cache, 
                        config, 
                        keyPair, 
                        rpcUrl,
                        refreshCallback
                    );
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            return state;
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

        private static CandyMachineState CandyMachineControls(
            CandyMachineState state,
            CandyMachineCache cache, 
            CandyMachineConfiguration config,
            string keyPair,
            string rpcUrl,
            Action refreshCallback
        )
        {
            EditorGUILayout.BeginVertical();
            {
                
                if (cache?.Info.CandyMachine != null && cache?.Info.CandyMachine != string.Empty) 
                {
                    CandyMachineControlGrid(
                        state,
                        config,
                        cache,
                        cache.Info.CandyMachine, 
                        cache.Info.CandyGuard, 
                        keyPair, 
                        rpcUrl
                    );
                } 
                else 
                {
                    CandyMachineSetupControls(cache, config, keyPair, rpcUrl, refreshCallback);
                }
            }
            EditorGUILayout.EndVertical();
            return state;
        }

        private static void CandyMachineControlGrid(
            CandyMachineState state,
            CandyMachineConfiguration config,
            CandyMachineCache cache,
            string candyMachineKey, 
            string candyGuardKey, 
            string keypair,
            string rpcUrl
        ) 
        {
            var groups = config.guards?.groups?.Select(group => group.label).ToArray();
            if (groups != null) 
            {
                state.guardGroup = SolanaEditorUtility.DropdownField("Mint Guard Group", state.guardGroup, groups);
            }
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Freeze", settingsButtonStyle)) 
                    {
                        Debug.Log("Settings Clicked");
                    }
                    if (GUILayout.Button("Update Guards", settingsButtonStyle)) 
                    {
                        CandyMachineController.UpdateGuards(
                            new(candyGuardKey),
                            new(candyMachineKey),
                            config,
                            cache,
                            keypair,
                            rpcUrl
                        );
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Withdraw", settingsButtonStyle)) 
                    {
                        CandyMachineController.Withdraw(
                            new(candyMachineKey),
                            new(candyGuardKey),
                            keypair,
                            rpcUrl
                        );
                    }
                    if (GUILayout.Button("Sign", settingsButtonStyle)) 
                    {
                        CandyMachineController.Sign(
                            new (candyMachineKey),
                            keypair,
                            rpcUrl
                        );
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
                            config,
                            keypair,
                            rpcUrl,
                            groups?[state.guardGroup]
                        );
                    }
                    if (GUILayout.Button("Reveal", settingsButtonStyle)) 
                    {
                        CandyMachineController.Reveal(
                            cache,
                            config.hiddenSettings.ToCandyMachineHiddenSettings(),
                            new(candyMachineKey),
                            keypair,
                            rpcUrl
                        );
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
            string rpcUrl,
            Action refreshCallback
        )
        {
            GUILayout.Label(
                "Import an existing cache - leave empty to create one on upload.",
                SolanaEditorUtility.propLabelStyle
            );
            var newPath = SolanaEditorUtility.FileSelectField(
                "Cache",
                config.cacheFilePath,
                false,
                "Import an existing cache",
                "json"
            );
            if (newPath != config.cacheFilePath) 
            {
                config.cacheFilePath = newPath;
                AssetDatabase.SaveAssets();
                refreshCallback?.Invoke();
            }
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

        private static void CandyMachineDetails(CandyMachineCache cache, CandyMachineConfiguration config)
        {
            EditorGUILayout.BeginVertical();
            {
                if (cache?.Info.CandyMachine != null) {
                    SolanaEditorUtility.StaticTextProperty("Cache", config.cacheFilePath);
                    SolanaEditorUtility.StaticTextProperty("Address", cache.Info.CandyMachine);
                    SolanaEditorUtility.StaticTextProperty("Authority", cache.Info.Creator);
                }
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

    }
}
