using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class LoginScreen : SimpleScreen
    {
        [SerializeField]
        private TMP_InputField passwordInputField;
        [SerializeField]
        private TextMeshProUGUI passwordText;
        [SerializeField]
        private Button loginBtn;
        [SerializeField]
        private Button loginBtnGoogle;
        [SerializeField]
        private Button loginBtnTwitter;
        [SerializeField]
        private Button loginBtnPhantom;
        [SerializeField]
        private Button backBtn;
        [SerializeField]
        private TextMeshProUGUI messageTxt;
        [SerializeField]
        private TMP_Dropdown dropdownRpcCluster;

        private void OnEnable()
        {
            dropdownRpcCluster.interactable = true;
            passwordInputField.text = string.Empty;
        }

        private void Start()
        {
            passwordText.text = "";

            passwordInputField.onSubmit.AddListener(delegate { LoginChecker(); });

            loginBtn.onClick.AddListener(LoginChecker);
            loginBtnGoogle.onClick.AddListener(delegate{LoginCheckerWeb3Auth(Provider.GOOGLE);});
            loginBtnTwitter.onClick.AddListener(delegate{LoginCheckerWeb3Auth(Provider.TWITTER);});
            loginBtnPhantom.onClick.AddListener(LoginCheckerPhantom);

            if(messageTxt != null)
                messageTxt.gameObject.SetActive(false);
            
            if (Application.platform != RuntimePlatform.Android && 
                Application.platform != RuntimePlatform.IPhonePlayer
                && Application.platform != RuntimePlatform.WindowsPlayer
                && Application.platform != RuntimePlatform.LinuxEditor
                && Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.OSXEditor)
            {
                loginBtnGoogle.gameObject.SetActive(false);
                loginBtnTwitter.gameObject.SetActive(false);
            }
        }

        private async void LoginChecker()
        {
            var password = passwordInputField.text;
            var account = await SimpleWallet.Instance.LoginInGameWallet(password);
            CheckAccount(account);
        }
        
        private async void LoginCheckerPhantom()
        {
            var account = await SimpleWallet.Instance.LoginPhantom();
            CheckAccount(account);
        }
        
        private async void LoginCheckerWeb3Auth(Provider provider)
        {
            var account = await SimpleWallet.Instance.LoginInWeb3Auth(provider);
            CheckAccount(account);
        }


        private void CheckAccount(Account account)
        {
            if (account != null)
            {
                dropdownRpcCluster.interactable = false;
                manager.ShowScreen(this, "wallet_screen");
                messageTxt.gameObject.SetActive(false);
                gameObject.SetActive(false);
            }
            else
            {
                passwordInputField.text = string.Empty;
                messageTxt.gameObject.SetActive(true);
            }
        }

        public void OnClose()
        {
            var wallet = GameObject.Find("wallet");
            wallet.SetActive(false);
        }
    }
}

