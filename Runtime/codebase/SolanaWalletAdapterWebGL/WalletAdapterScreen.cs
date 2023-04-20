using UnityEngine;
using System;
using System.Collections.Generic;
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


        private void UpdateWalletAdapterButtons()
         {
             foreach (var wallet in SolanaWalletAdapterWebGL.Wallets)
             {
                 if (_addedWallets.Contains(wallet.name))
                 {
                     continue;  
                 }
                 _addedWallets.Add(wallet.name);
                var g = Instantiate(buttonPrefab, viewPortContent);
                 var walletView = g.GetComponent<WalletAdapterButton>();
                 walletView.WalletNameLabel.text = wallet.name;
                 walletView.Name = wallet.name;
                 walletView.DetectedLabel.SetActive(wallet.installed);
                 
                 walletView.OnSelectedAction = walletName =>
                 {
                     OnSelectedAction?.Invoke(walletName);
                 };
                 
             }
             
         }
         
         public void OnClose()
         {
             transform.parent.gameObject.SetActive(false);
         }
         
         
        
    }
}