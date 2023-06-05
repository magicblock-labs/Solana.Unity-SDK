using Newtonsoft.Json;
using System;
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
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int limit;
    }

    [Serializable]
    internal class AddressGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string address;
    }

    [Serializable]
    internal class AllowListGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string merkleRoot;
    }

    [Serializable]
    internal class BotTaxGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private bool lastInstruction;
    }

    [Serializable]
    internal class EndDateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string date;
    }

    [Serializable]
    internal class GateKeeperGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string gatekeeperNetwork;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private bool expireOnUse;
    }

    [Serializable]
    internal class NFTBurnGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;
    }

    [Serializable]
    internal class NFTGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;
    }

    [Serializable]
    internal class NFTPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string requiredCollection;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;
    }

    [Serializable]
    internal class RedeemedAmountGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int maximum;
    }

    [Serializable]
    internal class SolPaymentGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int value;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string destination;
    }

    [Serializable]
    internal class StartDateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string date;
    }

    [Serializable]
    internal class ThirdPartySignerGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string signerKey;
    }

    [Serializable]
    internal class TokenBurnGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;
    }

    [Serializable]
    internal class TokenGateGuard : CandyMachineGuard
    {
        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private int amount;

        [ShowWhen("enabled"), SerializeField, JsonProperty]
        private string mint;
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
    }
}
