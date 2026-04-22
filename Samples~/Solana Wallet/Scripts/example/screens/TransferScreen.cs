using System;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK.Example.Services;
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

            if (_nft != null)
            {
                TransferNft(recipientAddress);
            }
            else if (_transferTokenAccount == null)
            {
                if (CheckInput(recipientAddress))
                    TransferSol(recipientAddress);
            }
            else
            {
                if (CheckInput(recipientAddress))
                    TransferToken(recipientAddress);
            }
        }

        private async void TransferSol(string recipientAddress)
        {
            RequestResult<string> result = await Web3.Instance.WalletBase.Transfer(
                new PublicKey(recipientAddress),
                Convert.ToUInt64(float.Parse(amountTxt.text)*SolLamports));
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

        bool CheckInput(string recipientAddress)
        {
            if (string.IsNullOrEmpty(amountTxt.text))
            {
                errorTxt.text = "Please input transfer amount";
                return false;
            }

            if (string.IsNullOrEmpty(toPublicTxt.text))
            {
                errorTxt.text = "Please enter receiver public key";
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

            if (_transferTokenAccount == null)
            {
                if (float.Parse(amountTxt.text) > _ownedSolAmount)
                {
                    errorTxt.text = "Not enough funds for transaction.";
                    return false;
                }
            }
            else
            {
                if (long.Parse(amountTxt.text) > long.Parse(ownedAmountTxt.text))
                {
                    errorTxt.text = "Not enough funds for transaction.";
                    return false;
                }
            }
            errorTxt.text = "";
            return true;
        }

        private async void TransferToken(string recipientAddress)
        {
            RequestResult<string> result = await Web3.Instance.WalletBase.Transfer(
                new PublicKey(recipientAddress),
                new PublicKey(_transferTokenAccount.Account.Data.Parsed.Info.Mint),
                ulong.Parse(amountTxt.text));
            HandleResponse(result);
        }

        private async System.Threading.Tasks.Task<string> ResolveRecipientAddress()
        {
            var destination = toPublicTxt.text?.Trim();
            if (string.IsNullOrEmpty(destination))
                return null;

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
            }
        }

        private void ResetInputFields()
        {
            errorTxt.text = "";
            amountTxt.text = "";
            toPublicTxt.text = "";
            amountTxt.interactable = true;
        }

        public override void HideScreen()
        {
            base.HideScreen();
            _transferTokenAccount = null;
            gameObject.SetActive(false);
        }
    }

}