using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Wallet;
using CandyMachineV2.Accounts;
using CandyMachineV2.Errors;
using CandyMachineV2.Program;
using CandyMachineV2.Types;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;

namespace CandyMachineV2
{
    namespace Accounts
    {
        public partial class CandyMachine
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 13649831137213787443UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{51, 173, 177, 113, 25, 241, 109, 189};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "9eM5CfcKCCt";
            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey TokenMint { get; set; }

            public ulong ItemsRedeemed { get; set; }

            public CandyMachineData Data { get; set; }

            public static CandyMachine Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                CandyMachine result = new CandyMachine();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Wallet = _data.GetPubKey(offset);
                offset += 32;
                if (_data.GetBool(offset++))
                {
                    result.TokenMint = _data.GetPubKey(offset);
                    offset += 32;
                }

                result.ItemsRedeemed = _data.GetU64(offset);
                offset += 8;
                offset += CandyMachineData.Deserialize(_data, offset, out var resultData);
                result.Data = resultData;
                return result;
            }
        }

        public partial class CollectionPDA
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 3845182396760569650UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{50, 183, 127, 103, 4, 213, 92, 53};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "9V1x5Jvbgur";
            public PublicKey Mint { get; set; }

            public PublicKey CandyMachine { get; set; }

            public static CollectionPDA Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                CollectionPDA result = new CollectionPDA();
                result.Mint = _data.GetPubKey(offset);
                offset += 32;
                result.CandyMachine = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum CandyMachineErrorKind : uint
        {
        }
    }

    namespace Types
    {
        public partial class WhitelistMintSettings
        {
            public WhitelistMintMode Mode { get; set; }

            public PublicKey Mint { get; set; }

            public bool Presale { get; set; }

            public ulong? DiscountPrice { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8((byte)Mode, offset);
                offset += 1;
                _data.WritePubKey(Mint, offset);
                offset += 32;
                _data.WriteBool(Presale, offset);
                offset += 1;
                if (DiscountPrice != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU64(DiscountPrice.Value, offset);
                    offset += 8;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out WhitelistMintSettings result)
            {
                int offset = initialOffset;
                result = new WhitelistMintSettings();
                result.Mode = (WhitelistMintMode)_data.GetU8(offset);
                offset += 1;
                result.Mint = _data.GetPubKey(offset);
                offset += 32;
                result.Presale = _data.GetBool(offset);
                offset += 1;
                if (_data.GetBool(offset++))
                {
                    result.DiscountPrice = _data.GetU64(offset);
                    offset += 8;
                }

                return offset - initialOffset;
            }
        }

        public partial class CandyMachineData
        {
            public string Uuid { get; set; }

            public ulong Price { get; set; }

            public string Symbol { get; set; }

            public ushort SellerFeeBasisPoints { get; set; }

            public ulong MaxSupply { get; set; }

            public bool IsMutable { get; set; }

            public bool RetainAuthority { get; set; }

            public long? GoLiveDate { get; set; }

            public EndSettings EndSettings { get; set; }

            public Creator[] Creators { get; set; }

            public HiddenSettings HiddenSettings { get; set; }

            public WhitelistMintSettings WhitelistMintSettings { get; set; }

            public ulong ItemsAvailable { get; set; }

            public GatekeeperConfig Gatekeeper { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Uuid, offset);
                _data.WriteU64(Price, offset);
                offset += 8;
                offset += _data.WriteBorshString(Symbol, offset);
                _data.WriteU16(SellerFeeBasisPoints, offset);
                offset += 2;
                _data.WriteU64(MaxSupply, offset);
                offset += 8;
                _data.WriteBool(IsMutable, offset);
                offset += 1;
                _data.WriteBool(RetainAuthority, offset);
                offset += 1;
                if (GoLiveDate != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteS64(GoLiveDate.Value, offset);
                    offset += 8;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (EndSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += EndSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                _data.WriteS32(Creators.Length, offset);
                offset += 4;
                foreach (var creatorsElement in Creators)
                {
                    offset += creatorsElement.Serialize(_data, offset);
                }

                if (HiddenSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += HiddenSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (WhitelistMintSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += WhitelistMintSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                _data.WriteU64(ItemsAvailable, offset);
                offset += 8;
                if (Gatekeeper != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += Gatekeeper.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out CandyMachineData result)
            {
                int offset = initialOffset;
                result = new CandyMachineData();
                offset += _data.GetBorshString(offset, out var resultUuid);
                result.Uuid = resultUuid;
                result.Price = _data.GetU64(offset);
                offset += 8;
                offset += _data.GetBorshString(offset, out var resultSymbol);
                result.Symbol = resultSymbol;
                result.SellerFeeBasisPoints = _data.GetU16(offset);
                offset += 2;
                result.MaxSupply = _data.GetU64(offset);
                offset += 8;
                result.IsMutable = _data.GetBool(offset);
                offset += 1;
                result.RetainAuthority = _data.GetBool(offset);
                offset += 1;
                if (_data.GetBool(offset++))
                {
                    result.GoLiveDate = _data.GetS64(offset);
                    offset += 8;
                }

                if (_data.GetBool(offset++))
                {
                    offset += EndSettings.Deserialize(_data, offset, out var resultEndSettings);
                    result.EndSettings = resultEndSettings;
                }

                uint resultCreatorsLength = _data.GetU32(offset);
                offset += 4;
                result.Creators = new Creator[resultCreatorsLength];
                for (uint resultCreatorsIdx = 0; resultCreatorsIdx < resultCreatorsLength; resultCreatorsIdx++)
                {
                    offset += Creator.Deserialize(_data, offset, out var resultCreatorsresultCreatorsIdx);
                    result.Creators[resultCreatorsIdx] = resultCreatorsresultCreatorsIdx;
                }

                if (_data.GetBool(offset++))
                {
                    offset += HiddenSettings.Deserialize(_data, offset, out var resultHiddenSettings);
                    result.HiddenSettings = resultHiddenSettings;
                }

                if (_data.GetBool(offset++))
                {
                    offset += WhitelistMintSettings.Deserialize(_data, offset, out var resultWhitelistMintSettings);
                    result.WhitelistMintSettings = resultWhitelistMintSettings;
                }

                result.ItemsAvailable = _data.GetU64(offset);
                offset += 8;
                if (_data.GetBool(offset++))
                {
                    offset += GatekeeperConfig.Deserialize(_data, offset, out var resultGatekeeper);
                    result.Gatekeeper = resultGatekeeper;
                }

                return offset - initialOffset;
            }
        }

        public partial class GatekeeperConfig
        {
            public PublicKey GatekeeperNetwork { get; set; }

            public bool ExpireOnUse { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(GatekeeperNetwork, offset);
                offset += 32;
                _data.WriteBool(ExpireOnUse, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out GatekeeperConfig result)
            {
                int offset = initialOffset;
                result = new GatekeeperConfig();
                result.GatekeeperNetwork = _data.GetPubKey(offset);
                offset += 32;
                result.ExpireOnUse = _data.GetBool(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }

        public partial class EndSettings
        {
            public EndSettingType EndSettingType { get; set; }

            public ulong Number { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8((byte)EndSettingType, offset);
                offset += 1;
                _data.WriteU64(Number, offset);
                offset += 8;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out EndSettings result)
            {
                int offset = initialOffset;
                result = new EndSettings();
                result.EndSettingType = (EndSettingType)_data.GetU8(offset);
                offset += 1;
                result.Number = _data.GetU64(offset);
                offset += 8;
                return offset - initialOffset;
            }
        }

        public partial class HiddenSettings
        {
            public string Name { get; set; }

            public string Uri { get; set; }

            public byte[] Hash { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Name, offset);
                offset += _data.WriteBorshString(Uri, offset);
                _data.WriteSpan(Hash, offset);
                offset += Hash.Length;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out HiddenSettings result)
            {
                int offset = initialOffset;
                result = new HiddenSettings();
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                offset += _data.GetBorshString(offset, out var resultUri);
                result.Uri = resultUri;
                result.Hash = _data.GetBytes(offset, 32);
                offset += 32;
                return offset - initialOffset;
            }
        }

        public partial class ConfigLine
        {
            public string Name { get; set; }

            public string Uri { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Name, offset);
                offset += _data.WriteBorshString(Uri, offset);
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out ConfigLine result)
            {
                int offset = initialOffset;
                result = new ConfigLine();
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                offset += _data.GetBorshString(offset, out var resultUri);
                result.Uri = resultUri;
                return offset - initialOffset;
            }
        }

        public partial class Creator
        {
            public PublicKey Address { get; set; }

            public bool Verified { get; set; }

            public byte Share { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(Address, offset);
                offset += 32;
                _data.WriteBool(Verified, offset);
                offset += 1;
                _data.WriteU8(Share, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out Creator result)
            {
                int offset = initialOffset;
                result = new Creator();
                result.Address = _data.GetPubKey(offset);
                offset += 32;
                result.Verified = _data.GetBool(offset);
                offset += 1;
                result.Share = _data.GetU8(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }

        public enum WhitelistMintMode : byte
        {
            BurnEveryTime,
            NeverBurn
        }

        public enum EndSettingType : byte
        {
            Date,
            Amount
        }
    }

    public partial class CandyMachineClient : TransactionalBaseClient<CandyMachineErrorKind>
    {
        public CandyMachineClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey("cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ"))
        {
        }

        public async Task<ProgramAccountsResultWrapper<List<CandyMachine>>> GetCandyMachinesAsync(string programAddress = "cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ", Commitment commitment = Commitment.Finalized)
        {
            var list = new List<MemCmp>{new MemCmp{Bytes = CandyMachine.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new ProgramAccountsResultWrapper<List<CandyMachine>>(res);
            List<CandyMachine> resultingAccounts = new List<CandyMachine>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => CandyMachine.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new ProgramAccountsResultWrapper<List<CandyMachine>>(res, resultingAccounts);
        }
        

        public async Task<AccountResultWrapper<CandyMachine>> GetCandyMachineAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new AccountResultWrapper<CandyMachine>(res);
            var resultingAccount = CandyMachine.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new AccountResultWrapper<CandyMachine>(res, resultingAccount);
        }

        public async Task<AccountResultWrapper<CollectionPDA>> GetCollectionPDAAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new AccountResultWrapper<CollectionPDA>(res);
            var resultingAccount = CollectionPDA.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new AccountResultWrapper<CollectionPDA>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeCandyMachineAsync(string accountAddress, Action<SubscriptionState, ResponseValue<AccountInfo>, CandyMachine> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                CandyMachine parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = CandyMachine.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeCollectionPDAAsync(string accountAddress, Action<SubscriptionState, ResponseValue<AccountInfo>, CollectionPDA> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                CollectionPDA parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = CollectionPDA.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendMintNftAsync(MintNftAccounts accounts, byte creatorBump, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId = null)
        {
            programId ??= new("cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ");
            TransactionInstruction instr = Program.CandyMachineProgram.MintNft(accounts, creatorBump, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }
        

        protected override Dictionary<uint, ProgramError<CandyMachineErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<CandyMachineErrorKind>>{};
        }
    }

    namespace Program
    {
        public class MintNftAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey CandyMachineCreator { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey MintAuthority { get; set; }

            public PublicKey UpdateAuthority { get; set; }

            public PublicKey MasterEdition { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }

            public PublicKey Clock { get; set; }

            public PublicKey RecentBlockhashes { get; set; }

            public PublicKey InstructionSysvarAccount { get; set; }
        }

        public class UpdateCandyMachineAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }
        }

        public class AddConfigLinesAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class InitializeCandyMachineAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }
        }

        public class SetCollectionAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CollectionPda { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey Edition { get; set; }

            public PublicKey CollectionAuthorityRecord { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }
        }

        public class RemoveCollectionAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CollectionPda { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey CollectionAuthorityRecord { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }
        }

        public class UpdateAuthorityAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }
        }

        public class WithdrawFundsAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }
        }

        public static class CandyMachineProgram
        {
            public static TransactionInstruction MintNft(MintNftAccounts accounts, byte creatorBump, PublicKey programId = null)
            {
                programId ??= new("cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ");
                List<AccountMeta> keys = new()
                {AccountMeta.Writable(accounts.CandyMachine, false), AccountMeta.ReadOnly(accounts.CandyMachineCreator, false), AccountMeta.ReadOnly(accounts.Payer, true), AccountMeta.Writable(accounts.Wallet, false), AccountMeta.Writable(accounts.Metadata, false), AccountMeta.Writable(accounts.Mint, false), AccountMeta.ReadOnly(accounts.MintAuthority, true), AccountMeta.ReadOnly(accounts.UpdateAuthority, true), AccountMeta.Writable(accounts.MasterEdition, false), AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false), AccountMeta.ReadOnly(accounts.TokenProgram, false), AccountMeta.ReadOnly(accounts.SystemProgram, false), AccountMeta.ReadOnly(accounts.Rent, false), AccountMeta.ReadOnly(accounts.Clock, false), AccountMeta.ReadOnly(accounts.RecentBlockhashes, false), AccountMeta.ReadOnly(accounts.InstructionSysvarAccount, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18096548587977980371UL, offset);
                offset += 8;
                _data.WriteU8(creatorBump, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

        }
    }
    
    public static class CandyMachineUtils{
        public static readonly PublicKey TokenMetadataProgramId = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");
        public static readonly PublicKey CandyMachineProgramId = new("cndy3Z4yapfJBmL3ShUp5exZKqR3z33thTzeNMm2gRZ");
        public static readonly PublicKey instructionSysVarAccount = new("Sysvar1nstructions1111111111111111111111111");
        
        /// <summary>
        /// Mint one token from the Candy Machine
        /// </summary>
        /// <param name="account">The target account used for minting the token</param>
        /// <param name="candyMachineKey">The CandyMachine public key</param>
        /// <param name="rpc">The RPC instance</param>
        public static async Task<Transaction> MintOneToken(Account account, PublicKey candyMachineKey, IRpcClient rpc)
        {
            var mint = new Account();
            var associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(account, mint.PublicKey);
            
            var candyMachineClient = new CandyMachineClient(rpc, null);
            var candyMachineWrap  = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
            var candyMachine = candyMachineWrap.ParsedResult;
            
            var (candyMachineCreator, creatorBump) = getCandyMachineCreator(candyMachineKey);
            

            var mintNftAccounts = new MintNftAccounts
            {
                CandyMachine = candyMachineKey,
                CandyMachineCreator = candyMachineCreator,
                Clock = SysVars.ClockKey,
                InstructionSysvarAccount = instructionSysVarAccount,
                MasterEdition = getMasterEdition(mint.PublicKey),
                Metadata = getMetadata(mint.PublicKey),
                Mint = mint.PublicKey,
                MintAuthority = account,
                Payer = account,
                RecentBlockhashes = SysVars.RecentBlockHashesKey,
                Rent = SysVars.RentKey,
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = TokenMetadataProgramId,
                TokenProgram = TokenProgram.ProgramIdKey,
                UpdateAuthority = account,
                Wallet = candyMachine.Wallet
            };

            var candyMachineInstruction = CandyMachineProgram.MintNft(mintNftAccounts, creatorBump);

            var blockHash = await rpc.GetLatestBlockHashAsync();
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
        }

        public static PublicKey getMasterEdition(PublicKey mint)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[]
                    {
                        Encoding.UTF8.GetBytes("metadata"),
                        TokenMetadataProgramId.KeyBytes,
                        mint.KeyBytes,
                        Encoding.UTF8.GetBytes("edition"),
                    },
                    TokenMetadataProgramId,
                    out PublicKey masterEdition, out _))
            {
                throw new InvalidProgramException();
            }
            return masterEdition;
        }
        
        public static PublicKey getMetadata(PublicKey mint)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[]
                    {
                        Encoding.UTF8.GetBytes("metadata"),
                        TokenMetadataProgramId.KeyBytes,
                        mint.KeyBytes,
                    },
                    TokenMetadataProgramId,
                    out PublicKey metadataAddress, out _))
            {
                throw new InvalidProgramException();
            }
            return metadataAddress;
        }
        
        public static (PublicKey candyMachineCreator, byte creatorBump) getCandyMachineCreator(PublicKey candyMachineAddress)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[] {Encoding.UTF8.GetBytes("candy_machine"), candyMachineAddress.KeyBytes},
                    CandyMachineProgramId,
                    out PublicKey candyMachineCreator,
                    out byte creatorBump))
            {
                throw new InvalidProgramException();
            }
            return (candyMachineCreator, creatorBump);
        }
    }
}