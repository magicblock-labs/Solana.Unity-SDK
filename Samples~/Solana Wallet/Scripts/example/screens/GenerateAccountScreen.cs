using System;
using Solana.Unity.Wallet.Bip39;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class GenerateAccountScreen : SimpleScreen
    {
        [SerializeField]
        public TextMeshProUGUI mnemonicTxt;
        [SerializeField]
        public Button generateBtn;
        [SerializeField]
        public Button restoreBtn;
        [SerializeField]
        public Button saveMnemonicsBtn;
        [SerializeField]
        public Button backBtn;
        [SerializeField]
        public TMP_InputField passwordInputField;
        [SerializeField]
        public TextMeshProUGUI needPasswordTxt;


        private void Start()
        {
            mnemonicTxt.text = new Mnemonic(WordList.English, WordCount.TwentyFour).ToString();

            if(generateBtn != null)
            {
                generateBtn.onClick.AddListener(() =>
                {
                    MainThreadDispatcher.Instance().Enqueue(GenerateNewAccount);
                });
            }

            if(restoreBtn != null)
            {
                restoreBtn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "re-generate_screen");
                });
            }

            if(saveMnemonicsBtn != null)
            {
                saveMnemonicsBtn.onClick.AddListener(CopyMnemonicsToClipboard);
            }

            if(backBtn != null)
            {
                backBtn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "login_screen");
                });
            }
        }

        private void OnEnable()
        {
            needPasswordTxt.gameObject.SetActive(false);
            mnemonicTxt.text = new Mnemonic(WordList.English, WordCount.TwentyFour).ToString();
        }

        private async void GenerateNewAccount()
        {
            if (string.IsNullOrEmpty(passwordInputField.text))
            {
                needPasswordTxt.gameObject.SetActive(true);
                needPasswordTxt.text = "Need Password!";
                return;
            }
            
            var password = passwordInputField.text;
            var mnemonic = mnemonicTxt.text.Trim();
            try
            {
                await Web3.Instance.CreateAccount(mnemonic, password);
                manager.ShowScreen(this, "wallet_screen");
                needPasswordTxt.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                passwordInputField.gameObject.SetActive(true);
                passwordInputField.text = ex.ToString();
            }
        }
        
        public void CopyMnemonicsToClipboard()
        {
            Clipboard.Copy(mnemonicTxt.text.Trim());
            gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics copied to clipboard", 3);
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();
            gameObject.SetActive(true);

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

    } 
}
