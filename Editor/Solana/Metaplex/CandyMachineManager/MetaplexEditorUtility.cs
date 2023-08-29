using Newtonsoft.Json;
using Solana.Unity.Rpc;
using Solana.Unity.SDK.Metaplex;
using Solana.Unity.Wallet;
using System;
using System.IO;
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

        internal struct CandyMachine
        {
            internal CandyMachineConfiguration config;
            internal CandyMachineCache cache;
            internal CandyMachineState state;
            internal Texture collectionIcon;

            internal CandyMachine(
                CandyMachineConfiguration config,
                CandyMachineCache cache,
                CandyMachineState state,
                Texture collectionIcon
            )
            {
                this.config = config;
                this.cache = cache;
                this.state = state;
                this.collectionIcon = collectionIcon;
            }
        }

        internal class CandyMachineState
        {
            internal int guardGroup;
            internal int freezePeriod;
        }

        #endregion

        #region Internal

        internal static CandyMachineState CandyMachineField(
            CandyMachine candyMachine,
            string keyPair,
            string rpcUrl,
            Action refreshCallback = null
        )
        {
            EditorGUILayout.BeginHorizontal(candyMachineFieldStyle);
            {
                CollectionImage(candyMachine.collectionIcon);
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.Space();
                    CandyMachineDetails(candyMachine.cache, candyMachine.config);
                    CandyMachineControls(
                        candyMachine.state,
                        candyMachine.cache,
                        candyMachine.config, 
                        keyPair, 
                        rpcUrl,
                        refreshCallback
                    );
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            return candyMachine.state;
        }

        #endregion

        #region UI Components

        private static void CollectionImage(Texture collectionIcon)
        {
            var icon = collectionIcon;
            if (icon == null) 
            {
                icon = (Texture2D)Resources.Load("DefaultCollectionIcon");
            }
            GUILayout.Box(icon);
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
                
                if (cache?.Info.CandyMachineKey != null) 
                {
                    CandyMachineControlGrid(
                        state,
                        config,
                        cache,
                        cache.Info.CandyMachineKey, 
                        cache.Info.CandyGuardKey, 
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
            PublicKey candyMachineKey, 
            PublicKey candyGuardKey, 
            string keypair,
            string rpcUrl
        ) 
        {
            var groups = config.guards?.groups?.Select(group => group.label).ToArray();
            if (groups != null && groups.Length > 0) 
            {
                state.guardGroup = SolanaEditorUtility.DropdownField("Control Guard Group", state.guardGroup, groups);
            }
            GUI.enabled = candyGuardKey != null;
            state.freezePeriod = SolanaEditorUtility.RangeField("Freeze Period Seconds", state.freezePeriod, 0, 30 * 24 * 60 * 60); // Freeze can be a max of 30 days.
            GUI.enabled = true;
            var guardGroup = groups?.Length > 0 ? groups[state.guardGroup] : null;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Re-Deploy", settingsButtonStyle)) {

                        CandyMachineController.InitializeCandyMachine(
                            config,
                            cache,
                            keypair,
                            rpcUrl
                        );
                    }
                    if (GUILayout.Button("Withdraw", settingsButtonStyle)) {
                        CandyMachineController.Withdraw(
                            candyMachineKey,
                            candyGuardKey,
                            keypair,
                            rpcUrl
                        );
                    }
                    if (GUILayout.Button("Sign", settingsButtonStyle)) {
                        CandyMachineController.Sign(
                            candyMachineKey,
                            keypair,
                            rpcUrl
                        );
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Update Guards", settingsButtonStyle)) {
                        CandyMachineController.UpdateGuards(
                            candyGuardKey,
                            candyMachineKey,
                            config,
                            cache,
                            keypair,
                            rpcUrl
                        );
                    }
                    if (GUILayout.Button("Mint", settingsButtonStyle)) {
                        CandyMachineController.MintToken(
                            candyMachineKey,
                            candyGuardKey,
                            config,
                            keypair,
                            rpcUrl,
                            guardGroup
                        );
                    }
                    if (GUILayout.Button("Reveal", settingsButtonStyle)) {
                        CandyMachineController.Reveal(
                            cache,
                            config.hiddenSettings.ToCandyMachineHiddenSettings(),
                            candyMachineKey,
                            keypair,
                            rpcUrl
                        );
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    GUI.enabled = candyGuardKey != null;
                    if (GUILayout.Button("Freeze", settingsButtonStyle)) {
                        CandyMachineController.Freeze(
                            keypair,
                            rpcUrl,
                            candyGuardKey,
                            candyMachineKey,
                            guardGroup,
                            state.freezePeriod
                        );
                    }
                    if (GUILayout.Button("Thaw", settingsButtonStyle)) {
                        CandyMachineController.Thaw(
                            keypair,
                            rpcUrl,
                            candyGuardKey,
                            candyMachineKey,
                            guardGroup
                        );
                    }
                    if (GUILayout.Button("Unlock Funds", settingsButtonStyle)) {
                        CandyMachineController.UnlockFunds(
                            keypair,
                            rpcUrl,
                            candyGuardKey,
                            candyMachineKey,
                            guardGroup
                        );
                    }
                    GUI.enabled = true;
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
            var assetsUploaded = cache?.Items?.Count() ?? 0;
            GUILayout.Label(
                string.Format("Uploaded Assets: {0} / {1}", assetsUploaded, config.number),
                SolanaEditorUtility.propLabelStyle
            );
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Deploy", settingsButtonStyle))
                {
                    CandyMachineController.InitializeCandyMachine(
                        config,
                        cache,
                        keyPair,
                        rpcUrl,
                        refreshCallback
                    );
                }
                if (GUILayout.Button("Upload", settingsButtonStyle)) {
                    if (cache == null) 
                    {
                        var newCache = new CandyMachineCache();
                        var cachePath = EditorUtility.SaveFilePanel(
                            "Save the CandyMachine cache.", 
                            Application.dataPath, 
                            "cache", 
                            "json"
                        );
                        var cacheJson = JsonConvert.SerializeObject(newCache);
                        File.WriteAllText(cachePath, cacheJson);
                        config.cacheFilePath = cachePath;
                        AssetDatabase.SaveAssets();
                        CandyMachineController.UploadCandyMachineAssets(newCache, config, keyPair, rpcUrl, refreshCallback);
                    }
                    else 
                    {
                        CandyMachineController.UploadCandyMachineAssets(cache, config, keyPair, rpcUrl, refreshCallback);
                    }
                }
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
