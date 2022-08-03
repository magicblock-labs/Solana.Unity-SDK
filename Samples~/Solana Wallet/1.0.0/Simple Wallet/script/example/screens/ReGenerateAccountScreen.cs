using Newtonsoft.Json;
using SFB;
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solana.Unity.SDK.Example
{
    [RequireComponent(typeof(TxtLoader))]
    public class ReGenerateAccountScreen : SimpleScreen
    {
        public TMP_InputField mnemonic_txt;
        public Button generate_btn;
        public Button create_btn;
        public Button back_btn;
        public Button load_mnemonics_btn;
        public TMP_InputField password_input_field;
        public TextMeshProUGUI wrong_password_txt;

        public TextMeshProUGUI error_txt;

        private string[] _paths;
        private string _path;
        private string _loadedMnemonics;

        private void OnEnable()
        {
            wrong_password_txt.gameObject.SetActive(false);
        }

        void Start()
        {
            if(generate_btn != null)
            {
                generate_btn.onClick.AddListener(() =>
                {
                    GenerateNewAccount();
                });
            }

            if(create_btn != null)
            {
                create_btn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "generate_screen");
                });
            }

            if(back_btn != null)
            {
                back_btn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "generate_screen");
                });
            }

            load_mnemonics_btn.onClick.AddListener(LoadMnemonicsFromTxtClicked);
        }

        public async void GenerateNewAccount()
        {
            string password = password_input_field.text;
            string mnemonic = mnemonic_txt.text;

            Account account = await SimpleWallet.Instance.CreateAccount(mnemonic, password);
            if (account != null)
            {
                manager.ShowScreen(this, "wallet_screen");
            }
            else
            {
                error_txt.text = "Keywords are not in a valid format.";
            }
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();

            error_txt.text = String.Empty;
            mnemonic_txt.text = String.Empty;
            password_input_field.text = String.Empty;

            gameObject.SetActive(true);
        }

        public override void HideScreen()
        {
            base.HideScreen();
            gameObject.SetActive(false);
        }

        private void LoadMnemonicsFromTxtClicked()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                 UploadFile(gameObject.name, "OnFileUpload", ".txt", false);
#elif UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_STANDALONE
                _paths = StandaloneFileBrowser.OpenFilePanel("Title", "", "txt", false);
                _path = _paths[0];
                _loadedMnemonics = File.ReadAllText(_path);
#elif UNITY_ANDROID || UNITY_IPHONE
                string txt;
                txt = NativeFilePicker.ConvertExtensionToFileType("txt");
                NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
		            {
			            if (path == null)
				            Debug.Log("Operation cancelled");
			            else
			            {
                            _loadedMnemonics = File.ReadAllText(path);
                        }
		            }, new string[] { txt });
		        Debug.Log("Permission result: " + permission);
#endif

#if UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IPHONE
                ResolveMnemonicsByType();
#endif
            }

            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

        private void ResolveMnemonicsByType()
        {
            if (!string.IsNullOrEmpty(_loadedMnemonics))
            {
                if (SimpleWallet.Instance.storageMethod == StorageMethod.JSON)
                {
                    try
                    {
                        JSONDeserialization();
                    }
                    catch
                    {
                        try
                        {
                            SimpleTxtDeserialization();
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
                else if (SimpleWallet.Instance.storageMethod == StorageMethod.SimpleTxt)
                {
                    try
                    {
                        SimpleTxtDeserialization();
                    }
                    catch
                    {
                        try
                        {
                            JSONDeserialization();
                        }
                        catch
                        {
       
                            return;
                        }
                    }
                }
            }

            void JSONDeserialization()
            {
                MnemonicsModel mnemonicsModel = JsonConvert.DeserializeObject<MnemonicsModel>(_loadedMnemonics);
                string deserializedMnemonics = string.Join(" ", mnemonicsModel.Mnemonics);
                mnemonic_txt.text = deserializedMnemonics;
            }

            void SimpleTxtDeserialization()
            {
                mnemonic_txt.text = _loadedMnemonics;
            }
        }

        //
        // WebGL
        //
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif
        // Called from browser
        public void OnFileUpload(string url)
        {
            StartCoroutine(OutputRoutine(url));
        }
        private IEnumerator OutputRoutine(string url)
        {
            var loader = new WWW(url);
            yield return loader;

            MainThreadDispatcher.Instance().Enqueue(() => { _loadedMnemonics = loader.text; });
            MainThreadDispatcher.Instance().Enqueue(() => { ResolveMnemonicsByType(); });           
        }
    }
}
