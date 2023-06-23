using Newtonsoft.Json;
using Solana.Unity.Metaplex.CandyGuard;
using Solana.Unity.Programs.Utilities;
using System;
using System.Text;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

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
    }

    [Serializable]
    internal class NFTBurnGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        internal NftBurn CandyGuardParam => new() {
            RequiredCollection = new(requiredCollection)
        };
    }

    [Serializable]
    internal class NFTGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        internal NftGate CandyGuardParam => new() {
            RequiredCollection = new(requiredCollection)
        };
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
    }
}
