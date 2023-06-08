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

        private static string configPath;

        #endregion

        #region Static

        /// <summary>
        /// Opens a new copy of this window ready to create a new config.
        /// </summary>
        /// <param name="configPath">
        /// The path to which to save the new config asset.
        /// </param>
        internal static void OpenNew(string configPath)
        {
            GetWindow(typeof(CandyMachineSetupWizard), false, "Candy Machine Setup Wizard");
            CandyMachineSetupWizard.configPath = configPath;
        }

        #endregion

        #region SolanaSetupWizard

        /// <inheritdoc/>
        private protected override void OnWizardFinished()
        {
            AssetDatabase.CreateAsset(target.targetObject, configPath);
            var config = (CandyMachineConfiguration)target.targetObject;
            AssetDatabase.SaveAssets();
            var candyMachineData = config.ToCandyMachineData();
            // TODO: Init candy machine
            Close();
        }

        #endregion
    }
}
