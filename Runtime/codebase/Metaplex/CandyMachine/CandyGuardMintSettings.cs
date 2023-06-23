using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.Text;

namespace Solana.Unity.SDK.Metaplex
{
    public class CandyGuardMintSettings
    {

        #region Types

        public class ThirdPartySignerMintSettings
        {
            public PublicKey Signer { get; set; }
        }

        public class GatekeeperMintSettings
        {
            public PublicKey Network { get; set; }
            public bool ExpireOnUse { get; set; }
        }

        public class NftPaymentMintSettings
        {
            public PublicKey Destination { get; set; }
            public PublicKey Mint { get; set; }
            public byte TokenStandard { get; set; }
        }

        public class NftGateMintSettings
        {
            public PublicKey Mint { get; set; }
        }

        public class NftBurnMintSettings
        {
            public PublicKey RequiredCollection { get; set; }
            public PublicKey Mint { get; set; }
            public byte TokenStandard { get; set; }
        }

        #endregion

        #region Constants

        private static readonly PublicKey GATEWAY_PROGRAM_ID = new("gatem74V238djXdzWnJf94Wo1DcnuGkfijbf3AuBhfs");

        #endregion

        #region Properties

        public string GuardGroup;
        public ThirdPartySignerMintSettings ThirdPartySigner { get; set; }
        public GatekeeperMintSettings Gatekeeper { get; set; }
        public NftPaymentMintSettings NftPayment { get; set; }
        public NftGateMintSettings NftGate { get; set; }
        public NftBurnMintSettings NftBurn { get; set; }

        #endregion

        #region Public

        public List<AccountMeta> GetMintArgs(Account payer)
        {
            var remainingAccounts = new List<AccountMeta>();

            if (ThirdPartySigner != null) 
            {
                remainingAccounts.Add(AccountMeta.Writable(ThirdPartySigner.Signer, true));
            }

            if (Gatekeeper != null && PublicKey.TryFindProgramAddress(
                new byte[][] { 
                    payer.PublicKey.KeyBytes, 
                    Encoding.UTF8.GetBytes("gateway"),
                    new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                    Gatekeeper.Network.KeyBytes
                },
                GATEWAY_PROGRAM_ID,
                out var tokenAccount,
                out var _
            )) {
                remainingAccounts.Add(AccountMeta.Writable(tokenAccount, false));
                if (Gatekeeper.ExpireOnUse && PublicKey.TryFindProgramAddress(
                    new byte[][] { Gatekeeper.Network.KeyBytes, Encoding.UTF8.GetBytes("expire") }, 
                    GATEWAY_PROGRAM_ID, 
                    out var expireAccount,
                    out var _
                )) 
                {
                    remainingAccounts.Add(AccountMeta.ReadOnly(GATEWAY_PROGRAM_ID, false));
                    remainingAccounts.Add(AccountMeta.ReadOnly(expireAccount, false));
                }
            }

            if (NftPayment != null) 
            {
                var nftTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, NftPayment.Mint);
                var nftMetadata = PDALookup.FindMetadataPDA(NftPayment.Mint);
                var destinationAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(NftPayment.Destination, NftPayment.Mint);

                remainingAccounts.Add(AccountMeta.Writable(nftTokenAccount, false));
                remainingAccounts.Add(AccountMeta.Writable(nftMetadata, false));
                remainingAccounts.Add(AccountMeta.ReadOnly(NftPayment.Mint, false));
                remainingAccounts.Add(AccountMeta.ReadOnly(NftPayment.Destination, false));
                remainingAccounts.Add(AccountMeta.Writable(destinationAccount, false));
                remainingAccounts.Add(AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false));
            }

            if (NftGate != null) 
            {
                var nftTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, NftGate.Mint);
                var nftMetadata = PDALookup.FindMetadataPDA(NftGate.Mint);

                remainingAccounts.Add(AccountMeta.Writable(nftTokenAccount, false));
                remainingAccounts.Add(AccountMeta.ReadOnly(nftMetadata, false));
            }

            if (NftBurn != null) 
            {
                var nftTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, NftBurn.Mint);
                var nftMetadata = PDALookup.FindMetadataPDA(NftBurn.Mint);
                var tokenEdition = PDALookup.FindMasterEditionPDA(NftBurn.Mint);
                var collectionMetadata = PDALookup.FindMetadataPDA(NftBurn.RequiredCollection);

                remainingAccounts.Add(AccountMeta.Writable(nftTokenAccount, false));
                remainingAccounts.Add(AccountMeta.Writable(nftMetadata, false));
                remainingAccounts.Add(AccountMeta.Writable(tokenEdition, false));
                remainingAccounts.Add(AccountMeta.Writable(NftBurn.Mint, false));
                remainingAccounts.Add(AccountMeta.Writable(collectionMetadata, false));
            }

            return remainingAccounts;
        }

        #endregion
    }
}
