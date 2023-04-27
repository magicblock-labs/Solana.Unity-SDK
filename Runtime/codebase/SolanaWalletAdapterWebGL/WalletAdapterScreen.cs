using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
   
    public class WalletAdapterScreen: MonoBehaviour
    {
        public GameObject buttonPrefab;
        public RectTransform viewPortContent;
        public Action<string> OnSelectedAction;
        private HashSet<string> _addedWallets = new();
        

        private void OnEnable()
        {
            UpdateWalletAdapterButtons();
        }

        private void _createWalletAdapterButton(SolanaWalletAdapterWebGL.WalletSpecs wallet)
        {
            var g = Instantiate(buttonPrefab, viewPortContent);
            var walletButton = g.GetComponent<WalletAdapterButton>();
            walletButton.WalletNameLabel.text = wallet.name;
            walletButton.Name = wallet.name;
            walletButton.DetectedLabel.GetComponent<TextMeshProUGUI>().enabled = wallet.installed;
            walletButton.OnSelectedAction = walletName =>
            { 
                OnSelectedAction?.Invoke(walletName);
            };
            Texture2D tex = new Texture2D(2, 2);
            var imgBytesArray = Convert.FromBase64String(wallet.icon);
            tex.LoadImage(imgBytesArray);
            Sprite iconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            walletButton.Icon.GetComponent<Image>().sprite = iconSprite;
        }
        private void UpdateWalletAdapterButtons()
         {
             foreach (var wallet in SolanaWalletAdapterWebGL.Wallets)
             {
                 if (_addedWallets.Contains(wallet.name))
                 {
                     continue;  
                 }
                 _addedWallets.Add(wallet.name);
                 _createWalletAdapterButton(wallet);
             }
         }
         
         public void OnClose()
         {
             transform.parent.gameObject.SetActive(false);
         }

    }
}