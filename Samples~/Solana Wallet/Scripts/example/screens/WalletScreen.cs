using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using System.Threading.Tasks;

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
            refreshBtn.onClick.AddListener(async () =>
            {
                UpdateWalletBalanceDisplay();
                await GetOwnedTokenAccounts();
            });

            sendBtn.onClick.AddListener(() =>
            {
                TransitionToTransfer();
            });

            receiveBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "receive_screen");
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

        private async Task GetOwnedTokenAccounts()
        {
            DisableTokenItems();
            var result = await SimpleWallet.Instance.Wallet.GetTokenAccounts();

            if (result is {Length: > 0})
            {
                foreach (var item in result)
                {
                    if (!(float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)) continue;
                    var nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SimpleWallet.Instance.Wallet.ActiveRpcClient);

                    var tk = Instantiate(tokenItem, tokenContainer, true);
                    tk.transform.localScale = Vector3.one;
                    _instantiatedTokens.Add(tk);
                    tk.SetActive(true);
                    tk.GetComponent<TokenItem>().InitializeData(item, this, nft);
                }
            }
        }
        
        private void DisableTokenItems()
        {
            if(_instantiatedTokens == null) return;
            foreach (GameObject token in _instantiatedTokens)
            {
                Destroy(token);
            }
            _instantiatedTokens.Clear();
        }
        
        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();
            gameObject.SetActive(true);
            UpdateWalletBalanceDisplay();
            #pragma warning disable CS4014
            GetOwnedTokenAccounts();
            #pragma warning restore CS4014
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
