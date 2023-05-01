using Solana.Unity.Wallet;

namespace Solana.Unity.SDK.Metaplex 
{
    public struct CandyMachineDetails 
    {

        #region Properties

        public Account Keypair { get; private set; }
        public string RpcUrl { get; private set; }

        #endregion

        #region Constructors

        public CandyMachineDetails(Account keypair, string rpcUrl) {
            Keypair = keypair;
            RpcUrl = rpcUrl;
        }

        #endregion
    }
}
