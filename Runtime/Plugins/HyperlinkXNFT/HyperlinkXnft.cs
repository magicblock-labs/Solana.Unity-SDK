using System.Runtime.InteropServices;
using UnityEngine;

// ReSharper disable once CheckNamespace

public class HyperlinkXnft : MonoBehaviour
{

    [DllImport("__Internal")]
    private static extern void HyperlinkXNFT(string linkUrl);

    public void OpenLink(string link)
    {
        //xnft link has to start with https:// and not have "www" after it. Very important!
    #if UNITY_EDITOR
        Application.OpenURL(link);
    #else
        Application.OpenURL(link);
        HyperlinkXNFT(link);
    #endif
    }


}
