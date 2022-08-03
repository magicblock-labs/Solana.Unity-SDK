using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Solana.Unity.Wallet.Bip39;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solana.Unity.SDK.Example
{
    [RequireComponent(typeof(TxtLoader))]
    public class GenerateAccountScreen : SimpleScreen
    {
        public TextMeshProUGUI mnemonic_txt;
        public Button generate_btn;
        public Button restore_btn;
        public Button save_mnemonics_btn;
        public Button back_btn;
        public TMP_InputField password_input_field;
        public TextMeshProUGUI need_password_txt;

        private TxtLoader _txtLoader;
        private string _mnemonicsFileTitle = "Mnemonics";
        private string _privateKeyFileTitle = "PrivateKey";

        void Start()
        {
            _txtLoader = GetComponent<TxtLoader>();
            mnemonic_txt.text = new Mnemonic(WordList.English, WordCount.Twelve).ToString();

            if(generate_btn != null)
            {
                generate_btn.onClick.AddListener(() =>
                {
                    MainThreadDispatcher.Instance().Enqueue(GenerateNewAccount);
                });
            }

            if(restore_btn != null)
            {
                restore_btn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "re-generate_screen");
                });
            }

            if(save_mnemonics_btn != null)
            {
                save_mnemonics_btn.onClick.AddListener(() =>
                {
                    _txtLoader.SaveTxt(_mnemonicsFileTitle, mnemonic_txt.text, false);
                });
            }

            if(back_btn != null)
            {
                back_btn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "login_screen");
                });
            }

            _txtLoader.TxtSavedAction += SaveMnemonicsToTxtFile;
        }

        private void OnEnable()
        {
            need_password_txt.gameObject.SetActive(false);
            mnemonic_txt.text = new Mnemonic(WordList.English, WordCount.Twelve).ToString();
        }

        private async void GenerateNewAccount()
        {
            if (string.IsNullOrEmpty(password_input_field.text))
            {
                need_password_txt.gameObject.SetActive(true);
                need_password_txt.text = "Need Password!";
                return;
            }
            
            var password = password_input_field.text;
            var mnemonic = mnemonic_txt.text.Trim();
            try
            {
                await SimpleWallet.Instance.CreateAccount(mnemonic, password);
                manager.ShowScreen(this, "wallet_screen");
                need_password_txt.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                password_input_field.gameObject.SetActive(true);
                password_input_field.text = ex.ToString();
            }
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

        private void SaveMnemonicsToTxtFile(string path, string mnemonics, string fileTitle)
        {
            if (!this.gameObject.activeSelf) return;
            if (fileTitle != _mnemonicsFileTitle) return;

            if (SimpleWallet.Instance.StorageMethodReference == StorageMethod.JSON)
            {
                List<string> mnemonicsList = new List<string>();

                string[] splittedStringArray = mnemonics.Split(' ');
                foreach (string stringInArray in splittedStringArray)
                {
                    mnemonicsList.Add(stringInArray);
                }
                MnemonicsModel mnemonicsModel = new MnemonicsModel
                {
                    Mnemonics = mnemonicsList
                };

                if (path != string.Empty)
                    File.WriteAllText(path, JsonConvert.SerializeObject(mnemonicsModel));
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mnemonicsModel));
                    DownloadFile(gameObject.name, "OnFileDownload", _mnemonicsFileTitle + ".txt", bytes, bytes.Length);
                }
            }
            else if (SimpleWallet.Instance.StorageMethodReference == StorageMethod.SimpleTxt)
            {
                if (path != string.Empty)
                    File.WriteAllText(path, mnemonics);
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(mnemonics);
                    DownloadFile(gameObject.name, "OnFileDownload", _mnemonicsFileTitle + ".txt", bytes, bytes.Length);
                }
            }
        }
        //
        // WebGL
        //
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);
#endif
        // Called from browser
        private void OnFileDownload()
        {

        }
    } 
}
