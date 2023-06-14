using System;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Metaplex.Candymachine;
using Solana.Unity.Metaplex.Candymachine.Types;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Metaplex.Candymachine.Errors;
using UnityEngine;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.SDK.Metaplex;
using System.Collections.Generic;

using MetaplexCreator = Solana.Unity.Metaplex.NFT.Library.Creator;

namespace CandyMachineV2
{

    public static class CandyMachineCommands
    {

        #region Program IDs

        public static readonly PublicKey TokenMetadataProgramId = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");
        public static readonly PublicKey CandyMachineProgramId = new("cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ");
        public static readonly PublicKey instructionSysVarAccount = new("Sysvar1nstructions1111111111111111111111111");

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
        private static readonly int COLLECTION_CACHE_KEY = -1;

        #endregion

        public static async Task<Transaction> CreateCollection(
            Account candyMachineAccount,
            CandyMachineCache cache,
            CandyMachineData candyMachineData,
            IRpcClient rpc
        )
        {
            var collectionMint = new Account();
            if (cache.Items.TryGetValue(COLLECTION_CACHE_KEY, out var collectionItem))
            {
                var rent = await rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize);
                var associatedTokenAddress = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                    cache.Info.Creator, 
                    collectionMint.PublicKey
                );
                var collectionMetadataKey = PDALookup.FindMetadataPDA(collectionMint.PublicKey);
                var collectionMasterEdition = PDALookup.FindMasterEditionPDA(collectionMint.PublicKey);
                var collectionMetadata = new Metadata {
                    name = collectionItem.name,
                    symbol = candyMachineData.Symbol,
                    uri = collectionItem.metadataLink,
                    sellerFeeBasisPoints = 0,
                    creators = new List<MetaplexCreator> { new (cache.Info.Creator, 100, true) }
                };
                var blockHash = await rpc.GetRecentBlockHashAsync();

                var transaction = new TransactionBuilder()
                    .AddInstruction(
                        SystemProgram.CreateAccount(
                            cache.Info.Creator,
                            collectionMint.PublicKey,
                            rent.Result,
                            TokenProgram.MintAccountDataSize,
                            TokenProgram.ProgramIdKey
                        )
                    )
                    .AddInstruction(
                        TokenProgram.InitializeMint(
                            collectionMint.PublicKey,
                            0,
                            cache.Info.Creator,
                            cache.Info.Creator
                        )
                    )
                    .AddInstruction(
                        AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                            cache.Info.Creator,
                            cache.Info.Creator,
                            collectionMint.PublicKey
                        )
                    )
                    .AddInstruction(
                        TokenProgram.MintTo(
                            collectionMint.PublicKey,
                            associatedTokenAddress,
                            1,
                            cache.Info.Creator
                        )
                    )
                    .AddInstruction(
                        MetadataProgram.CreateMetadataAccount(
                            collectionMetadataKey,
                            collectionMint.PublicKey,
                            cache.Info.Creator,
                            cache.Info.Creator,
                            cache.Info.Creator,
                            collectionMetadata,
                            TokenStandard.NonFungible,
                            true,
                            true,
                            null,
                            1
                        )
                    )
                    .AddInstruction(
                        MetadataProgram.CreateMasterEdition(
                            1,
                            collectionMasterEdition,
                            collectionMint.PublicKey,
                            cache.Info.Creator,
                            cache.Info.Creator,
                            cache.Info.Creator,
                            collectionMetadataKey
                        )
                    )
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(cache.Info.Creator);

                collectionItem.onChain = true;
                cache.Info.CollectionMint = collectionMint.PublicKey;
                var tx = Transaction.Deserialize(transaction.Serialize());
                tx.PartialSign(collectionMint);
                return tx;
            } else {
                Debug.LogError("Trying to create and set collection when collection item info isn't in cache! This shouldn't happen!");
                return null;
            }
        }

        public static async Task<Transaction> InitializeCandyMachine(
            Account account,
            Account candyMachineAccount,
            PublicKey collectionMint,
            PublicKey collectionUpdateAuthority,
            CandyMachineData candyMachineData,
            IRpcClient rpc
        )
        {
            var candyMachineCreator = GetCandyMachineCreator(candyMachineAccount);
            var collectionMetadata = PDALookup.FindMetadataPDA(collectionMint);
            var collectionMasterEdition = PDALookup.FindMasterEditionPDA(collectionMint);
            var collectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
                collectionUpdateAuthority,
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
                CollectionUpdateAuthority = collectionUpdateAuthority,
                CollectionDelegateRecord = collectionDelegateRecord,
                TokenMetadataProgram = TokenMetadataProgramId,
                SystemProgram = SystemProgram.ProgramIdKey,
                SysvarInstructions = instructionSysVarAccount
            };
            var candyMachineInstruction = CandyMachineProgram.InitializeV2(
                initializeCandyMachineAccounts,
                candyMachineData,
                (byte)TokenStandard.NonFungible,
                CandyMachineProgramId
            );
            var blockHash = await rpc.GetRecentBlockHashAsync();
            try {
                var candyAccountSize = GetSpaceForCandyMachine(candyMachineData);
                var minimumRent = await rpc.GetMinimumBalanceForRentExemptionAsync((long)candyAccountSize);
                var transaction = new TransactionBuilder()
                    .AddInstruction(
                        SystemProgram.CreateAccount(
                            account,
                            candyMachineAccount.PublicKey,
                            minimumRent.Result,
                            TokenProgram.MintAccountDataSize,
                            TokenProgram.ProgramIdKey
                        )
                    )
                    .AddInstruction(candyMachineInstruction)
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(account);

                var tx = Transaction.Deserialize(transaction.Serialize());
                tx.PartialSign(candyMachineAccount);
                return tx;
            }
            catch 
            {
                Debug.LogError(
                    string.Format("Error: {0} Numerical overflow error.", CandyMachineErrorKind.NumericalOverflowError)
                );
                return null;
            }
        }

        /// <summary>
        /// Mint one token from the Candy Machine
        /// </summary>
        /// <param name="account">The target account used for minting the token</param>
        /// <param name="candyMachineKey">The CandyMachine public key</param>
        /// <param name="rpc">The RPC instance</param>
/*        public static async Task<Transaction> MintOneToken(Account account, PublicKey candyMachineKey, IRpcClient rpc)
        {
            var mint = new Account();
            var associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(account, mint.PublicKey);

            var candyMachineClient = new CandyMachineClient(rpc, null, CandyMachineProgramId);
            var candyMachineWrap = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
            var candyMachine = candyMachineWrap.ParsedResult;

            var candyMachineCreator = getCandyMachineCreator(candyMachineKey);

            var mintNftAccounts = new MintV2Accounts {
                CandyMachine = candyMachineKey,
                AuthorityPda = candyMachineCreator,
                Payer = account,
                MintAuthority = account,
                NftMetadata = getMetadata(mint.PublicKey),
                NftMasterEdition = getMasterEdition(mint.PublicKey),
                Mint = mint.PublicKey,
                RecentBlockhashes = SysVars.RecentBlockHashesKey,
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = TokenMetadataProgramId,
                SplTokenProgram = TokenProgram.ProgramIdKey,
                UpdateAuthority = account
            };

            var candyMachineInstruction = CandyMachineProgram.MintV2(mintNftAccounts, CandyMachineProgramId);

            var blockHash = await rpc.GetRecentBlockHashAsync();
            var minimumRent = await rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize);

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(
                    SystemProgram.CreateAccount(
                        account,
                        mint.PublicKey,
                        minimumRent.Result,
                        TokenProgram.MintAccountDataSize,
                        TokenProgram.ProgramIdKey))
                .AddInstruction(
                    TokenProgram.InitializeMint(
                        mint.PublicKey,
                        0,
                        account,
                        account))
                .AddInstruction(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        account,
                        account,
                        mint.PublicKey))
                .AddInstruction(
                    TokenProgram.MintTo(
                        mint.PublicKey,
                        associatedTokenAccount,
                        1,
                        account))
                .AddInstruction(candyMachineInstruction);

            var tx = Transaction.Deserialize(transaction.Serialize());
            tx.PartialSign(mint);
            return tx;
        }*/

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

        public static ulong GetSpaceForCandyMachine(CandyMachineData candyMachineData)
        {
            var space = HIDDEN_SETTINGS_START;
            if (candyMachineData.HiddenSettings == null) {
                space += 4;
                space += candyMachineData.ItemsAvailable * GetConfigLineSize(candyMachineData.ConfigLineSettings);
                checked {
                    space /= 8;
                }
                space += 1;
                space += candyMachineData.ItemsAvailable * 4;
            }
            return space;
        }

        public static ulong GetConfigLineSize(ConfigLineSettings configLine)
        {
            if (configLine == null) {
                return 0;
            }
            return configLine.NameLength + configLine.UriLength;
        }
    }
}
