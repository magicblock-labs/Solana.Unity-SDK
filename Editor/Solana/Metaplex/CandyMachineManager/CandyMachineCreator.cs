namespace Solana.Unity.SDK.Editor
{
    public class CandyMachineCreator : SolanaSetupWizard
    {

        #region Properties

        private protected override WizardQuestion[] Questions => new WizardQuestion[] {
            new ("How much will your NFTs sell for?", "0"),
            new ("What is the symbol for your NFTs?", "0"),
        };

        #endregion

        #region SolanaSetupWizard

        private protected override void OnWizardFinished()
        {
            // Create config file
        }

        #endregion
    }
}
