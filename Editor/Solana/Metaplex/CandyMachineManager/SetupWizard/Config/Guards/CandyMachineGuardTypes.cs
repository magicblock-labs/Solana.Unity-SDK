using Newtonsoft.Json;
using Solana.Unity.Metaplex.CandyGuard;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Programs.Utilities;
using System;
using UnityEngine;

using static Solana.Unity.SDK.Metaplex.CandyGuardMintSettings;

namespace Solana.Unity.SDK.Editor
{

    /// <summary>
    /// Abstract representation of a serializable CandyMachine guard for creating configs
    /// in the editor.
    /// 
    /// Field <see cref="enabled"/>: Controls whether this guard is being used, as guards
    /// can't be made null within a <see cref="ScriptableObject"/>.
    /// </summary>
    internal abstract class CandyMachineGuard
    {
        [SerializeField]
        internal bool enabled;
    }

    [Serializable]
    internal class MintLimitGuard : CandyMachineGuard
    {

        [ShowWhen("enabled"), SerializeField, JsonProperty, Range(0, 255)]
        private int id;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int limit;

        internal MintLimit CandyGuardParam => new() { Id = (byte)id, Limit = (ushort)limit };

        internal MintLimitMintSettings GetMintSettings()
        {
            return enabled ? new() { Id = id } : null;
        }
    }

    [Serializable]
    internal class AddressGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string address;

        internal AddressGate CandyGuardParam => new() { Address = new(address) };
    }

    [Serializable]
    internal class AllowListGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string merkleRoot;

        internal AllowList CandyGuardParam => new() { MerkleRoot = KeyStore.Utils.HexToByteArray(merkleRoot) };
        internal AllowListMintSettings GetMintSettings()
        {
            return enabled ? new() { 
                MerkleRoot = KeyStore.Utils.HexToByteArray(merkleRoot) 
            }: null;
        }
    }

    [Serializable]
    internal class BotTaxGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private float value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private bool lastInstruction;

        internal BotTax CandyGuardParam => new() { 
            Lamports = (ulong)(value * SolHelper.LAMPORTS_PER_SOL), 
            LastInstruction = lastInstruction 
        };
    }

    [Serializable]
    internal class EndDateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string date;

        internal EndDate CandyGuardParam => new() { Date = DateTimeOffset.Parse(date).ToUnixTimeSeconds() };
    }

    [Serializable]
    internal class GateKeeperGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string gatekeeperNetwork;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private bool expireOnUse;

        internal Gatekeeper CandyGuardParam => new() {
            ExpireOnUse = expireOnUse,
            GatekeeperNetwork = new(gatekeeperNetwork)
        };

        internal GatekeeperMintSettings GetMintSettings()
        {
            return enabled ? new() { 
                ExpireOnUse = expireOnUse,
                Network = new(gatekeeperNetwork) 
            } : null;
        }
    }

    [Serializable]
    internal class NFTBurnGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        internal NftBurn CandyGuardParam => new() {
            RequiredCollection = new(requiredCollection)
        };

        internal NftBurnMintSettings GetMintSettings(MetadataAccount[] tokenAccounts)
        {
            foreach (var account in tokenAccounts) 
            {
                if (account?.metadata?.collectionLink?.key?.Key == requiredCollection) {
                    return new NftBurnMintSettings() {
                        Mint = new(account.mint),
                        RequiredCollection = new(requiredCollection),
                        TokenStandard = (byte)account.metadata.tokenStandard
                    };
                }
            }
            return null;
        }
    }

    [Serializable]
    internal class NFTGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        internal NftGate CandyGuardParam => new() {
            RequiredCollection = new(requiredCollection)
        };

        internal NftGateMintSettings GetMintSettings(MetadataAccount[] tokenAccounts)
        {
            foreach (var account in tokenAccounts) {
                if (account?.metadata?.collectionLink?.key?.Key == requiredCollection) {
                    return new NftGateMintSettings() {
                        Mint = new(account.mint)
                    };
                }
            }
            return null;
        }
    }

    [Serializable]
    internal class NFTPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;

        internal NftPayment CandyGuardParam => new() {
            Destination = new(destination),
            RequiredCollection = new(requiredCollection)
        };

        internal NftPaymentMintSettings GetMintSettings(MetadataAccount[] tokenAccounts)
        {
            foreach (var account in tokenAccounts) {
                if (account?.metadata?.collectionLink?.key?.Key == requiredCollection) {
                    return new NftPaymentMintSettings() {
                        Mint = new(account.mint),
                        Destination = new (destination),
                        TokenStandard = (byte)account.metadata.tokenStandard
                    };
                }
            }
            return null;
        }
    }

    [Serializable]
    internal class RedeemedAmountGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int maximum;

        internal RedeemedAmount CandyGuardParam => new() { Maximum = (ulong)maximum };
    }

    [Serializable]
    internal class SolPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;

        internal SolPayment CandyGuardParam => new() {
            Destination = new(destination),
            Lamports = SolHelper.ConvertToLamports(value)
        };

        internal SolPaymentMintSettings GetMintSettings()
        {
            return enabled ? new() { 
                Destination = new(destination) 
            } : null;
        }
    }

    [Serializable]
    internal class StartDateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string date;

        internal StartDate CandyGuardParam => new() {
            Date = DateTimeOffset.Parse(date).ToUnixTimeSeconds()
        };
    }

    [Serializable]
    internal class ThirdPartySignerGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string signerKey;

        internal ThirdPartySigner CandyGuardParam => new() {
            SignerKey = new(signerKey)
        };

        internal ThirdPartySignerMintSettings GetMintSettings()
        {
            return enabled ? new() { Signer = new(signerKey) } : null;
        }
    }

    [Serializable]
    internal class TokenBurnGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;

        internal TokenBurn CandyGuardParam => new() {
            Amount = (ulong)amount,
            Mint = new(mint)
        };

        internal TokenBurnMintSettings GetMintSettings()
        {
            return enabled ? new() { Mint = new(mint) } : null;
        }
    }

    [Serializable]
    internal class TokenGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;

        internal TokenGate CandyGuardParam => new() {
            Amount = (ulong)amount,
            Mint = new(mint)
        };

        internal TokenGateMintSettings GetMintSettings()
        {
            return enabled ? new() { Mint = new(mint) } : null;
        }
    }

    [Serializable]
    internal class TokenPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destinationAta;

        internal TokenPayment CandyGuardParam => new() {
            DestinationAta = new(destinationAta),
            Amount = (ulong)amount,
            Mint = new(mint)
        };

        internal TokenPaymentMintSettings GetMintSettings()
        {
            return enabled ? new() {
                Mint = new (mint),
                DestinationAta = new (destinationAta)
            } : null;
        }
    }
}
