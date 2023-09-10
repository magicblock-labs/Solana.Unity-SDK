using Solana.Unity.Metaplex.CandyGuard.Program;
using Solana.Unity.Metaplex.Candymachine;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Solana.Unity.SDK.Metaplex
{

    public static class CandyMachineCommands
    {

        #region Program IDs

        public static readonly PublicKey CandyMachineProgramId = new("CndyV3LdqHUfDLmE5naZjVN8rBZz4tqhdefbAnjHG3JR");
        public static readonly PublicKey CandyGuardProgramId = new("Guard1JwRhJkVH6XZhzoYxeBVQe872VH6QggF4BWmS9g");

        #endregion

        #region Constants

        private static readonly PublicKey RECENT_SLOTHASHES = new("SysvarS1otHashes111111111111111111111111111");
        private static readonly uint COMPUTE_UNITS = 400_000;
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

        public static PublicKey GetCandyMachineCreator(PublicKey candyMachineAddress)
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

        public static async Task<string> InitializeCandyMachine(
            Account account,
            Account candyMachineAccount,
            PublicKey collectionMint,
            CandyMachineData candyMachineData,
            IRpcClient rpcClient,
            bool skipPreflight = false
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
                SysvarInstructions = SysVars.InstructionAccount,
                AuthorizationRulesProgram = MetadataAuthProgram.ProgramIdKey
            };
            var candyMachineInstruction = CandyMachineProgram.InitializeV2(
                initializeCandyMachineAccounts,
                candyMachineData,
                (byte)TokenStandard.NonFungible,
                CandyMachineProgramId
            );
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var candyAccountSize = GetSpaceForCandyMachine(candyMachineData);
            var minimumRent = await rpcClient.GetMinimumBalanceForRentExemptionAsync((long)candyAccountSize);
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
            var txId = await rpcClient.SendAndConfirmTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        public static async Task<string> AddConfigLines(
            Account account,
            PublicKey candyMachineAccount,
            ConfigLine[] configLines,
            uint index,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var addConfigLinesAccounts = new AddConfigLinesAccounts() {
                Authority = account,
                CandyMachine = candyMachineAccount
            };
            var addConfigLinesInstruction = CandyMachineProgram.AddConfigLines(
                addConfigLinesAccounts,
                index,
                configLines,
                CandyMachineProgramId
            );
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(addConfigLinesInstruction)
                .Build(new List<Account> { account });
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        public static async Task<string> MintOneToken(
            Account payer,
            PublicKey receiver,
            Account mint,
            PublicKey collectionUpdateAuthority,
            PublicKey candyMachineKey,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                receiver, 
                mint.PublicKey
            );
            var candyMachineClient = new CandyMachineClient(rpcClient, null, CandyMachineProgramId);
            var stateRequest = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
            var state = stateRequest.ParsedResult;
            var candyMachineCreator = GetCandyMachineCreator(candyMachineKey);

            var mintNftAccounts = new Unity.Metaplex.Candymachine.MintV2Accounts {
                MintAuthority = payer,
                AuthorityPda = candyMachineCreator,
                Payer = payer,
                NftOwner = receiver,
                CandyMachine = candyMachineKey,
                NftMetadata = PDALookup.FindMetadataPDA(mint),
                NftMasterEdition = PDALookup.FindMasterEditionPDA(mint),
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = MetadataProgram.ProgramIdKey,
                SplTokenProgram = TokenProgram.ProgramIdKey,
                CollectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
                    collectionUpdateAuthority,
                    state.CollectionMint,
                    candyMachineCreator,
                    MetadataDelegateRole.Collection
                ),
                CollectionMasterEdition = PDALookup.FindMasterEditionPDA(state.CollectionMint),
                CollectionMetadata = PDALookup.FindMetadataPDA(state.CollectionMint),
                CollectionMint = state.CollectionMint,
                CollectionUpdateAuthority = collectionUpdateAuthority,
                NftMint = mint,
                NftMintAuthority = payer,
                Token = associatedTokenAccount,
                TokenRecord = PDALookup.FindTokenRecordPDA(associatedTokenAccount, mint),
                SplAtaProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                SysvarInstructions = SysVars.InstructionAccount,
                RecentSlothashes = RECENT_SLOTHASHES
            };
            var candyMachineInstruction = CandyMachineProgram.MintV2(mintNftAccounts, CandyMachineProgramId);
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var computeInstruction = ComputeBudgetProgram.SetComputeUnitLimit(COMPUTE_UNITS);
            candyMachineInstruction = new TransactionInstruction {
                Data = candyMachineInstruction.Data,
                ProgramId = candyMachineInstruction.ProgramId,
                Keys = candyMachineInstruction.Keys.Select(k => {
                    if (k.PublicKey == mint.PublicKey) {
                        return AccountMeta.Writable(mint, true);
                    }
                    return k;
                }).ToList()
            };
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(payer)
                .AddInstruction(computeInstruction)
                .AddInstruction(candyMachineInstruction)
                .Build(new List<Account> { payer, mint });
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight, Rpc.Types.Commitment.Confirmed);
            return txId.Result;
        }

        public static async Task<string> MintOneTokenWithGuards(
            Account payer,
            PublicKey receiver,
            Account mint,
            PublicKey collectionUpdateAuthority,
            PublicKey candyMachineKey, 
            PublicKey candyGuardKey,
            CandyGuardMintSettings mintSettings,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(payer, mint.PublicKey);
            var candyMachineClient = new CandyMachineClient(rpcClient, null, CandyMachineProgramId);
            var stateRequest = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
            var state = stateRequest.ParsedResult;
            var candyMachineCreator = GetCandyMachineCreator(candyMachineKey);

            var mintNftAccounts = new Unity.Metaplex.CandyGuard.Program.MintV2Accounts {
                CandyMachineAuthorityPda = candyMachineCreator,
                Payer = payer,
                Minter = receiver,
                CandyMachine = candyMachineKey,
                NftMetadata = PDALookup.FindMetadataPDA(mint),
                NftMasterEdition = PDALookup.FindMasterEditionPDA(mint),
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = MetadataProgram.ProgramIdKey,
                SplTokenProgram = TokenProgram.ProgramIdKey,
                CollectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
                    collectionUpdateAuthority, 
                    state.CollectionMint, 
                    candyMachineCreator, 
                    MetadataDelegateRole.Collection
                ),
                CollectionMasterEdition = PDALookup.FindMasterEditionPDA(state.CollectionMint),
                CollectionMetadata = PDALookup.FindMetadataPDA(state.CollectionMint),
                CollectionMint = state.CollectionMint,
                CollectionUpdateAuthority = collectionUpdateAuthority,
                NftMint = mint,
                NftMintAuthority = payer,
                Token = associatedTokenAccount,
                TokenRecord = PDALookup.FindTokenRecordPDA(associatedTokenAccount, mint),
                SplAtaProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                SysvarInstructions = SysVars.InstructionAccount,
                RecentSlothashes = RECENT_SLOTHASHES,
                CandyGuard = candyGuardKey,
                CandyMachineProgram = CandyMachineProgramId
            };
            var mintSettingsAccounts = mintSettings.GetMintArgs(payer, mint, candyMachineKey, candyGuardKey);
            var candyMachineInstruction = CandyGuardProgram.MintV2(
                mintNftAccounts,
                new byte[0],
                mintSettings.GuardGroup,
                CandyGuardProgramId
            );

            var blockHash = await rpcClient.GetLatestBlockHashAsync();
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
                .SetFeePayer(payer)
                .AddInstruction(computeInstruction)
                .AddInstruction(candyMachineInstruction)
                .Build(new List<Account> { payer, mint });
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight, Rpc.Types.Commitment.Confirmed);
            return txId.Result;
        }

        public static async Task<(string txId, PublicKey guardAccount)> InitializeGuards(
            Account account,
            GuardData guardData,
            IRpcClient rpcClient,
            bool skipPreflight = false
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
            Array.Copy(guardDataBytes, resultData, offset);
            var baseAccount = new Account();
            if (PublicKey.TryFindProgramAddress(
                new List<byte[]> { Encoding.UTF8.GetBytes("candy_guard"), baseAccount.PublicKey.KeyBytes }, 
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
                var blockHash = await rpcClient.GetLatestBlockHashAsync();
                var transaction = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(account)
                    .AddInstruction(candyGuardInstruction)
                    .Build(new List<Account> { account, baseAccount });
                var txId = await rpcClient.SendAndConfirmTransactionAsync(transaction, skipPreflight);
                return (txId.Result, guardAccount);
            }
            Debug.LogError("Failed to create CandyGuard account.");
            return (null, null);
        }

        public static async Task<string> AddGuards(
            Account account,
            PublicKey guardKey,
            GuardData guardData,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var guardDataBytes = new byte[3600];
            var offset = guardData.Serialize(guardDataBytes, 0);
            var resultData = new byte[offset];
            Array.Copy(guardDataBytes, resultData, offset);
            var candyGuardInstruction = CandyGuardProgram.Update(
                new() {
                    SystemProgram = SystemProgram.ProgramIdKey,
                    Authority = account,
                    CandyGuard = guardKey,
                    Payer = account
                },
                resultData,
                CandyGuardProgramId
            );
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(candyGuardInstruction)
                .Build(new List<Account> { account });
            var txId = await rpcClient.SendAndConfirmTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        public static async Task<string> WrapCandyMachine(
            Account account,
            PublicKey candyGuardKey,
            PublicKey candyMachineAccount,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var wrapAccounts = new WrapAccounts() {
                Authority = account,
                CandyMachineAuthority = account,
                CandyGuard = candyGuardKey,
                CandyMachine = candyMachineAccount,
                CandyMachineProgram = CandyMachineProgramId
            };
            var wrapInstruction = CandyGuardProgram.Wrap(
                wrapAccounts,
                CandyGuardProgramId
            );
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(wrapInstruction)
                .Build(new List<Account>() { account });
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        public static async Task<string> Withdraw(
            Account account,
            PublicKey candyMachineAccount,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var withdrawAccounts = new Unity.Metaplex.Candymachine.WithdrawAccounts() {
                Authority = account,
                CandyMachine = candyMachineAccount
            };
            var withdrawInstruction = CandyMachineProgram.Withdraw(withdrawAccounts, CandyMachineProgramId);
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(withdrawInstruction)
                .Build(account);
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        public static async Task<string> WithdrawGuards(
            Account account,
            PublicKey candyGuardAccount,
            IRpcClient rpcClient,
            bool skipPreflight = false
        )
        {
            var withdrawAccounts = new Unity.Metaplex.CandyGuard.Program.WithdrawAccounts() {
                CandyGuard = candyGuardAccount,
                Authority = account
            };
            var withdrawInstruction = CandyGuardProgram.Withdraw(withdrawAccounts, CandyMachineProgramId);
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(withdrawInstruction)
                .Build(account);
            var txId = await rpcClient.SendTransactionAsync(transaction, skipPreflight);
            return txId.Result;
        }

        #region Private

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