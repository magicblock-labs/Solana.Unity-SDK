using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class ReGenerateAccountScreen : SimpleScreen
    {
        [SerializeField]
        private TMP_InputField mnemonicTxt;
        [SerializeField]
        private Button generateBtn;
        [SerializeField]
        private Button createBtn;
        [SerializeField]
        private Button backBtn;
        [SerializeField]
        private Button loadMnemonicsBtn;
        [SerializeField]
        private TMP_InputField passwordInputField;
        [SerializeField]
        private TextMeshProUGUI wrongPasswordTxt;
        [SerializeField]
        private TextMeshProUGUI errorTxt;

        private void OnEnable()
        {
            wrongPasswordTxt.gameObject.SetActive(false);
        }

        private void Start()
        {
            if(generateBtn != null)
            {
                generateBtn.onClick.AddListener(GenerateNewAccount);
            }

            if(createBtn != null)
            {
                createBtn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "generate_screen");
                });
            }

            if(backBtn != null)
            {
                backBtn.onClick.AddListener(() =>
                {
                    manager.ShowScreen(this, "generate_screen");
                });
            }

            loadMnemonicsBtn.onClick.AddListener(PasteMnemonicsClicked);
        }

        private async void GenerateNewAccount()
        {
            var password = passwordInputField.text;
            var mnemonic = mnemonicTxt.text;

            var account = await Web3.Instance.CreateAccount(mnemonic, password);
            if (account != null)
            {
                manager.ShowScreen(this, "wallet_screen");
            }
            else
            {
                errorTxt.text = "Keywords are not in a valid format.";
            }
        }
        
        private void PasteMnemonicsClicked()
        {
            mnemonicTxt.text = GUIUtility.systemCopyBuffer;
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();

            errorTxt.text = string.Empty;
            mnemonicTxt.text = string.Empty;
            passwordInputField.text = string.Empty;

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
