using System;
using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
public class WalletAdapterButton: MonoBehaviour
    {
        public TextMeshProUGUI WalletNameLabel;
        public GameObject DetectedLabel;
        public GameObject Icon;
        public string Name { get; set; }

        public Action<string> OnSelectedAction;
        public void OnSelected()
        {
            OnSelectedAction?.Invoke(Name);
        }
    }
}