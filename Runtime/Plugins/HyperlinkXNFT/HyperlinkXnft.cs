using System.Runtime.InteropServices;
using UnityEngine;

// ReSharper disable once CheckNamespace

public class HyperlinkXnft : MonoBehaviour
{
   

    public void OpenLink(string link)
    {
        Application.OpenURL(link);
        #if UNITY_WEBGL && !UNITY_EDITOR
            //xnft link has to start with https:// and not have "www" after it. Very important!
            HyperlinkXNFT(link);
        #endif
    }
    
    #if UNITY_WEBGL
            [DllImport("__Internal")]
            private static extern void HyperlinkXNFT(string linkUrl);
    #else
        private static void HyperlinkXNFT(string linkUrl){}
    #endif

}
