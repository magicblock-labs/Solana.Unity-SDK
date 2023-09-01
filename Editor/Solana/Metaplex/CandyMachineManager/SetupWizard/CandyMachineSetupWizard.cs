using System;
using System.IO;
using UnityEditor;

namespace Solana.Unity.SDK.Editor
{
    /// <summary>
    /// A setup wizard used to create <see cref="CandyMachineConfiguration"/>s.
    /// </summary>
    internal class CandyMachineSetupWizard : SolanaSetupWizard<CandyMachineConfiguration>
    {

        #region Properties

        protected override string SavePath => configPath;

        #endregion

        #region Fields

        private static string configPath;
        private static Action onCompleted;

        #endregion

        #region Static

        /// <summary>
        /// Opens a new copy of this window ready to create a new config.
        /// </summary>
        /// <param name="configPath">
        /// The path to which to save the new config asset.
        /// </param>
        internal static void OpenNew(string configPath, Action onCompleted)
        {
            GetWindow(typeof(CandyMachineSetupWizard), false, "Candy Machine Setup Wizard");
            CandyMachineSetupWizard.configPath = configPath;
            CandyMachineSetupWizard.onCompleted = onCompleted;
        }

        #endregion

        #region SolanaSetupWizard

        /// <inheritdoc/>
        private protected override void OnWizardFinished()
        {
            var filePath = EditorUtility.SaveFilePanel("Save Config File", configPath, "config", "asset");
            filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            AssetDatabase.CreateAsset(target.targetObject, filePath);
            AssetDatabase.SaveAssets();
            Close();
            onCompleted?.Invoke();
        }

        #endregion
    }
}
