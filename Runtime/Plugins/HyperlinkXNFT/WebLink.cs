using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebLink : MonoBehaviour
{
    public string linkUrl;

    [DllImport("__Internal")]
    private static extern void HyperlinkXNFT(string linkUrl);
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OpenLink(string customLink = "")
    {
        if(customLink.Length==0)
            Application.OpenURL(linkUrl);
        else
            Application.OpenURL(customLink);
    }

    public void OpenLinkXNFT(string customLink = "")
    {
        //xnft link has to start with https:// and not have "www" after it. Very important!
    #if UNITY_EDITOR
            OpenLink(customLink);
    #else
            if (customLink.Length == 0)
                HyperlinkXNFT(linkUrl);
            else
                HyperlinkXNFT(customLink);
    #endif
    }

    public void OpenDeepLink(string customLink = "")
    {
    #if UNITY_IOS || UNITY_ANROID
                string refUrl = UnityWebRequest.EscapeURL("SolPlay");
                string escapedUrl = UnityWebRequest.EscapeURL(url);
                string inWalletUrl = $"https://phantom.app/ul/browse/{url}?ref=solplay";
    #else
            string inWalletUrl = (customLink.Length==0) ? linkUrl : customLink;
    #endif
            Application.OpenURL(inWalletUrl);
    }


}
