using Newtonsoft.Json;
using Solana.Unity.Metaplex.CandyGuard;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.SDK.Metaplex;
using System;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [Serializable]
    internal class CandyMachineGuardGroup
    {

        #region Properties

        internal Group FormattedGroup => new() {
            Guards = guards.FormattedSet,
            Label = label
        };

        #endregion

        #region Fields

        [SerializeField, JsonProperty]
        internal string label;

        [SerializeField, JsonProperty]
        internal CandyMachineGuardSet guards;

        #endregion

        #region Internal

        internal CandyGuardMintSettings GetMintSettings(MetadataAccount[] tokenAccounts)
        {
            return guards.GetMintSettings(label, tokenAccounts);
        }

        #endregion
    }

    [Serializable]
    internal class CandyMachineGuardSet
    {

        #region Properties

        internal GuardSet FormattedSet => new() {
            MintLimit = mintLimit.CandyGuardParam,
            AddressGate = addressGate.CandyGuardParam,
            AllowList = allowList.CandyGuardParam,
            BotTax = botTax.CandyGuardParam,
            StartDate = startDate.CandyGuardParam,
            EndDate = endDate.CandyGuardParam,
            Gatekeeper = gatekeeper.CandyGuardParam,
            NftBurn = nftBurn.CandyGuardParam,
            NftGate = nftGate.CandyGuardParam,
            NftPayment = nftPayment.CandyGuardParam,
            RedeemedAmount = redeemedAmount.CandyGuardParam,
            SolPayment = solPayment.CandyGuardParam,
            ThirdPartySigner = thirdPartySigner.CandyGuardParam,
            TokenBurn = tokenBurn.CandyGuardParam,
            TokenGate = tokenGate.CandyGuardParam,
            TokenPayment = tokenPayment.CandyGuardParam,
            FreezeSolPayment = freezeSolPayment.CandyGuardParam,
            FreezeTokenPayment = freezeTokenPayment.CandyGuardParam
        };

        #endregion

        #region Fields

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

        [SerializeField, JsonProperty]
        private FreezeSolPaymentGuard freezeSolPayment;

        [SerializeField, JsonProperty]
        private FreezeTokenPaymentGuard freezeTokenPayment;

        #endregion

        #region Internal

        // TODO: Optimize with reflection.
        internal void SetGuardsEnabled()
        {
            if (mintLimit != null) {
                mintLimit.enabled = true;
            }
            if (addressGate != null) {
                addressGate.enabled = true;
            }
            if (allowList != null) {
                allowList.enabled = true;
            }
            if (botTax != null) {
                botTax.enabled = true;
            }
            if (endDate != null) {
                endDate.enabled = true;
            }
            if (gatekeeper != null) {
                gatekeeper.enabled = true;
            }
            if (nftBurn != null) {
                nftBurn.enabled = true;
            }
            if (nftGate != null) {
                nftGate.enabled = true;
            }
            if (nftPayment != null) {
                nftPayment.enabled = true;
            }
            if (redeemedAmount != null) {
                redeemedAmount.enabled = true;
            }
            if (solPayment != null) {
                solPayment.enabled = true;
            }
            if (startDate != null) {
                startDate.enabled = true;
            }
            if (thirdPartySigner != null) {
                thirdPartySigner.enabled = true;
            }
            if (tokenBurn != null) {
                tokenBurn.enabled = true;
            }
            if (tokenGate != null) {
                tokenGate.enabled = true;
            }
            if (tokenPayment != null) {
                tokenPayment.enabled = true;
            }
        }

        internal CandyGuardMintSettings GetMintSettings(
            string label,
            MetadataAccount[] tokenAccounts
        )
        {
            return new CandyGuardMintSettings() {
                GuardGroup = label,
                ThirdPartySigner = thirdPartySigner.GetMintSettings(),
                MintLimit = mintLimit.GetMintSettings(),
                Gatekeeper = gatekeeper.GetMintSettings(),
                AllowList = allowList.GetMintSettings(),
                SolPayment = solPayment.GetMintSettings(),
                NftPayment = nftPayment.GetMintSettings(tokenAccounts),
                NftGate = nftGate.GetMintSettings(tokenAccounts),
                NftBurn = nftBurn.GetMintSettings(tokenAccounts),
                TokenBurn = tokenBurn.GetMintSettings(),
                TokenGate = tokenGate.GetMintSettings(),
                TokenPayment = tokenPayment.GetMintSettings(),
                FreezeSolPayment = freezeSolPayment.GetMintSettings(),
                FreezeTokenPayment = freezeTokenPayment.GetMintSettings()
            };
        }

        #endregion

        #region ShouldSerialize

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
