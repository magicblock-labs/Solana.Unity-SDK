using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// Resolves which installed wallet to target for MWA association.
    /// </summary>
    /// <remarks>
    /// The SDK stays platform-agnostic, so it does NOT query the Android
    /// PackageManager for installed wallets — that would require a
    /// <c>&lt;queries&gt;</c> declaration in the consuming app's manifest, which
    /// the SDK has no business shipping. Instead: the first association fires an
    /// untargeted intent and lets the OS show its wallet chooser; the chosen
    /// wallet is then identified from the authorization's account label
    /// (<see cref="ResolvePackageFromLabel"/>) and cached, so subsequent
    /// associations can target it directly.
    /// </remarks>
    public static class MwaWalletDiscovery
    {
        /// <summary>
        /// Known mapping of MWA account labels to Android package names.
        /// Used to identify the wallet after a successful authorization.
        /// </summary>
        private static readonly Dictionary<string, string> KnownWalletPackages = new(StringComparer.OrdinalIgnoreCase)
        {
            { "phantom-wallet", "app.phantom" },
            { "phantom", "app.phantom" },
            { "solflare", "com.solflare.mobile" },
            { "backpack", "app.backpack" },
            { "jupiter", "ag.jup.jupiter.android" },
            { "jupiter-wallet", "ag.jup.jupiter.android" },
            { "glow", "com.luma.wallet.prod" },
            { "ultimate", "com.aspect.ultimate" },
            { "tiplink", "xyz.tiplink.wallet" },
        };

        /// <summary>
        /// Attempts to map an MWA account label (from
        /// <see cref="AuthorizationResult.AccountLabel"/>) to an Android package
        /// name using the known-wallets table. Returns <c>null</c> for
        /// unrecognised labels.
        /// </summary>
        public static string ResolvePackageFromLabel(string accountLabel)
        {
            if (string.IsNullOrEmpty(accountLabel))
                return null;

            if (KnownWalletPackages.TryGetValue(accountLabel, out var package))
            {
                Debug.Log($"[MWA][Discovery] Mapped label \"{accountLabel}\" → {package}");
                return package;
            }

            Debug.Log($"[MWA][Discovery] Unknown wallet label: \"{accountLabel}\"");
            return null;
        }

        /// <summary>
        /// Returns the cached wallet package to target, or <c>null</c> when none
        /// is cached — in which case the caller should fire an untargeted intent
        /// so the OS shows its wallet chooser.
        /// </summary>
        public static async Task<string> ResolveWalletPackage(IMwaWalletSelectionCache cache)
        {
            if (cache == null)
                return null;

            var cached = await cache.GetSelectedWalletPackage();
            if (!string.IsNullOrEmpty(cached))
            {
                Debug.Log($"[MWA][Discovery] Using cached wallet: {cached}");
                return cached;
            }

            return null;
        }
    }
}
