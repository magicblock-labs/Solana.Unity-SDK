using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [Serializable]
    internal class CandyMachineGuardGroup
    {

        #region Properties

        [SerializeField, JsonProperty]
        internal string label;

        [SerializeField, JsonProperty]
        internal CandyMachineGuardSet guards;

        #endregion

    }

    [Serializable]
    internal class CandyMachineGuardSet
    {
        #region Properties

        [SerializeField, JsonProperty]
        internal MintLimitGuard mintLimit;

        [SerializeField, JsonProperty]
        internal AddressGateGuard addressGate;

        [SerializeField, JsonProperty]
        internal AllowListGuard allowList;

        [SerializeField, JsonProperty]
        internal BotTaxGuard botTax;

        [SerializeField, JsonProperty]
        internal StartDateGuard startDate;

        [SerializeField, JsonProperty]
        internal EndDateGuard endDate;

        [SerializeField, JsonProperty]
        internal GateKeeperGuard gatekeeper;

        [SerializeField, JsonProperty]
        internal NFTBurnGuard nftBurn;

        [SerializeField, JsonProperty]
        internal NFTGateGuard nftGate;

        [SerializeField, JsonProperty]
        internal NFTPaymentGuard nftPayment;

        [SerializeField, JsonProperty]
        internal RedeemedAmountGuard redeemedAmount;

        [SerializeField, JsonProperty]
        internal SolPaymentGuard solPayment;

        [SerializeField, JsonProperty]
        internal ThirdPartySignerGuard thirdPartySigner;

        [SerializeField, JsonProperty]
        internal TokenBurnGuard tokenBurn;

        [SerializeField, JsonProperty]
        internal TokenGateGuard tokenGate;

        [SerializeField, JsonProperty]
        internal TokenPaymentGuard tokenPayment;

        #endregion

        #region Public

        public bool ShouldSerializemintLimit()
        {
            return mintLimit.enabled;
        }

        public bool ShouldSerializeaddressGate()
        {
            return addressGate.enabled;
        }

        public bool ShouldSerializeallowList()
        {
            return allowList.enabled;
        }

        public bool ShouldSerializebotTax()
        {
            return botTax.enabled;
        }

        public bool ShouldSerializeendDate()
        {
            return endDate.enabled;
        }

        public bool ShouldSerializegatekeeper()
        {
            return gatekeeper.enabled;
        }

        public bool ShouldSerializenftBurn()
        {
            return nftBurn.enabled;
        }

        public bool ShouldSerializenftGate()
        {
            return nftGate.enabled;
        }

        public bool ShouldSerializenftPayment()
        {
            return nftPayment.enabled;
        }

        public bool ShouldSerializeredeemedAmount()
        {
            return redeemedAmount.enabled;
        }

        public bool ShouldSerializesolPayment()
        {
            return solPayment.enabled;
        }

        public bool ShouldSerializestartDate()
        {
            return startDate.enabled;
        }

        public bool ShouldSerializethirdPartySigner()
        {
            return thirdPartySigner.enabled;
        }

        public bool ShouldSerializetokenBurn()
        {
            return tokenBurn.enabled;
        }

        public bool ShouldSerializetokenGate()
        {
            return tokenGate.enabled;
        }

        public bool ShouldSerializetokenPayment()
        {
            return tokenPayment.enabled;
        }

        #endregion
    }
}
