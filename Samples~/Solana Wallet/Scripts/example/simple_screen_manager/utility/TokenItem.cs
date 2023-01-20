using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Solana.Unity.Extensions;
using Solana.Unity.Extensions.TokenMint;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class TokenItem : MonoBehaviour
    {
        public TextMeshProUGUI pub_txt;
        public TextMeshProUGUI ammount_txt;

        public RawImage logo;

        public Button transferButton;

        public TokenAccount TokenAccount;
        private Nft.Nft _nft;
        private SimpleScreen _parentScreen;
        private TokenMintResolver _tokenResolver;
        private Texture2D _texture;
        private TokenDef _tokenDef;

        private void Awake()
        {
            logo = GetComponentInChildren<RawImage>();
        }

        private void Start()
        {
            transferButton.onClick.AddListener(TransferAccount);
        }

        public void InitializeData(TokenAccount tokenAccount, SimpleScreen screen, Solana.Unity.SDK.Nft.Nft nftData = null)
        {
            _parentScreen = screen;
            TokenAccount = tokenAccount;
            if (nftData != null && int.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.Amount) == 1)
            {
                _nft = nftData;
                MainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ammount_txt.text = "";
                    pub_txt.text = nftData.metaplexData.data.name;
                });

                if (logo != null)
                {
                    MainThreadDispatcher.Instance().Enqueue(() => { logo.texture = nftData.metaplexData?.nftImage?.file; });
                }
            }
            else
            {
                MainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ammount_txt.text =
                        tokenAccount.Account.Data.Parsed.Info.TokenAmount.AmountDecimal.ToString(CultureInfo
                            .CurrentCulture);
                    pub_txt.text = nftData?.metaplexData?.data?.symbol ?? tokenAccount.Account.Data.Parsed.Info.Mint;
                });
                _tokenResolver ??= TokenMintResolver.Load();
                _tokenDef = _tokenResolver.Resolve(tokenAccount.Account.Data.Parsed.Info.Mint);
                var logoTask = LoadTokenLogo(_tokenDef);
            }
        }

        private async Task LoadTokenLogo(TokenDef tokenDef)
        {
            if(tokenDef is null || logo is null) return;
            var texture = await FileLoader.LoadFile<Texture2D>(tokenDef.TokenLogoUrl);
            _texture = FileLoader.Resize(texture, 75, 75);
            FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{tokenDef.TokenMint}.png"), _texture);
            MainThreadDispatcher.Instance().Enqueue(() => { logo.texture = _texture; });
        }

        public void TransferAccount()
        {
            if (_nft != null)
            {
                _parentScreen.manager.ShowScreen(_parentScreen, "transfer_screen", _nft);
            }
            else
            {
                _parentScreen.manager.ShowScreen(_parentScreen, "transfer_screen",  
                    Tuple.Create(TokenAccount, _tokenDef, _texture));
            }
        }

        public void UpdateAmount(string newAmount)
        {
            MainThreadDispatcher.Instance().Enqueue(() => { ammount_txt.text = newAmount; });
        }
    }
}
