
using Solana.Unity.Wallet;

namespace Solana.Unity.SDK
{
    public class Signer
    {
        public PublicKey publicKey;
        public byte[] secretKey;
    }

    public class Ed25519Keypair
    {
        public byte[] publicKey;
        public byte[] privateKey;
    }
}