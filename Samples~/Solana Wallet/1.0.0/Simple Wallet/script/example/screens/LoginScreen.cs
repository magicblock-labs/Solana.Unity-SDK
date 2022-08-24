using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System;
using Solana.Unity.Wallet;

namespace Solana.Unity.SDK.Example
{
    [RequireComponent(typeof(TxtLoader))]
    public class LoginScreen : SimpleScreen
    {
        public TMP_InputField _passwordInputField;
        public TextMeshProUGUI _passwordText;
        public Button _createNewWalletBtn;
        public Button _loginToWalletBtn;
        public Button _loginBtn;
        public Button _loginBtnGoogle;
        public Button _loginBtnTwitter;
        public Button _tryAgainBtn;
        public Button _backBtn;
        public TextMeshProUGUI _messageTxt;

        public List<GameObject> _panels = new();

        private void OnEnable()
        {
            _passwordInputField.text = string.Empty;
        }

        private void Start()
        {
            _passwordText.text = "";

            SwitchButtons("Login");

            if(_loginToWalletBtn != null)
            {
                _loginToWalletBtn.onClick.AddListener(() =>
                {
                    SwitchPanels(1);
                });
            }
 
            if(_backBtn != null)
            {
                _backBtn.onClick.AddListener(() =>
                {
                    SwitchPanels(0);
                });
            }

            if(_createNewWalletBtn != null)
            {
                _createNewWalletBtn.onClick.AddListener(() =>
                {
                    SimpleWallet.Instance.Wallet.Logout();
                    manager.ShowScreen(this, "generate_screen");
                    SwitchPanels(0);
                });
            }

            _passwordInputField.onSubmit.AddListener(delegate { LoginChecker(); });

            _loginBtn.onClick.AddListener(LoginChecker);
            _loginBtnGoogle.onClick.AddListener(delegate{LoginCheckerWeb3Auth(Provider.GOOGLE);});
            _loginBtnTwitter.onClick.AddListener(delegate{LoginCheckerWeb3Auth(Provider.TWITTER);});
            _tryAgainBtn.onClick.AddListener(() => { SwitchButtons("Login"); });  

            if(_messageTxt != null)
                _messageTxt.gameObject.SetActive(false);
            
            if (Application.platform != RuntimePlatform.Android && 
                Application.platform != RuntimePlatform.IPhonePlayer
                && Application.platform != RuntimePlatform.WindowsPlayer
                && Application.platform != RuntimePlatform.LinuxEditor
                && Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.OSXEditor)
            {
                _loginBtnGoogle.gameObject.SetActive(false);
                _loginBtnTwitter.gameObject.SetActive(false);
            }
        }

        private async void LoginChecker()
        {
            var password = _passwordInputField.text;
            var account = await SimpleWallet.Instance.LoginInGameWallet(password);
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
                manager.ShowScreen(this, "wallet_screen");
                gameObject.SetActive(false);
            }
            else
            {
                SwitchButtons("TryAgain");
            }
        }

        private void SwitchButtons(string btnName)
        {
            _loginBtn.gameObject.SetActive(false);
            _tryAgainBtn.gameObject.SetActive(false);

            switch (btnName)
            {
                case "Login":
                    _loginBtn.gameObject.SetActive(true);
                    _passwordInputField.gameObject.SetActive(true);
                    return;
                case "TryAgain":
                    _tryAgainBtn.gameObject.SetActive(true);
                    _passwordInputField.text = string.Empty;
                    _passwordInputField.gameObject.SetActive(false);
                    return;
            }
        }

        private void SwitchPanels(int order)
        {
            _passwordInputField.text = String.Empty;

            foreach (GameObject panel in _panels)
            {
                if (panel.transform.GetSiblingIndex() == order)
                    panel.SetActive(true);
                else
                    panel.SetActive(false);
            }
        }

        //
        // WebGL
        //
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

        // Called from browser
        public void OnFileUpload(string url)
        {
            StartCoroutine(OutputRoutine(url));
        }
        private IEnumerator OutputRoutine(string url)
        {
            var loader = new WWW(url);
            yield return loader;

            //LoginWithPrivateKeyCallback();
        }
    }
}

