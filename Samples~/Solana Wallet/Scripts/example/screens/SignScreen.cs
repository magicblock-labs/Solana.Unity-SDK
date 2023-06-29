using System;
using System.Text;
using codebase.utility;
using Solana.Unity.Wallet.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class SignScreen : SimpleScreen
    {
        public TextMeshProUGUI messageTxt;
        public TextMeshProUGUI signatureTxt;
        public Button signBtn;
        public Button closeBtn;

        private const long SolLamports = 1000000000;

        private void Start()
        {
            signBtn.onClick.AddListener(TrySign);

            closeBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "wallet_screen");
            });

            var btnSgn = signatureTxt.gameObject.AddComponent<Button>();
            btnSgn.onClick.AddListener(() => Clipboard.Copy(signatureTxt.text));
        }

        private async void TrySign()
        {
            var msgToSign = messageTxt.text.Trim().Replace("\u200B", "");
            var messageBytes = Encoding.UTF8.GetBytes(msgToSign);
            Debug.Log(msgToSign);
            var sign  = await Web3.Wallet.SignMessage(messageBytes);
            Debug.Log("Signature Valid: " + Web3.Wallet.Account.PublicKey.Verify(messageBytes, sign));
            signatureTxt.text = Encoders.Base58.EncodeData(sign);
        }
    }

}