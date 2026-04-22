using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;

namespace Solana.Unity.SDK.Example.Services
{
    /// <summary>
    /// On-chain .skr resolver (no backend dependency).
    /// </summary>
    public static class SkrAddressResolutionClient
    {
        private const string OriginTld = "ANS";
        private const string SuffixSkr = "skr";
        private const string HashPrefix = "ALT Name Service";
        private const string TldHousePrefix = "tld_house";
        private const string NameHousePrefix = "name_house";
        private const string NftRecordPrefix = "nft_record";
        private const int OwnerOffset = 40;
        private const int ExpiresAtOffset = 104;
        private const int NameRecordHeaderLength = 200;
        private const int NftRecordTagOffset = 8;
        private const int NftRecordMintOffset = 74;
        private const int SplTokenOwnerOffset = 32;
        private const int PublicKeyLength = 32;

        private static readonly PublicKey AnsProgramId =
            new("ALTNSZ46uaAUU7XUV6awvdorLGqAsPwa9shm7h4uP2FK");
        private static readonly PublicKey TldHouseProgramId =
            new("TLDHkysf5pCnKsVA4gXpNvmy7psXLPEu4LAdDJthT9S");
        private static readonly PublicKey NameHouseProgramId =
            new("NH3uX6FtVE2fNREAioP7hm5RaozotZxeL6khU1EHx51");

        private static readonly byte[] Zero32 = new byte[32];
        private static readonly IRpcClient NameResolutionRpcClient =
            ClientFactory.GetClient("https://api.mainnet-beta.solana.com");

        public static async Task<string> ResolveDomainToAddress(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;
            return await ResolveDomainToAddressOnChain(domain);
        }

        public static async Task<string> ResolveAddressToDomain(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;
            return await ResolveAddressToDomainOnChain(address);
        }

        private static async Task<string> ResolveDomainToAddressOnChain(string domainTld)
        {
            if (NameResolutionRpcClient == null)
                return null;

            if (!TrySplitDomain(domainTld, out var label, out var tld))
                return null;

            if (!TryDeriveNameAccount(OriginTld, null, out var originNameAccount))
                return null;

            if (!TryDeriveNameAccount($".{tld}", originNameAccount, out var tldParentAccount))
                return null;

            if (!TryDeriveNameAccount(label, tldParentAccount, out var domainAccount))
                return null;

            if (!PublicKey.TryFindProgramAddress(
                    new[] { Encoding.UTF8.GetBytes(TldHousePrefix), Encoding.UTF8.GetBytes($".{tld}") },
                    TldHouseProgramId,
                    out var tldHouseAccount,
                    out _))
            {
                return null;
            }

            var rawData = await GetRawAccountData(domainAccount);

            if (rawData.Length < NameRecordHeaderLength)
                return null;

            var expiresAt = ReadUInt64LittleEndian(rawData, ExpiresAtOffset);
            var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expiresAt != 0 && expiresAt < now)
                return null;

            var ownerBytes = rawData.Skip(OwnerOffset).Take(32).ToArray();
            if (ownerBytes.All(b => b == 0))
                return null;

            var owner = new PublicKey(ownerBytes);
            var actualOwner = await ResolveWrappedDomainOwner(domainAccount, tldHouseAccount, owner);
            return actualOwner?.Key;
        }

        private static async Task<string> ResolveAddressToDomainOnChain(string address)
        {
            if (NameResolutionRpcClient == null)
                return null;

            PublicKey owner;
            try
            {
                owner = new PublicKey(address.Trim());
            }
            catch (Exception)
            {
                return null;
            }

            if (!TryDeriveNameAccount(OriginTld, null, out var originNameAccount))
                return null;

            if (!TryDeriveNameAccount($".{SuffixSkr}", originNameAccount, out var tldParentAccount))
                return null;

            if (!PublicKey.TryFindProgramAddress(
                    new[] { Encoding.UTF8.GetBytes(TldHousePrefix), Encoding.UTF8.GetBytes($".{SuffixSkr}") },
                    TldHouseProgramId,
                    out var tldHouseAccount,
                    out _))
            {
                return null;
            }

            var filters = new List<MemCmp>
            {
                new() { Offset = 8, Bytes = tldParentAccount.ToString() },
                new() { Offset = OwnerOffset, Bytes = owner.ToString() }
            };
            var accounts = await NameResolutionRpcClient.GetProgramAccountsAsync(AnsProgramId, memCmpList: filters);
            if (accounts?.Result == null || accounts.Result.Count == 0)
                return null;

            foreach (var account in accounts.Result)
            {
                PublicKey nameAccount;
                try
                {
                    nameAccount = new PublicKey(account.PublicKey.ToString());
                }
                catch (Exception)
                {
                    continue;
                }

                if (!TryDeriveNameAccount(nameAccount.Key, null, out var reverseLookupAccount, tldHouseAccount))
                    continue;

                var reverseLookupRaw = await GetRawAccountData(reverseLookupAccount);
                if (reverseLookupRaw.Length <= NameRecordHeaderLength)
                    continue;

                var label = ExtractUtf8Label(reverseLookupRaw);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                return $"{label}.{SuffixSkr}";
            }

            return null;
        }

        private static string ExtractUtf8Label(byte[] rawData)
        {
            var payload = rawData.Skip(NameRecordHeaderLength).ToArray();
            var end = Array.IndexOf(payload, (byte)0);
            if (end < 0)
                end = payload.Length;
            return Encoding.UTF8.GetString(payload, 0, end).Trim();
        }

        private static async Task<byte[]> GetRawAccountData(PublicKey accountKey)
        {
            var response = await NameResolutionRpcClient.GetAccountInfoAsync(accountKey, Commitment.Confirmed);
            var encodedData = response?.Result?.Value?.Data?[0];
            if (string.IsNullOrEmpty(encodedData))
                return Array.Empty<byte>();

            try
            {
                return Convert.FromBase64String(encodedData);
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }

        private static async Task<PublicKey> ResolveWrappedDomainOwner(
            PublicKey nameAccount,
            PublicKey tldHouseAccount,
            PublicKey ownerFromNameRecord)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[] { Encoding.UTF8.GetBytes(NameHousePrefix), tldHouseAccount.KeyBytes },
                    NameHouseProgramId,
                    out var nameHouse,
                    out _))
            {
                return ownerFromNameRecord;
            }

            if (!PublicKey.TryFindProgramAddress(
                    new[]
                    {
                        Encoding.UTF8.GetBytes(NftRecordPrefix),
                        nameHouse.KeyBytes,
                        nameAccount.KeyBytes
                    },
                    NameHouseProgramId,
                    out var nftRecordPda,
                    out _))
            {
                return ownerFromNameRecord;
            }

            if (ownerFromNameRecord.Key != nftRecordPda.Key)
                return ownerFromNameRecord;

            var nftRecordRaw = await GetRawAccountData(nftRecordPda);
            if (nftRecordRaw.Length < NftRecordMintOffset + PublicKeyLength)
                return ownerFromNameRecord;

            var tag = nftRecordRaw[NftRecordTagOffset];
            if (tag != 1) // Tag.ActiveRecord
                return ownerFromNameRecord;

            var mint = new PublicKey(nftRecordRaw.Skip(NftRecordMintOffset).Take(PublicKeyLength).ToArray());
            var largestAccounts = await NameResolutionRpcClient.GetTokenLargestAccountsAsync(mint, Commitment.Confirmed);
            var largestTokenAccount = largestAccounts?.Result?.Value?.FirstOrDefault(a => a.AmountUlong > 0);
            if (largestTokenAccount == null)
                return ownerFromNameRecord;

            PublicKey tokenAccount;
            try
            {
                tokenAccount = new PublicKey(largestTokenAccount.Address);
            }
            catch (Exception)
            {
                return ownerFromNameRecord;
            }

            var tokenAccountRaw = await GetRawAccountData(tokenAccount);
            if (tokenAccountRaw.Length < SplTokenOwnerOffset + PublicKeyLength)
                return ownerFromNameRecord;

            return new PublicKey(tokenAccountRaw.Skip(SplTokenOwnerOffset).Take(PublicKeyLength).ToArray());
        }

        private static ulong ReadUInt64LittleEndian(byte[] data, int offset)
        {
            if (data == null || data.Length < offset + sizeof(ulong))
                return 0;

            ulong value = 0;
            for (var i = 0; i < sizeof(ulong); i++)
            {
                value |= ((ulong)data[offset + i]) << (8 * i);
            }

            return value;
        }

        private static bool TryDeriveNameAccount(
            string name,
            PublicKey parentName,
            out PublicKey nameAccount,
            PublicKey nameClass = null)
        {
            var hashedName = GetHashedName(name);
            var classSeed = nameClass?.KeyBytes ?? Zero32;
            var parentSeed = parentName?.KeyBytes ?? Zero32;

            return PublicKey.TryFindProgramAddress(
                new[] { hashedName, classSeed, parentSeed },
                AnsProgramId,
                out nameAccount,
                out _);
        }

        private static byte[] GetHashedName(string name)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes($"{HashPrefix}{name}"));
        }

        private static bool TrySplitDomain(string input, out string label, out string tld)
        {
            label = null;
            tld = null;

            var parts = input.Trim().ToLowerInvariant().Split('.');
            if (parts.Length < 2)
                return false;

            label = parts[^2];
            tld = parts[^1];
            return !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(tld);
        }

    }
}
