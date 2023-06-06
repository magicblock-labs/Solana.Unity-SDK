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
        private string label;

        [SerializeField, JsonProperty]
        private CandyMachineGuardSet guards;

        #endregion

    }

    [Serializable]
    internal class CandyMachineGuardSet
    {
        #region Properties

        [SerializeField, JsonProperty]
        private MintLimitGuard mintLimit;

        [SerializeField, JsonProperty]
        private AddressGateGuard addressGate;

        [SerializeField, JsonProperty]
        private AllowListGuard allowList;

        [SerializeField, JsonProperty]
        private BotTaxGuard botTax;

        [SerializeField, JsonProperty]
        private StartDateGuard startDate;

        [SerializeField, JsonProperty]
        private EndDateGuard endDate;

        [SerializeField, JsonProperty]
        private GateKeeperGuard gatekeeper;

        [SerializeField, JsonProperty]
        private NFTBurnGuard nftBurn;

        [SerializeField, JsonProperty]
        private NFTGateGuard nftGate;

        [SerializeField, JsonProperty]
        private NFTPaymentGuard nftPayment;

        [SerializeField, JsonProperty]
        private RedeemedAmountGuard redeemedAmount;

        [SerializeField, JsonProperty]
        private SolPaymentGuard solPayment;

        [SerializeField, JsonProperty]
        private ThirdPartySignerGuard thirdPartySigner;

        [SerializeField, JsonProperty]
        private TokenBurnGuard tokenBurn;

        [SerializeField, JsonProperty]
        private TokenGateGuard tokenGate;

        [SerializeField, JsonProperty]
        private TokenPaymentGuard tokenPayment;

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
