using UnityEngine;

// ReSharper disable once CheckNamespace

public static class LocalAssociationIntentCreator
{
    
    public static AndroidJavaObject CreateAssociationIntent(string associationToken, int port)
    {
        var intent = new AndroidJavaObject("android.content.Intent");
        intent.Call<AndroidJavaObject>("setAction", "android.intent.action.VIEW");
        intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.BROWSABLE");
        var url = $"{AssociationContract.SchemeMobileWalletAdapter}:/" +
                  $"{AssociationContract.LocalPathSuffix}?association={associationToken}&port={port}";
        var uriClass = new AndroidJavaClass("android.net.Uri");
        var uriData = uriClass.CallStatic<AndroidJavaObject>("parse", url);     
        intent.Call<AndroidJavaObject>("setData", uriData);
        //intent.Call<AndroidJavaObject>("addFlags", 0x14000000);
        return intent;
    }
}
