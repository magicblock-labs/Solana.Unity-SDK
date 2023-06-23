using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Metaplex.NFT;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Metaplex.Candymachine;
using Solana.Unity.Metaplex.CandyGuard.Program;
using Solana.Unity.Rpc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Solana.Unity.Metaplex.CandyGuard;

namespace Solana.Unity.SDK.Metaplex
{

    public static class CandyMachineCommands
    {

        #region Program IDs

        public static readonly PublicKey CandyMachineProgramId = new("CndyV3LdqHUfDLmE5naZjVN8rBZz4tqhdefbAnjHG3JR");
        public static readonly PublicKey CandyGuardProgramId = new("Guard1JwRhJkVH6XZhzoYxeBVQe872VH6QggF4BWmS9g");
        private static readonly uint COMPUTE_UNITS = 400_000;

        #endregion

        #region Constants

        public static readonly ulong MAX_CREATOR_LIMIT = 5;
        public static readonly ulong MAX_CREATOR_LEN = 32 + 1 + 1;
        public static readonly ulong MAX_NAME_LEN = 32;
        public static readonly ulong MAX_SYMBOL_LEN = 10;
        public static readonly ulong MAX_URI_LEN = 200;
        public static readonly ulong HIDDEN_SETTINGS_START =
            8                                         // discriminator
            + 8                                       // features
            + 32                                      // authority
            + 32                                      // mint authority
            + 32                                      // collection mint
            + 8                                       // items redeemed
            + 8                                       // items available (config data)
            + 4 + MAX_SYMBOL_LEN                      // u32 + max symbol length
            + 2                                       // seller fee basis points
            + 8                                       // max supply
            + 1                                       // is mutable
            + 4 + MAX_CREATOR_LIMIT * MAX_CREATOR_LEN // u32 + creators vec
            + 1                                       // option (config lines settings)
            + 4 + MAX_NAME_LEN                        // u32 + max name length
            + 4                                       // name length
            + 4 + MAX_URI_LEN                         // u32 + max uri length
            + 4                                       // uri length
            + 1                                       // is sequential
            + 1                                       // option (hidden setting)
            + 4 + MAX_NAME_LEN                        // u32 + max name length
            + 4 + MAX_URI_LEN                         // u32 + max uri length
            + 32;                                     // hash

        #endregion

        public static async Task<string> CreateCollection(
            Account payer,
            Account collectionMint,
            Metadata metdata,
            IRpcClient rpc
        )
        {
            var metadataClient = new MetadataClient(rpc);
            var request = await metadataClient.CreateNFT(
                payer,
                collectionMint,
                TokenStandard.NonFungible,
                metdata,
                true,
                true
            );
            return request.Result;
        }

        public static async Task<string> InitializeCandyMachine(
            Account account,
            Account candyMachineAccount,
            PublicKey collectionMint,
            CandyMachineData candyMachineData,
            IRpcClient rpc
        )
        {
            var candyMachineCreator = GetCandyMachineCreator(candyMachineAccount);
            var collectionMetadata = PDALookup.FindMetadataPDA(collectionMint);
            var collectionMasterEdition = PDALookup.FindMasterEditionPDA(collectionMint);
            var collectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
                account,
                collectionMint,
                candyMachineCreator,
                MetadataDelegateRole.Collection
            );
            var initializeCandyMachineAccounts = new InitializeV2Accounts {
                CandyMachine = candyMachineAccount,
                AuthorityPda = candyMachineCreator,
                Authority = account,
                Payer = account,
                CollectionMint = collectionMint,
                CollectionMetadata = collectionMetadata,
                CollectionMasterEdition = collectionMasterEdition,
                CollectionUpdateAuthority = account,
                CollectionDelegateRecord = collectionDelegateRecord,
                TokenMetadataProgram = MetadataProgram.ProgramIdKey,
                SystemProgram = SystemProgram.ProgramIdKey,
                SysvarInstructions = SysVars.InstructionAccount
            };
            var candyMachineInstruction = CandyMachineProgram.InitializeV2(
                initializeCandyMachineAccounts,
                candyMachineData,
                (byte)TokenStandard.NonFungible,
                CandyMachineProgramId
            );
            var blockHash = await rpc.GetRecentBlockHashAsync();
            var candyAccountSize = GetSpaceForCandyMachine(candyMachineData);
            var minimumRent = await rpc.GetMinimumBalanceForRentExemptionAsync((long)candyAccountSize);
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(
                    SystemProgram.CreateAccount(
                        account,
                        candyMachineAccount,
                        minimumRent.Result,
                        candyAccountSize,
                        CandyMachineProgramId
                    )
                )
                .AddInstruction(candyMachineInstruction)
                .Build(new List<Account> { account, candyMachineAccount });
            var txId = await rpc.SendTransactionAsync(transaction, true);
            return txId.Result;
        }

        public static async Task<string> MintOneToken(
            Account account,
            Account mint,
            PublicKey candyMachineAccount, 
            PublicKey candyGuardAccount,
            CandyGuardMintSettings mintSettings,
            IRpcClient rpc
        )
        {
            var associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(account, mint.PublicKey);
            var candyMachineClient = new CandyMachineClient(rpc, null, CandyMachineProgramId);
            var stateRequest = await candyMachineClient.GetCandyMachineAsync(candyMachineAccount);
            var state = stateRequest.ParsedResult;
            var candyMachineCreator = GetCandyMachineCreator(candyMachineAccount);

            var mintNftAccounts = new Unity.Metaplex.CandyGuard.Program.MintV2Accounts {
                CandyMachineAuthorityPda = candyMachineCreator,
                Payer = account,
                Minter = account,
                CandyMachine = candyMachineAccount,
                NftMetadata = PDALookup.FindMetadataPDA(mint),
                NftMasterEdition = PDALookup.FindMasterEditionPDA(mint),
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = MetadataProgram.ProgramIdKey,
                SplTokenProgram = TokenProgram.ProgramIdKey,
                CollectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
                    account, 
                    state.CollectionMint, 
                    candyMachineCreator, 
                    MetadataDelegateRole.Collection
                ),
                CollectionMasterEdition = PDALookup.FindMasterEditionPDA(state.CollectionMint),
                CollectionMetadata = PDALookup.FindMetadataPDA(state.CollectionMint),
                CollectionMint = state.CollectionMint,
                CollectionUpdateAuthority = account,
                NftMint = mint,
                NftMintAuthority = account,
                Token = associatedTokenAccount,
                TokenRecord = PDALookup.FindTokenRecordPDA(associatedTokenAccount, mint),
                SplAtaProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                SysvarInstructions = SysVars.InstructionAccount,
                RecentSlothashes = new PublicKey("SysvarS1otHashes111111111111111111111111111"),
                AuthorizationRules = null,
                AuthorizationRulesProgram = null,
                CandyGuard = candyGuardAccount,
                CandyMachineProgram = CandyMachineProgramId
            };
            var mintSettingsAccounts = mintSettings.GetMintArgs(account);
            var candyMachineInstruction = CandyGuardProgram.MintV2(
                mintNftAccounts,
                new byte[0],
                mintSettings.GuardGroup,
                CandyGuardProgramId
            );

            var blockHash = await rpc.GetRecentBlockHashAsync();
            var computeInstruction = ComputeBudgetProgram.SetComputeUnitLimit(COMPUTE_UNITS);
            candyMachineInstruction = new TransactionInstruction {
                Data = candyMachineInstruction.Data,
                ProgramId = candyMachineInstruction.ProgramId,
                Keys = candyMachineInstruction.Keys.Select(k => {
                    if (k.PublicKey == mint.PublicKey) {
                        return AccountMeta.Writable(mint, true);
                    }
                    return k;
                }).Concat(mintSettingsAccounts).ToList()
            };

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(computeInstruction)
                .AddInstruction(candyMachineInstruction)
                .Build(new List<Account> { account, mint });
            var txId = await rpc.SendTransactionAsync(transaction);
            return txId.Result;
        }

        public static async Task<(string txId, PublicKey guardAccount)> InitializeGuards(
            Account account,
            GuardData guardData,
            IRpcClient rpc
        )
        {
            if (guardData == null) 
            {
                Debug.LogError("Missing guard configuration");
                return (null, null);
            }
            Debug.Log("Initializing Candy Guard...");
            var guardDataBytes = new byte[3600];
            var offset = guardData.Serialize(guardDataBytes, 0);
            var resultData = new byte[offset];
            guardDataBytes.CopyTo(resultData, 0);
            var baseAccount = new Account();
            if (PublicKey.TryFindProgramAddress(
                new List<byte[]> { baseAccount.PublicKey.KeyBytes }, 
                CandyGuardProgramId, 
                out var guardAccount, 
                out var _
            ))
            {
                var candyGuardInstruction = CandyGuardProgram.Initialize(
                    new() {
                        SystemProgram = SystemProgram.ProgramIdKey,
                        Authority = account,
                        Base = baseAccount,
                        CandyGuard = guardAccount,
                        Payer = account
                    },
                    resultData,
                    CandyGuardProgramId
                );
                var blockHash = await rpc.GetRecentBlockHashAsync();
                var transaction = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(account)
                    .AddInstruction(candyGuardInstruction)
                    .Build(new List<Account> { baseAccount });
                var txId = await rpc.SendTransactionAsync(transaction);
                return (txId.Result, guardAccount);
            }
            Debug.LogError("Failed to create CandyGuard account.");
            return (null, null);
        }

        public static async Task<string> AddGuards(
            Account account,
            PublicKey guardAccount,
            GuardData guardData,
            IRpcClient rpc
        )
        {
            var guardDataBytes = new byte[3600];
            var offset = guardData.Serialize(guardDataBytes, 0);
            var resultData = new byte[offset];
            guardDataBytes.CopyTo(resultData, 0);
            var candyGuardInstruction = CandyGuardProgram.Update(
                    new() {
                        SystemProgram = SystemProgram.ProgramIdKey,
                        Authority = account,
                        CandyGuard = guardAccount,
                        Payer = account
                    },
                    resultData,
                    CandyGuardProgramId
                );
            var blockHash = await rpc.GetRecentBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(candyGuardInstruction)
                .Build(new List<Account> { account });
            var txId = await rpc.SendTransactionAsync(transaction);
            return txId.Result;
        }

        #region Private

        private static PublicKey GetCandyMachineCreator(PublicKey candyMachineAddress)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[] { Encoding.UTF8.GetBytes("candy_machine"), candyMachineAddress.KeyBytes },
                    CandyMachineProgramId,
                    out PublicKey candyMachineCreator,
                    out _)) {
                throw new InvalidProgramException();
            }
            return candyMachineCreator;
        }

        private static ulong GetSpaceForCandyMachine(CandyMachineData candyMachineData)
        {
            var space = HIDDEN_SETTINGS_START;
            if (candyMachineData.HiddenSettings == null) {
                space += 4;
                space += candyMachineData.ItemsAvailable * GetConfigLineSize(candyMachineData.ConfigLineSettings);
                space += candyMachineData.ItemsAvailable / 8;
                space += 1;
                space += candyMachineData.ItemsAvailable * 4;
            }
            return space;
        }

        private static ulong GetConfigLineSize(ConfigLineSettings configLine)
        {
            if (configLine == null) {
                return 0;
            }
            return configLine.NameLength + configLine.UriLength;
        }

        #endregion
    }
}
