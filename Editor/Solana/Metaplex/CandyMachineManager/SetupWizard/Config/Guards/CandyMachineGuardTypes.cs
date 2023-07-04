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

        internal MintLimit CandyGuardParam => enabled ? new() { 
            Id = (byte)id, 
            Limit = (ushort)limit 
        } : null;

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

        internal AddressGate CandyGuardParam => enabled ? new() { 
            Address = new(address) 
        } : null;
    }

    [Serializable]
    internal class AllowListGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string merkleRoot;

        internal AllowList CandyGuardParam => enabled ? new() { 
            MerkleRoot = KeyStore.Utils.HexToByteArray(merkleRoot) 
        } : null;

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

        internal BotTax CandyGuardParam => enabled ? new() { 
            Lamports = (ulong)(value * SolHelper.LAMPORTS_PER_SOL), 
            LastInstruction = lastInstruction 
        } : null;
    }

    [Serializable]
    internal class EndDateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string date;

        internal EndDate CandyGuardParam => enabled ? new() { 
            Date = DateTimeOffset.Parse(date).ToUnixTimeSeconds() 
        } : null;
    }

    [Serializable]
    internal class GateKeeperGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string gatekeeperNetwork;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private bool expireOnUse;

        internal Gatekeeper CandyGuardParam => enabled ? new() {
            ExpireOnUse = expireOnUse,
            GatekeeperNetwork = new(gatekeeperNetwork)
        } : null;

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

        internal NftBurn CandyGuardParam => enabled ? new() {
            RequiredCollection = new(requiredCollection)
        } : null;

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

        internal NftGate CandyGuardParam => enabled ? new() {
            RequiredCollection = new(requiredCollection)
        } : null;

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

        internal NftPayment CandyGuardParam => enabled ? new() {
            Destination = new(destination),
            RequiredCollection = new(requiredCollection)
        } : null;

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

        internal RedeemedAmount CandyGuardParam => enabled ? new() { 
            Maximum = (ulong)maximum 
        } : null;
    }

    [Serializable]
    internal class SolPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;

        internal SolPayment CandyGuardParam => enabled ? new() {
            Destination = new(destination),
            Lamports = SolHelper.ConvertToLamports(value)
        } : null;

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

        internal StartDate CandyGuardParam => enabled ? new() {
            Date = DateTimeOffset.Parse(date).ToUnixTimeSeconds()
        } : null;
    }

    [Serializable]
    internal class ThirdPartySignerGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string signerKey;

        internal ThirdPartySigner CandyGuardParam => enabled ? new() {
            SignerKey = new(signerKey)
        } : null;

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

        internal TokenBurn CandyGuardParam => enabled ? new() {
            Amount = (ulong)amount,
            Mint = new(mint)
        } : null;

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

        internal TokenGate CandyGuardParam => enabled ? new() {
            Amount = (ulong)amount,
            Mint = new(mint)
        } : null;

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

        internal TokenPayment CandyGuardParam => enabled ? new() {
            DestinationAta = new(destinationAta),
            Amount = (ulong)amount,
            Mint = new(mint)
        } : null;

        internal TokenPaymentMintSettings GetMintSettings()
        {
            return enabled ? new() {
                Mint = new (mint),
                DestinationAta = new (destinationAta)
            } : null;
        }
    }

    [Serializable]
    internal class FreezeSolPaymentGuard: CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;

        internal FreezeSolPayment CandyGuardParam => enabled ? new() {
            Destination = new(destination),
            Lamports = SolHelper.ConvertToLamports(value)
        } : null;

        internal FreezeSolPaymentMintSettings GetMintSettings()
        {
            return enabled ? new() {
                Destination = new(destination)
            } : null;
        }
    }

    [Serializable]
    internal class FreezeTokenPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destinationAta;

        internal FreezeTokenPayment CandyGuardParam => enabled ? new() {
            DestinationAta = new(destinationAta),
            Amount = (ulong)amount,
            Mint = new(mint)
        } : null;

        internal FreezeTokenPaymentMintSettings GetMintSettings()
        {
            return enabled ? new() {
                Mint = new(mint),
                DestinationAta = new(destinationAta)
            } : null;
        }
    }
}
