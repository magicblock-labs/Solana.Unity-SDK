using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;

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
        private List<GameObject> _instantiatedTokens;

        public void Start()
        {
            _instantiatedTokens = new List<GameObject>();
            WebSocketActions.WebSocketAccountSubscriptionAction += (bool istrue) => 
            {
                MainThreadDispatcher.Instance().Enqueue(UpdateWalletBalanceDisplay);
            };
            WebSocketActions.CloseWebSocketConnectionAction += DisconnectToWebSocket;
            refreshBtn.onClick.AddListener( () =>
            {
                Task.Run(UpdateWalletBalanceDisplay);
                Task.Run(GetOwnedTokenAccounts);
            });

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
                SimpleWallet.Instance.Logout();
                manager.ShowScreen(this, "login_screen");
                if(parentManager != null)
                    parentManager.ShowScreen(this, "[Connect_Wallet_Screen]");
            });
            
            savePrivateKeyBtn.onClick.AddListener(SavePrivateKeyOnClick);
            saveMnemonicsBtn.onClick.AddListener(SaveMnemonicsOnClick);

            _stopTask = new CancellationTokenSource();
        }

        private void OnEnable()
        {
            var hasPrivateKey = !string.IsNullOrEmpty(SimpleWallet.Instance.Wallet.Account.PrivateKey);
            savePrivateKeyBtn.gameObject.SetActive(hasPrivateKey);
            var hasMnemonics = !string.IsNullOrEmpty(SimpleWallet.Instance.Wallet.Mnemonic?.ToString());
            saveMnemonicsBtn.gameObject.SetActive(hasMnemonics);
        }

        private void SavePrivateKeyOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(SimpleWallet.Instance.Wallet.Account.PrivateKey?.ToString())) return;
            Clipboard.Copy(SimpleWallet.Instance.Wallet.Account.PrivateKey.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Private Key copied to clipboard", 3);
        }
        
        private void SaveMnemonicsOnClick()
        {
            if (!gameObject.activeSelf) return;
            if (string.IsNullOrEmpty(SimpleWallet.Instance.Wallet.Mnemonic?.ToString())) return;
            Clipboard.Copy(SimpleWallet.Instance.Wallet.Mnemonic.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics copied to clipboard", 3);
        }

        private void TransitionToTransfer(object data = null)
        {
            manager.ShowScreen(this, "transfer_screen", data);
        }

        private async void UpdateWalletBalanceDisplay()
        {
            if (SimpleWallet.Instance.Wallet.Account is null) return;
            double sol = await SimpleWallet.Instance.Wallet.GetBalance();
            MainThreadDispatcher.Instance().Enqueue(() => { lamports.text = $"{sol}"; });
        }

        private void DisconnectToWebSocket()
        {
            MainThreadDispatcher.Instance().Enqueue(() => { manager.ShowScreen(this, "login_screen"); });
            MainThreadDispatcher.Instance().Enqueue(() => { SimpleWallet.Instance.Wallet.Logout(); });
        }

        private async void GetOwnedTokenAccounts()
        {
            var tokens = await SimpleWallet.Instance.Wallet.GetTokenAccounts();
            // Remove tokens not owned anymore and update amounts
            await MainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                _instantiatedTokens.ForEach(tk =>
                {
                    var tokenInfo = tk.GetComponent<TokenItem>().TokenAccount.Account.Data.Parsed.Info;
                    var match = tokens.Where(t => t.Account.Data.Parsed.Info.Mint == tokenInfo.Mint).ToArray();
                    if (match.Length == 0 || match.Any(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 0))
                    {
                        Destroy(tk);
                    }
                    else
                    {
                        var newAmount = match[0].Account.Data.Parsed.Info.TokenAmount.UiAmountString;
                        tk.GetComponent<TokenItem>().UpdateAmount(newAmount);
                    }
                });
            });
            // Add new tokens
            if (tokens is {Length: > 0})
            {
                var tokenAccounts = tokens.OrderByDescending(
                    tk => tk.Account.Data.Parsed.Info.TokenAmount.AmountUlong);
                foreach (var item in tokenAccounts)
                {
                    if (!(item.Account.Data.Parsed.Info.TokenAmount.AmountUlong > 0)) break;
                    MainThreadDispatcher.Instance().Enqueue(async () =>
                    {
                        if (_instantiatedTokens.All(t => t.GetComponent<TokenItem>().TokenAccount.Account.Data.Parsed.Info.Mint != item.Account.Data.Parsed.Info.Mint))
                        {
                            var nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint,
                                SimpleWallet.Instance.Wallet.ActiveRpcClient);
                            var tk = Instantiate(tokenItem, tokenContainer, true);
                            tk.SetActive(false);
                            tk.transform.localScale = Vector3.one;
                            _instantiatedTokens.Add(tk);
                            tk.GetComponent<TokenItem>().InitializeData(item, this, nft);
                            tk.SetActive(true);
                        }
                    });
                }
            }
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();
            gameObject.SetActive(true);
            Task.Run(UpdateWalletBalanceDisplay);
            Task.Run(GetOwnedTokenAccounts);
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
