using UnityEngine.Scripting;

namespace Solana.Unity.SolanaMobileStack
{
    [Preserve]
    public sealed class AuthorizationRecord
    {
        public int SchemaVersion { get; set; } = 1;

        public string AuthToken { get; set; }

        public string AccountAddress { get; set; }

        public string AccountLabel { get; set; }

        public string AccountIcon { get; set; }

        public string[] Chains { get; set; }

        public string[] Features { get; set; }

        public string WalletUriBase { get; set; }

        public string WalletIcon { get; set; }

        public string Chain { get; set; }

        public long CachedAtUnixSeconds { get; set; }
    }
}
