using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using Cysharp.Threading.Tasks;
using Solana.Unity.Extensions;
using Solana.Unity.Rpc.Types;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class WalletScreen : SimpleScreen
    {
        [SerializeField]
        private TextMeshProUGUI lamports;
        [SerializeField]
        private Button refreshBtn;
        [SerializeField]
        private Button sendBtn;
        [SerializeField]
        private Button receiveBtn;
        [SerializeField]
        private Button swapBtn;
        [SerializeField]
        private Button logoutBtn;
        [SerializeField]
        private Button saveMnemonicsBtn;
        [SerializeField]
        private Button savePrivateKeyBtn;
        
        [SerializeField]
        private GameObject tokenItem;
        [SerializeField]
        private Transform tokenContainer;

        public SimpleScreenManager parentManager;

        private CancellationTokenSource _stopTask;
        private List<TokenItem> _instantiatedTokens = new();
        private static TokenMintResolver _tokenResolver;

        public void Start()
        {
            refreshBtn.onClick.AddListener(RefreshWallet);

            sendBtn.onClick.AddListener(() =>
            {
                TransitionToTransfer();
            });

            receiveBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "receive_screen");
            });
            
            swapBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "swap_screen");
            });

            logoutBtn.onClick.AddListener(() =>
            {
                WalletH.Instance.Logout();
                manager.ShowScreen(this, "login_screen");
                if(parentManager != null)
                    parentManager.ShowScreen(this, "[Connect_Wallet_Screen]");
            });
            
            savePrivateKeyBtn.onClick.AddListener(SavePrivateKeyOnClick);
            saveMnemonicsBtn.onClick.AddListener(SaveMnemonicsOnClick);

            _stopTask = new CancellationTokenSource();

            WalletH.WsRpc.SubscribeAccountInfo(
                WalletH.Instance.Wallet.Account.PublicKey,
                (_, accountInfo) =>
                {
                    Debug.Log("Account changed!, updated lamport: " + accountInfo.Value.Lamports);
                    RefreshWallet();
                },
                Commitment.Confirmed
            );
        }

        private void RefreshWallet()
        {
            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            GetOwnedTokenAccounts().AsAsyncUnitUniTask().Forget();
        }

        private void OnEnable()
        {
            var hasPrivateKey = !string.IsNullOrEmpty(WalletH.Instance.Wallet?.Account.PrivateKey);
            savePrivateKeyBtn.gameObject.SetActive(hasPrivateKey);
            var hasMnemonics = !string.IsNullOrEmpty(WalletH.Instance.Wallet?.Mnemonic?.ToString());
            saveMnemonicsBtn.gameObject.SetActive(hasMnemonics);
        }

        private void SavePrivateKeyOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(WalletH.Instance.Wallet.Account.PrivateKey?.ToString())) return;
            Clipboard.Copy(WalletH.Instance.Wallet.Account.PrivateKey.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Private Key copied to clipboard", 3);
        }
        
        private void SaveMnemonicsOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(WalletH.Instance.Wallet.Mnemonic?.ToString())) return;
            Clipboard.Copy(WalletH.Instance.Wallet.Mnemonic.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics copied to clipboard", 3);
        }

        private void TransitionToTransfer(object data = null)
        {
            manager.ShowScreen(this, "transfer_screen", data);
        }

        private async Task UpdateWalletBalanceDisplay()
        {
            if (WalletH.Instance.Wallet.Account is null) return;
            var sol = await WalletH.Base.GetBalance(Commitment.Confirmed);
            MainThreadDispatcher.Instance().Enqueue(() =>
            {
                lamports.text = $"{sol}";
            });
        }

        private async UniTask GetOwnedTokenAccounts()
        {
            var tokens = await WalletH.Base.GetTokenAccounts(Commitment.Confirmed);
            // Remove tokens not owned anymore and update amounts
            var tkToRemove = new List<TokenItem>();
            _instantiatedTokens.ForEach(tk =>
            {
                var tokenInfo = tk.TokenAccount.Account.Data.Parsed.Info;
                var match = tokens.Where(t => t.Account.Data.Parsed.Info.Mint == tokenInfo.Mint).ToArray();
                if (match.Length == 0 || match.Any(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 0))
                {
                    tkToRemove.Add(tk);
                    Destroy(tk.gameObject);
                    _instantiatedTokens.Remove(tk);
                }
                else
                {
                    var newAmount = match[0].Account.Data.Parsed.Info.TokenAmount.UiAmountString;
                    tk.UpdateAmount(newAmount);
                }
            });
            tkToRemove.ForEach(tk => _instantiatedTokens.Remove(tk));
            // Add new tokens
            if (tokens is {Length: > 0})
            {
                var tokenAccounts = tokens.OrderByDescending(
                    tk => tk.Account.Data.Parsed.Info.TokenAmount.AmountUlong);
                foreach (var item in tokenAccounts)
                {
                    if (!(item.Account.Data.Parsed.Info.TokenAmount.AmountUlong > 0)) break;
                    if (_instantiatedTokens.All(t => t.TokenAccount.Account.Data.Parsed.Info.Mint != item.Account.Data.Parsed.Info.Mint))
                    {
                        var tk = Instantiate(tokenItem, tokenContainer, true);
                        tk.transform.localScale = Vector3.one;

                        Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint,
                            WalletH.Instance.Wallet.ActiveRpcClient).AsUniTask().ContinueWith(nft =>
                        {
                            TokenItem tkInstance = tk.GetComponent<TokenItem>();
                            _instantiatedTokens.Add(tkInstance);
                            tk.SetActive(true);
                            if (tkInstance)
                            {
                                tkInstance.InitializeData(item, this, nft).Forget();
                            }
                        }).Forget();
                    }
                }
            }
        }
        
        public static async UniTask<TokenMintResolver> GetTokenMintResolver()
        {
            if(_tokenResolver != null) return _tokenResolver;
            var tokenResolver = await TokenMintResolver.LoadAsync();
            if(tokenResolver != null) _tokenResolver = tokenResolver;
            return _tokenResolver;
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();
            gameObject.SetActive(true);
            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            GetOwnedTokenAccounts().Forget();
        }

        public override void HideScreen()
        {
            base.HideScreen();
            gameObject.SetActive(false);
        }
        
        public void OnClose()
        {
            var wallet = GameObject.Find("wallet");
            wallet.SetActive(false);
        }


        private void OnDestroy()
        {
            if (_stopTask is null) return;
            _stopTask.Cancel();
        }

    }
}
