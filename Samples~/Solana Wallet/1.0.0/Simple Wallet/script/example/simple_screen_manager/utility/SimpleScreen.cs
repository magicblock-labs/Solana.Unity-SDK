using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Solana.Unity.SDK.Example
{
    public class SimpleScreen : MonoBehaviour, ISimpleScreen
    {
        public SimpleScreenManager manager { get; set; }

        public virtual void HideScreen()
        {
            gameObject.SetActive(false);
        }

        public virtual void ShowScreen(object data = null)
        {
            gameObject.SetActive(true);
        }
    }
}
