using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Solana.Unity.SolanaMobileStack
{
    public static class AuthorizationResponseParser
    {
        // Size caps
        private const int IconMaxBytes = 64 * 1024;
        private const int DisplayAddressMaxChars = 128;
        private const int ChainsMaxEntries = 32;
        private const int FeaturesMaxEntries = 32;
        private const int AccountsMaxEntries = 16;
        private const int PublicKeyByteLength = 32;

        public static AuthorizationResult Parse(JToken result) => Parse(result, LogVerbosity.Default);

        public static AuthorizationResult Parse(JToken result, LogVerbosity verbosity)
        {
            if (result == null || result.Type != JTokenType.Object)
                throw new InvalidAuthorizationException("authorize response is not a JSON object");

            // (1) auth_token required, non-empty, non-whitespace.
            string? authToken = (string?)result["auth_token"];
            if (string.IsNullOrWhiteSpace(authToken))
                throw new InvalidAuthorizationException("auth_token is empty or whitespace");

            // (2) accounts array length must fall in [1, AccountsMaxEntries].
            JToken? accountsToken = result["accounts"];
            if (accountsToken == null || accountsToken.Type != JTokenType.Array)
                throw new InvalidAuthorizationException("accounts is missing or not an array");

            var accountsArr = (JArray)accountsToken;
            if (accountsArr.Count == 0)
                throw new InvalidAuthorizationException("accounts is empty");
            if (accountsArr.Count > AccountsMaxEntries)
                throw new InvalidAuthorizationException(
                    $"accounts has {accountsArr.Count} entries; max is {AccountsMaxEntries} ");

            // accounts[0].address base64 + 32-byte validation
            string? primaryAddress = (string?)accountsArr[0]?["address"];
            if (string.IsNullOrWhiteSpace(primaryAddress))
                throw new InvalidAuthorizationException("accounts[0].address is missing or empty");
            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(primaryAddress);
            }
            catch (FormatException fx)
            {
                throw new InvalidAuthorizationException(
                    "accounts[0].address is not valid base64", fx);
            }
            if (decoded.Length != PublicKeyByteLength)
                throw new InvalidAuthorizationException(
                    $"accounts[0].address decodes to {decoded.Length} bytes; expected {PublicKeyByteLength}");

            // (4 + 5) Build AccountInfo list with size-cap drops / truncations.
            var accounts = new List<AccountInfo>(accountsArr.Count);
            var droppedFields = new List<string>();
            for (int i = 0; i < accountsArr.Count; i++)
            {
                var entry = accountsArr[i];
                string? icon = (string?)entry["icon"];
                string? displayAddress = (string?)entry["display_address"];
                var chainsArr = entry["chains"] as JArray;
                var featuresArr = entry["features"] as JArray;

                // icon > 64 KiB post-base64-decode → null
                if (!string.IsNullOrEmpty(icon))
                {
                    int iconBytes = EstimateBase64DecodedLength(ExtractBase64Payload(icon));
                    if (iconBytes > IconMaxBytes)
                    {
                        Debug.Log($"[MWA parse] dropped accounts[{i}].icon — observed length {iconBytes} bytes (cap {IconMaxBytes})");
                        droppedFields.Add($"accounts[{i}].icon");
                        icon = null;
                    }
                }

                // display_address > 128 chars → null
                if (!string.IsNullOrEmpty(displayAddress) && displayAddress!.Length > DisplayAddressMaxChars)
                {
                    Debug.Log($"[MWA parse] dropped accounts[{i}].display_address — observed length {displayAddress.Length} chars (cap {DisplayAddressMaxChars})");
                    droppedFields.Add($"accounts[{i}].display_address");
                    displayAddress = null;
                }

                // chains > 32 entries → truncate to first 32 and log
                string[]? chains = null;
                if (chainsArr != null)
                {
                    if (chainsArr.Count > ChainsMaxEntries)
                    {
                        Debug.Log($"[MWA parse] dropped accounts[{i}].chains — observed length {chainsArr.Count} entries (cap {ChainsMaxEntries}) — truncating to first {ChainsMaxEntries}");
                        droppedFields.Add($"accounts[{i}].chains");
                    }
                    int take = Math.Min(chainsArr.Count, ChainsMaxEntries);
                    chains = new string[take];
                    for (int j = 0; j < take; j++) chains[j] = (string?)chainsArr[j] ?? string.Empty;
                }

                // features > 32 entries → truncate to first 32 and log
                string[]? features = null;
                if (featuresArr != null)
                {
                    if (featuresArr.Count > FeaturesMaxEntries)
                    {
                        Debug.Log($"[MWA parse] dropped accounts[{i}].features — observed length {featuresArr.Count} entries (cap {FeaturesMaxEntries}) — truncating to first {FeaturesMaxEntries}");
                        droppedFields.Add($"accounts[{i}].features");
                    }
                    int take = Math.Min(featuresArr.Count, FeaturesMaxEntries);
                    features = new string[take];
                    for (int j = 0; j < take; j++) features[j] = (string?)featuresArr[j] ?? string.Empty;
                }

                accounts.Add(new AccountInfo
                {
                    Address = (string?)entry["address"] ?? string.Empty,
                    DisplayAddress = displayAddress,
                    DisplayAddressFormat = (string?)entry["display_address_format"],
                    Icon = icon,
                    Chains = chains,
                    Features = features,
                    Label = (string?)entry["label"],
                });
            }

            string? walletUriBase = (string?)result["wallet_uri_base"];

            // Wallet-level wallet_icon subject to 64 KiB post-decode cap .
            string? walletIcon = (string?)result["wallet_icon"];
            if (!string.IsNullOrEmpty(walletIcon))
            {
                int walletIconBytes = EstimateBase64DecodedLength(ExtractBase64Payload(walletIcon));
                if (walletIconBytes > IconMaxBytes)
                {
                    Debug.Log($"[MWA parse] dropped wallet_icon — observed length {walletIconBytes} bytes (cap {IconMaxBytes})");
                    droppedFields.Add("wallet_icon");
                    walletIcon = null;
                }
            }

            // (6) sign_in_result — optional SIWS response; parsed when present.
            JToken? signInResultToken = result["sign_in_result"];
            bool signInResultPresent = signInResultToken != null && signInResultToken.Type != JTokenType.Null;
            SignInResult signInResult = null;
            if (signInResultPresent)
            {
                signInResult = new SignInResult
                {
                    Address = (string?)signInResultToken["address"],
                    SignedMessage = (string?)signInResultToken["signed_message"],
                    Signature = (string?)signInResultToken["signature"],
                    SignatureType = (string?)signInResultToken["signature_type"],
                };
            }

            // Structured parse log — gated by MWA_VERBOSE compile symbol or runtime verbosity
#if MWA_VERBOSE
            LogStructured(result, droppedFields, signInResultPresent);
#else
            if (verbosity == LogVerbosity.Verbose)
                LogStructured(result, droppedFields, signInResultPresent);
#endif

            return new AuthorizationResult
            {
                AuthToken = authToken!,
                Accounts = accounts,
                WalletUriBase = walletUriBase,
                WalletIcon = walletIcon,
                SignInResult = signInResult,
            };
        }

        private static string ExtractBase64Payload(string dataUri)
        {
            if (string.IsNullOrEmpty(dataUri)) return dataUri;
            if (!dataUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return dataUri;
            int commaIdx = dataUri.IndexOf(',');
            if (commaIdx < 0) return dataUri;
            return dataUri.Substring(commaIdx + 1);
        }

        private static int EstimateBase64DecodedLength(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return 0;
            int len = b64.Length;
            int padding = 0;
            if (len >= 1 && b64[len - 1] == '=') padding++;
            if (len >= 2 && b64[len - 2] == '=') padding++;
            return (len * 3 / 4) - padding;
        }

        private static void LogStructured(JToken result, List<string> droppedFields, bool signInResultPresent)
        {
            var knownKeys = new HashSet<string> { "auth_token", "accounts", "wallet_uri_base", "wallet_icon", "sign_in_result" };
            var presentOptional = new List<string>();
            var absentOptional = new List<string>();
            var unknownKeys = new List<string>();
            foreach (var opt in new[] { "wallet_uri_base", "wallet_icon", "sign_in_result" })
            {
                if (result[opt] != null && result[opt]!.Type != JTokenType.Null) presentOptional.Add(opt);
                else absentOptional.Add(opt);
            }
            foreach (var prop in ((JObject)result).Properties())
                if (!knownKeys.Contains(prop.Name)) unknownKeys.Add(prop.Name);

            Debug.Log(
                $"[MWA parse] present=[{string.Join(",", presentOptional)}] " +
                $"absent=[{string.Join(",", absentOptional)}] " +
                $"unknown=[{string.Join(",", unknownKeys)}] " +
                $"dropped=[{string.Join(",", droppedFields)}] " +
                $"sign_in_result_present={signInResultPresent}");
        }
    }
}
