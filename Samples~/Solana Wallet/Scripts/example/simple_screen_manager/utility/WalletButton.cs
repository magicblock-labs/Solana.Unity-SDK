using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solana.Unity.SDK.Example
{
public class WalletAdapterButton: MonoBehaviour
    {
        public TextMeshProUGUI WalletNameLabel;
        public GameObject DetectedLabel;
        public GameObject CanSign;

        public string Name { get; set; }

        public Action<string> OnSelectedAction;
        
        public void OnSelected()
        {
            OnSelectedAction?.Invoke(Name);
        }
    }

}