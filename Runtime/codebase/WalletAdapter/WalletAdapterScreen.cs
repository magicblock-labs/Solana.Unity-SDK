using UnityEngine;
using System;

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

        
         private void Start()
        {
            Debug.Log("WalletAdapterScreen Start");
            AddWalletAdapterButtons();
            Debug.Log("WalletAdapterScreen Start Done");
        }
         
         private async void AddWalletAdapterButtons()
         {
             Debug.Log("Adding Wallet Adapter Buttons");
            
             
             Debug.Log($"Len: {WalletAdapter.Wallets.Length}");

             
             foreach (var wallet in WalletAdapter.Wallets)
             {
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
             gameObject.SetActive(false);
         }
         
         
        
    }
}