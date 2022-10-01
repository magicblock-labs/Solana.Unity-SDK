using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Utils
{
    //https://qiita.com/lucifuges/items/b17d602417a9a249689f
#if UNITY_IOS
    [DllImport("__Internal")]
    extern static void launchUrl(string url);
    [DllImport("__Internal")]
    extern static void dismiss();
#endif


    public static void LaunchUrl(string url)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        Application.OpenURL(url);
#elif UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intentBuilder = new AndroidJavaObject("androidx.browser.customtabs.CustomTabsIntent$Builder"))
        using (var intent = intentBuilder.Call<AndroidJavaObject>("build"))
        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
        using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", url))
        {
            intent.Call("launchUrl", activity, uri);
        }

#elif UNITY_IOS
    launchUrl(url);;
#endif
    }


    public static void Dismiss()
    {
#if UNITY_IOS && !UNITY_EDITOR
    dismiss();
#endif
    }


    public static byte[] DecodeBase64(string text)
    {
        var output = text;
        output = output.Replace('-', '+');
        output = output.Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new FormatException(text);
        }
        var converted = Convert.FromBase64String(output);
        return converted;
    }

    public static Dictionary<string, string> ParseQuery(string text)
    {
        if (text.Length > 0 && text[0] == '?')
            text = text.Remove(0, 1);

        var parts = text.Split('&').Where(x => !string.IsNullOrEmpty(x)).ToList();

        Dictionary<string, string> result = new Dictionary<string, string>();

        if (parts.Count > 0)
        {
            result = parts.ToDictionary(
                c => c.Split('=')[0],
                c => Uri.UnescapeDataString(c.Split('=')[1])
            );
        }

        return result;
    }

    public static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }


}