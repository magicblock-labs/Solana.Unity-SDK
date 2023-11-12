using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private Button signBtn;
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
        private bool _isLoadingTokens = false;

        public void Start()
        {
            refreshBtn.onClick.AddListener(RefreshWallet);

            sendBtn.onClick.AddListener(() =>
            {
                TransitionToTransfer();
            });
            
            signBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "sign_screen");
            });

            receiveBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "receive_screen");
            });
            
            swapBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "swap_screen_ag");
            });

            logoutBtn.onClick.AddListener(() =>
            {
                Web3.Instance.Logout();
                manager.ShowScreen(this, "login_screen");
                if(parentManager != null)
                    parentManager.ShowScreen(this, "[Connect_Wallet_Screen]");
            });
            
            savePrivateKeyBtn.onClick.AddListener(SavePrivateKeyOnClick);
            saveMnemonicsBtn.onClick.AddListener(SaveMnemonicsOnClick);

            _stopTask = new CancellationTokenSource();
            
            Web3.OnWalletChangeState += OnWalletChangeState;
        }

        private void OnWalletChangeState()
        {
            if(Web3.Wallet == null) return;
            swapBtn.transition = Web3.Wallet.RpcCluster == RpcCluster.MainNet ?
                Selectable.Transition.Animation
                : Selectable.Transition.ColorTint;
            swapBtn.interactable = Web3.Wallet.RpcCluster == RpcCluster.MainNet;

        }

        private void RefreshWallet()
        {
            Web3.UpdateBalance().Forget();
            GetOwnedTokenAccounts().AsAsyncUnitUniTask().Forget();
        }

        private void OnEnable()
        {
            Loading.StopLoading();
            var hasPrivateKey = !string.IsNullOrEmpty(Web3.Instance.WalletBase?.Account.PrivateKey);
            savePrivateKeyBtn.gameObject.SetActive(hasPrivateKey);
            var hasMnemonics = !string.IsNullOrEmpty(Web3.Instance.WalletBase?.Mnemonic?.ToString());
            saveMnemonicsBtn.gameObject.SetActive(hasMnemonics);
            Web3.OnBalanceChange += OnBalanceChange;
        }

        private void OnBalanceChange(double sol)
        {
            MainThreadDispatcher.Instance().Enqueue(() =>
            {
                lamports.text = $"{sol}";
            });
            GetOwnedTokenAccounts().AsAsyncUnitUniTask().Forget();
        }

        private void OnDisable()
        {
            Web3.OnBalanceChange -= OnBalanceChange;
        }

        private void SavePrivateKeyOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(Web3.Instance.WalletBase.Account.PrivateKey?.ToString())) return;
            Clipboard.Copy(Web3.Instance.WalletBase.Account.PrivateKey.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Private Key copied to clipboard", 3);
        }
        
        private void SaveMnemonicsOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(Web3.Instance.WalletBase.Mnemonic?.ToString())) return;
            Clipboard.Copy(Web3.Instance.WalletBase.Mnemonic.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics copied to clipboard", 3);
        }

        private void TransitionToTransfer(object data = null)
        {
            manager.ShowScreen(this, "transfer_screen", data);
        }

        private async UniTask GetOwnedTokenAccounts()
        {
            if(_isLoadingTokens) return;
            _isLoadingTokens = true;
            var tokens = await Web3.Wallet.GetTokenAccounts(Commitment.Processed);
            if(tokens == null) return;
            // Remove tokens not owned anymore and update amounts
            var tkToRemove = new List<TokenItem>();
            _instantiatedTokens.ForEach(tk =>
            {
                var tokenInfo = tk.TokenAccount.Account.Data.Parsed.Info;
                var match = tokens.Where(t => t.Account.Data.Parsed.Info.Mint == tokenInfo.Mint).ToArray();
                if (match.Length == 0 || match.Any(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 0))
                {
                    tkToRemove.Add(tk);
                }
                else
                {
                    var newAmount = match[0].Account.Data.Parsed.Info.TokenAmount.UiAmountString;
                    tk.UpdateAmount(newAmount);
                }
            });

            tkToRemove.ForEach(tk =>
            {
                _instantiatedTokens.Remove(tk);
                MainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Destroy(tk.gameObject);
                });
            });
            // Add new tokens
            List<UniTask> loadingTasks = new List<UniTask>();
            if (tokens is {Length: > 0})
            {
                var tokenAccounts = tokens.OrderByDescending(
                    tk => tk.Account.Data.Parsed.Info.TokenAmount.AmountUlong);
                foreach (var item in tokenAccounts)
                {
                    if (!(item.Account.Data.Parsed.Info.TokenAmount.AmountUlong > 0)) break;
                    if (_instantiatedTokens.All(t => t.TokenAccount.Account.Data.Parsed.Info.Mint != item.Account.Data.Parsed.Info.Mint))
                    {
                        // Run in the main thread
                        await MainThreadDispatcher.Instance().EnqueueAsync(() =>
                        {
                            var tk = Instantiate(tokenItem, tokenContainer, true);
                            tk.transform.localScale = Vector3.one;
                            var loadTask = Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint,
                                Web3.Instance.WalletBase.ActiveRpcClient, commitment: Commitment.Processed).AsUniTask();
                            loadingTasks.Add(loadTask);
                            loadTask.ContinueWith(nft =>
                            {
                                TokenItem tkInstance = tk.GetComponent<TokenItem>();
                                _instantiatedTokens.Add(tkInstance);
                                tk.SetActive(true);
                                if (tkInstance)
                                {
                                    tkInstance.InitializeData(item, this, nft).Forget();
                                }
                            }).Forget();
                        });
                    }
                }
            }
            await UniTask.WhenAll(loadingTasks);
            _isLoadingTokens = false;
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
