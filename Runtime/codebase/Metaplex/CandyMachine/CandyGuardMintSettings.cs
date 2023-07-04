using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
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

        public class AllowListMintSettings
        {
            public byte[] MerkleRoot { get; set; }
        }

        public class MintLimitMintSettings
        {
            public int Id { get; set; }
        }

        public class SolPaymentMintSettings
        {
            public PublicKey Destination { get; set; }
        }

        public class TokenBurnMintSettings
        {
            public PublicKey Mint { get; set; }
        }

        public class TokenGateMintSettings
        {
            public PublicKey Mint { get; set; }
        }

        public class TokenPaymentMintSettings
        {
            public PublicKey Mint { get; set; }
            public PublicKey DestinationAta { get; set; }
        }

        public class FreezeTokenPaymentMintSettings
        {
            public PublicKey Mint { get; set; }
            public PublicKey DestinationAta { get; set; }
        }

        public class FreezeSolPaymentMintSettings
        {
            public PublicKey Destination { get; set; }
        }

        #endregion

        #region Constants

        private static readonly PublicKey GATEWAY_PROGRAM_ID = new("gatem74V238djXdzWnJf94Wo1DcnuGkfijbf3AuBhfs");

        #endregion

        #region Properties

        public string GuardGroup;
        public ThirdPartySignerMintSettings ThirdPartySigner { get; set; }
        public GatekeeperMintSettings Gatekeeper { get; set; }
        public AllowListMintSettings AllowList { get; set; }
        public MintLimitMintSettings MintLimit { get; set; }
        public SolPaymentMintSettings SolPayment { get; set; }
        public NftPaymentMintSettings NftPayment { get; set; }
        public NftGateMintSettings NftGate { get; set; }
        public NftBurnMintSettings NftBurn { get; set; }
        public TokenBurnMintSettings TokenBurn { get; set; }
        public TokenGateMintSettings TokenGate { get; set; }
        public TokenPaymentMintSettings TokenPayment { get; set; }
        public FreezeSolPaymentMintSettings FreezeSolPayment { get; set; }
        public FreezeTokenPaymentMintSettings FreezeTokenPayment { get; set; }

        #endregion

        #region Public

        public List<AccountMeta> GetMintArgs(
            Account payer,
            Account mint,
            PublicKey candyMachineKey,
            PublicKey candyGuardKey
        )
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
            ))
            {
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

            if (AllowList != null) 
            {
                if (PublicKey.TryFindProgramAddress(
                    new List<byte[]> { 
                        Encoding.UTF8.GetBytes("allow_list"),
                        AllowList.MerkleRoot,
                        payer.PublicKey,
                        candyGuardKey,
                        candyMachineKey
                    },
                   CandyMachineCommands.CandyGuardProgramId,
                    out var proofPda,
                    out var _
                )) 
                {
                    remainingAccounts.Add(AccountMeta.ReadOnly(proofPda, false));
                }
            }

            if (MintLimit != null) 
            {
                if (PublicKey.TryFindProgramAddress(
                    new List<byte[]> {
                        Encoding.UTF8.GetBytes("mint_limit"),
                        BitConverter.GetBytes(MintLimit.Id),
                        payer.PublicKey,
                        candyGuardKey,
                        candyMachineKey
                    },
                   CandyMachineCommands.CandyGuardProgramId,
                    out var proofPda,
                    out var _
                )) {
                    remainingAccounts.Add(AccountMeta.Writable(proofPda, false));
                }
            }

            if (SolPayment != null)
            {
                remainingAccounts.Add(AccountMeta.Writable(SolPayment.Destination, false));
            }

            if (TokenBurn != null) 
            {
                var burnAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, TokenBurn.Mint);
                remainingAccounts.Add(AccountMeta.Writable(burnAta, false));
                remainingAccounts.Add(AccountMeta.Writable(TokenBurn.Mint, false));
            }

            if (TokenGate != null) 
            {
                var gateAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, TokenGate.Mint);
                remainingAccounts.Add(AccountMeta.ReadOnly(gateAta, false));
            }

            if (TokenPayment != null) 
            {
                var payerTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, TokenPayment.Mint);
                remainingAccounts.Add(AccountMeta.Writable(payerTokenAccount, false));
                remainingAccounts.Add(AccountMeta.Writable(TokenPayment.DestinationAta, false));
            }

            if (FreezeSolPayment != null) {
                if (PublicKey.TryFindProgramAddress(
                    new List<byte[]>() {
                        Encoding.UTF8.GetBytes("freeze_escrow"),
                        FreezeSolPayment.Destination.KeyBytes,
                        candyGuardKey.KeyBytes,
                        candyMachineKey.KeyBytes
                    },
                    CandyMachineCommands.CandyGuardProgramId,
                    out var freezePda,
                    out var _
                )) 
                {
                    remainingAccounts.Add(AccountMeta.Writable(freezePda, false));
                    var nftAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, mint);
                    remainingAccounts.Add(AccountMeta.ReadOnly(nftAta, false));
                }
            }

            if (FreezeTokenPayment != null) {
                if (PublicKey.TryFindProgramAddress(
                    new List<byte[]>() {
                        Encoding.UTF8.GetBytes("freeze_escrow"),
                        FreezeTokenPayment.DestinationAta.KeyBytes,
                        candyGuardKey.KeyBytes,
                        candyMachineKey.KeyBytes
                    },
                    CandyMachineCommands.CandyGuardProgramId,
                    out var freezePda,
                    out var _
                )) {
                    remainingAccounts.Add(AccountMeta.Writable(freezePda, false));
                    var nftAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, mint);
                    remainingAccounts.Add(AccountMeta.ReadOnly(nftAta, false));
                    var tokenAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, FreezeTokenPayment.Mint);
                    remainingAccounts.Add(AccountMeta.Writable(tokenAta, false));
                    var freezeAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(freezePda, mint);
                    remainingAccounts.Add(AccountMeta.Writable(freezeAta, false));
                }
            }

            return remainingAccounts;
        }

        public void OverrideWith(CandyGuardMintSettings overrides)
        {
            GuardGroup = overrides.GuardGroup;
            if (overrides.Gatekeeper != null)
            {
                Gatekeeper = overrides.Gatekeeper;
            }

            if (overrides.ThirdPartySigner != null) 
            {
                ThirdPartySigner = overrides.ThirdPartySigner;
            }

            if (overrides.NftGate != null) 
            {
                NftGate = overrides.NftGate;
            }

            if (overrides.NftBurn != null)
            {
                NftBurn = overrides.NftBurn;
            }

            if (overrides.NftPayment != null)
            {
                NftPayment = overrides.NftPayment;
            }

            if (overrides.AllowList != null)
            {
                AllowList = overrides.AllowList;
            }

            if (overrides.TokenGate != null)
            {
                TokenGate = overrides.TokenGate;
            }

            if (overrides.TokenBurn != null)
            {
                TokenBurn = overrides.TokenBurn;
            }

            if (overrides.MintLimit != null)
            {
                MintLimit = overrides.MintLimit;
            }

            if (overrides.SolPayment != null)
            {
                SolPayment = overrides.SolPayment;
            }

            if (overrides.TokenPayment != null)
            {
                TokenPayment = overrides.TokenPayment;
            }

            if (overrides.FreezeSolPayment != null) 
            {
                FreezeSolPayment = overrides.FreezeSolPayment;
            }

            if (overrides.FreezeTokenPayment != null) 
            {
                FreezeTokenPayment = overrides.FreezeTokenPayment;
            }
        }

        #endregion
    }
}
