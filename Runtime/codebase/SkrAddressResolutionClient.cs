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

namespace Solana.Unity.SDK
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

        // Reverse-lookup table program for NFT-wrapped domains (devnet-as.magicblock.app).
        private const string WrappedLookupProgramAddress = "6eG7z75HG4FfNj4Zi83XKjZw1KYXmkAUMSVUH5C7VbrC";
        private static readonly byte[] WrappedLookupDiscriminator = { 78, 47, 200, 226, 220, 71, 95, 55 };

        private static readonly byte[] Zero32 = new byte[32];
        private static IRpcClient _nameResolutionRpcClient;
        private static IRpcClient NameResolutionRpcClient =>
            _nameResolutionRpcClient ??= ClientFactory.GetClient("https://api.mainnet-beta.solana.com");

        private static IRpcClient _lookupTableRpcClient;
        private static IRpcClient LookupTableRpcClient =>
            _lookupTableRpcClient ??= ClientFactory.GetClient("https://devnet-as.magicblock.app");

        /// <summary>
        /// Override the RPC endpoint used for domain resolution. Call before any resolution
        /// occurs. A private RPC (e.g. Helius, QuickNode) is required for reverse lookup
        /// since public endpoints restrict getProgramAccounts.
        /// </summary>
        public static void SetRpcUrl(string url) =>
            _nameResolutionRpcClient = ClientFactory.GetClient(url);

        /// <summary>
        /// Override the RPC endpoint used for the NFT-wrapped domain lookup table program.
        /// Defaults to devnet-as.magicblock.app.
        /// </summary>
        public static void SetLookupRpcUrl(string url) =>
            _lookupTableRpcClient = ClientFactory.GetClient(url);
        private static readonly object ReverseLookupLock = new();
        private static readonly Dictionary<string, ReverseLookupCacheEntry> ReverseLookupCache = new();
        private static readonly Dictionary<string, Task<string>> ReverseLookupInFlight = new();
        private static readonly TimeSpan ReverseLookupHitTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ReverseLookupMissTtl = TimeSpan.FromSeconds(20);

        private sealed class ReverseLookupCacheEntry
        {
            public string Domain;
            public DateTime ExpiresAtUtc;
        }

        public static async Task<string> ResolveDomainToAddress(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;

            try
            {
                return await ResolveDomainToAddressOnChain(domain).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> ResolveAddressToDomain(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var normalizedAddress = address.Trim();

            Task<string> lookupTask;
            lock (ReverseLookupLock)
            {
                if (TryGetCachedReverseLookup(normalizedAddress, out var cachedDomain))
                    return cachedDomain;

                if (!ReverseLookupInFlight.TryGetValue(normalizedAddress, out lookupTask))
                {
                    lookupTask = ResolveAddressToDomainOnChainSafe(normalizedAddress);
                    ReverseLookupInFlight[normalizedAddress] = lookupTask;

                    _ = lookupTask.ContinueWith(_ =>
                    {
                        lock (ReverseLookupLock)
                        {
                            if (ReverseLookupInFlight.TryGetValue(normalizedAddress, out var currentTask) && ReferenceEquals(currentTask, lookupTask))
                                ReverseLookupInFlight.Remove(normalizedAddress);
                        }
                    }, TaskScheduler.Default);
                }
            }

            var resolvedDomain = await lookupTask.ConfigureAwait(false);
            CacheReverseLookup(normalizedAddress, resolvedDomain);
            return resolvedDomain;
        }

        private static async Task<string> ResolveAddressToDomainOnChainSafe(string address)
        {
            try
            {
                var resolveTask = ResolveAddressToDomainOnChain(address);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var winner = await Task.WhenAny(resolveTask, timeoutTask).ConfigureAwait(false);
                if (winner != resolveTask)
                    return null;
                return await resolveTask.ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
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

            var rawData = await GetRawAccountData(domainAccount).ConfigureAwait(false);

            if (rawData.Length < NameRecordHeaderLength)
                return null;

            var expiresAt = ReadUInt64LittleEndian(rawData, ExpiresAtOffset);
            var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expiresAt != 0 && expiresAt < now)
                return null;

            var ownerBytes = SliceBytes(rawData, OwnerOffset, PublicKeyLength);
            if (ownerBytes.All(b => b == 0))
                return null;

            var owner = new PublicKey(ownerBytes);
            var actualOwner = await ResolveWrappedDomainOwner(domainAccount, tldHouseAccount, owner).ConfigureAwait(false);
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

            // Second filter narrows to accounts directly owned by the target wallet (non-wrapped domains).
            // NFT-wrapped domains store the nftRecordPda at OwnerOffset so they won't match here;
            // they are handled below via the wrapped-domain lookup table program.
            var filters = new List<MemCmp>
            {
                new() { Offset = 8, Bytes = tldParentAccount.ToString() },
                new() { Offset = OwnerOffset, Bytes = owner.ToString() }
            };
            var accounts = await NameResolutionRpcClient.GetProgramAccountsAsync(AnsProgramId, memCmpList: filters).ConfigureAwait(false);

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

                var nameRecordRaw = await GetRawAccountData(nameAccount).ConfigureAwait(false);
                if (nameRecordRaw.Length < NameRecordHeaderLength)
                    continue;

                var expiresAt = ReadUInt64LittleEndian(nameRecordRaw, ExpiresAtOffset);
                var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (expiresAt != 0 && expiresAt < now)
                    continue;

                var recordOwnerBytes = SliceBytes(nameRecordRaw, OwnerOffset, PublicKeyLength);
                if (recordOwnerBytes.All(b => b == 0))
                    continue;

                var recordOwner = new PublicKey(recordOwnerBytes);
                var actualOwner = await ResolveWrappedDomainOwner(nameAccount, tldHouseAccount, recordOwner).ConfigureAwait(false);
                if (actualOwner?.Key != owner.Key)
                    continue;

                var reverseLookupRaw = await GetRawAccountData(reverseLookupAccount).ConfigureAwait(false);
                if (reverseLookupRaw.Length <= NameRecordHeaderLength)
                    continue;

                var label = ExtractUtf8Label(reverseLookupRaw);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                if (!TryDeriveNameAccount(label, tldParentAccount, out var forwardResolvedNameAccount))
                    continue;

                if (forwardResolvedNameAccount.Key != nameAccount.Key)
                    continue;

                return $"{label}.{SuffixSkr}";
            }

            return await ResolveWrappedDomainLabel(owner, tldParentAccount).ConfigureAwait(false);
        }

        private static async Task<string> ResolveWrappedDomainLabel(PublicKey owner, PublicKey tldParentAccount)
        {
            var reverseLookupAccount = await GetWrappedReverseLookupAccount(owner).ConfigureAwait(false);
            if (reverseLookupAccount == null)
                return null;

            var reverseLookupRaw = await GetRawAccountData(reverseLookupAccount).ConfigureAwait(false);
            if (reverseLookupRaw.Length <= NameRecordHeaderLength)
                return null;

            var label = ExtractUtf8Label(reverseLookupRaw);
            if (string.IsNullOrWhiteSpace(label))
                return null;

            if (!TryDeriveNameAccount(label, tldParentAccount, out var forwardResolvedNameAccount))
                return null;

            var nameRecordRaw = await GetRawAccountData(forwardResolvedNameAccount).ConfigureAwait(false);
            if (nameRecordRaw.Length < NameRecordHeaderLength)
                return null;

            var expiresAt = ReadUInt64LittleEndian(nameRecordRaw, ExpiresAtOffset);
            var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expiresAt != 0 && expiresAt < now)
                return null;

            return $"{label}.{SuffixSkr}";
        }

        private static async Task<PublicKey> GetWrappedReverseLookupAccount(PublicKey pk)
        {
            var programKey = new PublicKey(WrappedLookupProgramAddress);

            if (!PublicKey.TryFindProgramAddress(
                    new[] { Encoding.UTF8.GetBytes("table"), new[] { pk.KeyBytes[0] } },
                    programKey,
                    out var tableAccount,
                    out _))
                return null;

            var bhResult = await LookupTableRpcClient.GetLatestBlockHashAsync().ConfigureAwait(false);
            if (bhResult?.Result?.Value?.Blockhash == null)
                return null;

            // Ephemeral keypair used only as fee-payer for simulation; signature validity is not checked.
            var feePayer = new Account();

            var transaction = new Transaction
            {
                FeePayer = feePayer.PublicKey,
                Instructions = new List<TransactionInstruction>
                {
                    new()
                    {
                        ProgramId = programKey,
                        Data = WrappedLookupDiscriminator.Concat(pk.KeyBytes).ToArray(),
                        Keys = new List<AccountMeta>
                        {
                            AccountMeta.ReadOnly(tableAccount, false),
                        }
                    }
                },
                Signatures = new List<SignaturePubKeyPair>(),
                RecentBlockHash = bhResult.Result.Value.Blockhash,
            };

            var rawTransaction = transaction.Build(feePayer);
            var result = await LookupTableRpcClient.SimulateTransactionAsync(rawTransaction).ConfigureAwait(false);

            var returnValue = result?.Result?.Value?.Logs?
                .FirstOrDefault(l => l.StartsWith("Program return:"))
                ?.Split(' ');

            if (returnValue == null || returnValue.Length < 4)
                return null;

            try
            {
                return new PublicKey(Convert.FromBase64String(returnValue[3]));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ExtractUtf8Label(byte[] rawData)
        {
            var payload = SliceBytes(rawData, NameRecordHeaderLength, rawData.Length - NameRecordHeaderLength);
            var end = Array.IndexOf(payload, (byte)0);
            if (end < 0)
                end = payload.Length;
            return Encoding.UTF8.GetString(payload, 0, end).Trim();
        }

        private static bool TryGetCachedReverseLookup(string address, out string domain)
        {
            lock (ReverseLookupLock)
            {
                if (ReverseLookupCache.TryGetValue(address, out var entry))
                {
                    if (entry.ExpiresAtUtc > DateTime.UtcNow)
                    {
                        domain = entry.Domain;
                        return true;
                    }

                    ReverseLookupCache.Remove(address);
                }
            }

            domain = null;
            return false;
        }

        private static void CacheReverseLookup(string address, string domain)
        {
            var ttl = string.IsNullOrEmpty(domain) ? ReverseLookupMissTtl : ReverseLookupHitTtl;
            var cacheEntry = new ReverseLookupCacheEntry
            {
                Domain = domain,
                ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
            };

            lock (ReverseLookupLock)
            {
                ReverseLookupCache[address] = cacheEntry;
            }
        }

        private static async Task<byte[]> GetRawAccountData(PublicKey accountKey)
        {
            var response = await NameResolutionRpcClient.GetAccountInfoAsync(accountKey, Commitment.Confirmed).ConfigureAwait(false);
            var accountData = response?.Result?.Value?.Data;
            if (accountData == null || accountData.Count == 0)
                return Array.Empty<byte>();

            if (accountData.Count > 1 && !string.Equals(accountData[1], "base64", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<byte>();

            var encodedData = accountData[0];
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

            // Domain is NFT-wrapped: ownerFromNameRecord is the NFT record PDA, not a wallet.
            // All error paths below must return null — returning ownerFromNameRecord here
            // would resolve to a PDA and cause transfers to be sent to an invalid address.
            var nftRecordRaw = await GetRawAccountData(nftRecordPda).ConfigureAwait(false);
            if (nftRecordRaw.Length < NftRecordMintOffset + PublicKeyLength)
                return null;

            var tag = nftRecordRaw[NftRecordTagOffset];
            if (tag != 1) // Tag.ActiveRecord
                return null;

            var mint = new PublicKey(SliceBytes(nftRecordRaw, NftRecordMintOffset, PublicKeyLength));
            var largestAccounts = await NameResolutionRpcClient.GetTokenLargestAccountsAsync(mint, Commitment.Confirmed).ConfigureAwait(false);
            var largestTokenAccount = largestAccounts?.Result?.Value?.FirstOrDefault(a => a.AmountUlong > 0);
            if (largestTokenAccount == null)
                return null;

            PublicKey tokenAccount;
            try
            {
                tokenAccount = new PublicKey(largestTokenAccount.Address);
            }
            catch (Exception)
            {
                return null;
            }

            var tokenAccountRaw = await GetRawAccountData(tokenAccount).ConfigureAwait(false);
            if (tokenAccountRaw.Length < SplTokenOwnerOffset + PublicKeyLength)
                return null;

            return new PublicKey(SliceBytes(tokenAccountRaw, SplTokenOwnerOffset, PublicKeyLength));
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
            if (parts.Length != 2 || parts[1] != SuffixSkr)
                return false;

            label = parts[0];
            tld = parts[1];
            return !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(tld);
        }

        private static byte[] SliceBytes(byte[] source, int offset, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }
    }
}
