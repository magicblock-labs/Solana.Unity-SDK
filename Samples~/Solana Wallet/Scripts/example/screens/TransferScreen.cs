using System;
using System.Globalization;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class TransferScreen : SimpleScreen
    {
        public TextMeshProUGUI ownedAmountTxt;
        public TextMeshProUGUI nftTitleTxt;
        public TextMeshProUGUI errorTxt;
        public TMP_InputField toPublicTxt;
        public TMP_InputField amountTxt;
        public Button transferBtn;
        public RawImage nftImage;
        public Button closeBtn;

        private TokenAccount _transferTokenAccount;
        private Nft.Nft _nft;
        private double _ownedSolAmount;
        private ulong _ownedTokenAmount;
        
        private const long SolLamports = 1000000000;

        private void Start()
        {
            transferBtn.onClick.AddListener(TryTransfer);

            closeBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "wallet_screen");
            });
        }

        private async void TryTransfer()
        {
            var recipientAddress = await ResolveRecipientAddress();
            if (string.IsNullOrEmpty(recipientAddress)) return;
            if (!CheckRecipientAddress(recipientAddress)) return;

            if (_nft != null)
            {
                if (!CheckNftInput()) return;
                TransferNft(recipientAddress);
            }
            else if (_transferTokenAccount == null)
            {
                if (CheckInput(out var transferAmount))
                    TransferSol(recipientAddress, transferAmount);
            }
            else
            {
                if (CheckInput(out var transferAmount))
                    TransferToken(recipientAddress, transferAmount);
            }
        }

        private async void TransferSol(string recipientAddress, ulong lamports)
        {
            RequestResult<string> result = await Web3.Instance.WalletBase.Transfer(
                new PublicKey(recipientAddress),
                lamports);
            HandleResponse(result);
        }

        private async void TransferNft(string recipientAddress)
        {
            RequestResult<string> result = await Web3.Instance.WalletBase.Transfer(
                new PublicKey(recipientAddress),
                new PublicKey(_nft.metaplexData.data.mint),
                1);
            HandleResponse(result);
        }

        private bool CheckRecipientAddress(string recipientAddress)
        {
            if (string.IsNullOrEmpty(toPublicTxt.text))
            {
                errorTxt.text = "Please enter receiver public key or .skr domain";
                return false;
            }

            try
            {
                _ = new PublicKey(recipientAddress);
            }
            catch (Exception)
            {
                errorTxt.text = "Receiver must be a valid public key or .skr domain";
                return false;
            }

            errorTxt.text = "";
            return true;
        }

        private bool CheckNftInput()
        {
            if (_nft == null)
            {
                errorTxt.text = "Invalid NFT selection";
                return false;
            }

            if (string.IsNullOrEmpty(amountTxt.text))
            {
                errorTxt.text = "Please input transfer amount";
                return false;
            }

            if (ulong.TryParse(amountTxt.text, out var amount) && amount == 1)
            {
                errorTxt.text = "";
                return true;
            }

            errorTxt.text = "NFT transfer amount must be 1";
            return false;
        }

        bool CheckInput(out ulong transferAmount)
        {
            transferAmount = 0;

            if (string.IsNullOrEmpty(amountTxt.text))
            {
                errorTxt.text = "Please input transfer amount";
                return false;
            }

            var amountText = amountTxt.text.Trim();
            if (_transferTokenAccount == null)
            {
                if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountSol))
                {
                    errorTxt.text = "Please input a valid amount";
                    return false;
                }

                if (amountSol <= 0)
                {
                    errorTxt.text = "Transfer amount must be greater than zero";
                    return false;
                }

                if (amountSol > (decimal)_ownedSolAmount)
                {
                    errorTxt.text = "Not enough funds for transaction.";
                    return false;
                }

                var lamportsDecimal = amountSol * SolLamports;
                if (lamportsDecimal < 1m)
                {
                    errorTxt.text = "Transfer amount is too small";
                    return false;
                }

                if (lamportsDecimal > ulong.MaxValue)
                {
                    errorTxt.text = "Transfer amount is too large";
                    return false;
                }

                transferAmount = decimal.ToUInt64(decimal.Truncate(lamportsDecimal));
            }
            else
            {
                if (!ulong.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var amountToken))
                {
                    errorTxt.text = "Please input a valid whole number amount";
                    return false;
                }

                if (amountToken == 0)
                {
                    errorTxt.text = "Transfer amount must be greater than zero";
                    return false;
                }

                if (amountToken > _ownedTokenAmount)
                {
                    errorTxt.text = "Not enough funds for transaction.";
                    return false;
                }

                transferAmount = amountToken;
            }
            errorTxt.text = "";
            return true;
        }

        private async void TransferToken(string recipientAddress, ulong amount)
        {
            RequestResult<string> result = await Web3.Instance.WalletBase.Transfer(
                new PublicKey(recipientAddress),
                new PublicKey(_transferTokenAccount.Account.Data.Parsed.Info.Mint),
                amount);
            HandleResponse(result);
        }

        private async System.Threading.Tasks.Task<string> ResolveRecipientAddress()
        {
            var destination = toPublicTxt.text?.Trim();
            if (string.IsNullOrEmpty(destination))
            {
                errorTxt.text = "Please enter receiver public key or .skr domain";
                return null;
            }

            if (!destination.EndsWith(".skr", StringComparison.OrdinalIgnoreCase))
                return destination;

            var resolvedAddress = await SkrAddressResolutionClient.ResolveDomainToAddress(destination);
            if (string.IsNullOrEmpty(resolvedAddress))
            {
                errorTxt.text = $"Unable to resolve {destination}";
                return null;
            }

            toPublicTxt.text = resolvedAddress;
            return resolvedAddress;
        }

        private void HandleResponse(RequestResult<string> result)
        {
            errorTxt.text = result.Result == null ? result.Reason : "";
            if (result.Result != null)
            {
                manager.ShowScreen(this, "wallet_screen");
            }
        }

        public override async void ShowScreen(object data = null)
        {
            base.ShowScreen();

            ResetInputFields();
            await PopulateInfoFields(data);
            gameObject.SetActive(true);
        }
        
        public void OnClose()
        {
            var wallet = GameObject.Find("wallet");
            wallet.SetActive(false);
        }

        private async System.Threading.Tasks.Task PopulateInfoFields(object data)
        {
            nftImage.gameObject.SetActive(false);
            nftTitleTxt.gameObject.SetActive(false);
            ownedAmountTxt.gameObject.SetActive(false);
            if (data != null && data.GetType() == typeof(Tuple<TokenAccount, string, Texture2D>))
            {
                var (tokenAccount, tokenDef, texture) = (Tuple<TokenAccount, string, Texture2D>)data;
                ownedAmountTxt.text = $"{tokenAccount.Account.Data.Parsed.Info.TokenAmount.Amount}";
                if (!ulong.TryParse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.Amount, NumberStyles.Integer, CultureInfo.InvariantCulture, out _ownedTokenAmount))
                    _ownedTokenAmount = 0;
                nftTitleTxt.gameObject.SetActive(true);
                nftImage.gameObject.SetActive(true);
                nftTitleTxt.text = $"{tokenDef}";
                nftImage.texture = texture;
                nftImage.color = Color.white;
            }
            else if (data != null && data.GetType() == typeof(Nft.Nft))
            {
                nftTitleTxt.gameObject.SetActive(true);
                nftImage.gameObject.SetActive(true);
                _nft = (Nft.Nft)data;
                nftTitleTxt.text = $"{_nft.metaplexData.data.offchainData?.name}";
                nftImage.texture = _nft.metaplexData?.nftImage?.file;
                nftImage.color = Color.white;
                amountTxt.text = "1";
                amountTxt.interactable = false;
            }
            else
            {
                _ownedSolAmount = await Web3.Instance.WalletBase.GetBalance();
                ownedAmountTxt.text = $"{_ownedSolAmount}";
                _ownedTokenAmount = 0;
            }
        }

        private void ResetInputFields()
        {
            errorTxt.text = "";
            amountTxt.text = "";
            toPublicTxt.text = "";
            amountTxt.interactable = true;
            _ownedTokenAmount = 0;
        }

        public override void HideScreen()
        {
            base.HideScreen();
            _transferTokenAccount = null;
            gameObject.SetActive(false);
        }
    }

}