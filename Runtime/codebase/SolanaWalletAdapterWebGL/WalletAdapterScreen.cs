using UnityEngine;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
   
    public class WalletAdapterScreen: MonoBehaviour
    {
        [SerializeField]
        private GameObject walletButtonPrefab;
        [SerializeField]
        private RectTransform walletListScrollTransform;
        [SerializeField]
        public Action<string> OnSelectedAction;
        [SerializeField]
        private HashSet<string> _addedWallets = new HashSet<string>();
        
         
        private void OnEnable()
        {
            UpdateWalletAdapterButtons();
        }
        
        
         
         private void UpdateWalletAdapterButtons()
         {
             Debug.Log("Adding Wallet Adapter Buttons");
             Debug.Log($"Len: {SolanaWalletAdapterWebGL.Wallets.Length}");
             foreach (var wallet in SolanaWalletAdapterWebGL.Wallets)
             {
                 if (_addedWallets.Contains(wallet.name))
                 {
                     continue;  
                 }
                 _addedWallets.Add(wallet.name);
                Debug.Log($"Wallet: {wallet.name}");
                var g = Instantiate(walletButtonPrefab, walletListScrollTransform);
                 var walletView = g.GetComponent<WalletAdapterButton>();
                 walletView.WalletNameLabel.text = wallet.name;
                 walletView.Name = wallet.name;
                 walletView.DetectedLabel.SetActive(wallet.installed);
                 
                 walletView.OnSelectedAction = walletName =>
                 {
                     Debug.Log($"Selected Wallet: {walletName} - {wallet.name}");
                     Debug.Log("Calling OnSelectedAction");
                     OnSelectedAction?.Invoke(walletName);
                     Debug.Log("Calling OnSelectedAction Done");
                 };
                 
             }
             
         }
         
         public void OnClose()
         {
             Debug.Log("Closing ");
             transform.parent.gameObject.SetActive(false);
         }
         
         
        
    }
}