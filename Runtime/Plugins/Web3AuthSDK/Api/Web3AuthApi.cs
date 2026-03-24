using System.Collections;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;
using UnityEngine;

public class Web3AuthApi
{
    static Web3AuthApi instance;
    static string baseAddress = "https://session.web3auth.io/v2";

    public static Web3AuthApi getInstance()
    {
        if (instance == null)
            instance = new Web3AuthApi();
        return instance;
    }

    public IEnumerator authorizeSession(string key, string origin, Action<StoreApiResponse> callback)
    {
        //var requestURL = $"{baseAddress}/store/get?key={key}";
        //var request = UnityWebRequest.Get(requestURL);
        WWWForm data = new WWWForm();
        data.AddField("key", key);

        var request = UnityWebRequest.Post($"{baseAddress}/store/get", data);
        // Only send Origin when it's a valid HTTP(S) URL. Custom schemes (e.g. torusapp://) can cause
        // the session API v2 to reject the request. The pre-v9 SDK did not send origin; omitting it
        // for custom schemes restores session persistence on mobile.
        // Normalize to scheme://host[:port] only (Origin header must not include path).
        string originToSend = null;
        if (!string.IsNullOrEmpty(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var originUri) &&
            (originUri.Scheme == Uri.UriSchemeHttp || originUri.Scheme == Uri.UriSchemeHttps))
        {
            originToSend = originUri.GetLeftPart(UriPartial.Authority);
        }
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(originToSend))
            originToSend = "http://localhost";
#endif
        if (!string.IsNullOrEmpty(originToSend))
            request.SetRequestHeader("origin", originToSend);

        yield return request.SendWebRequest();
        // Debug.Log("baseAddress =>" + baseAddress);
        // Debug.Log("key =>" + key);
        // //Debug.Log("request URL =>"+ requestURL);
        // Debug.Log("request.isNetworkError =>" + request.isNetworkError);
        // Debug.Log("request.isHttpError =>" + request.isHttpError);
        // Debug.Log("request.isHttpError =>" + request.error);
        // Debug.Log("request.result =>" + request.result);
        // Debug.Log("request.downloadHandler.text =>" + request.downloadHandler.text);
        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            callback(Newtonsoft.Json.JsonConvert.DeserializeObject<StoreApiResponse>(result));
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"Web3Auth session restore failed: {request.responseCode} {request.error}. Origin: {(string.IsNullOrEmpty(originToSend) ? "omitted (custom scheme or empty)" : "sent")}.");
#endif
            callback(null);
        }
    }

    public IEnumerator logout(LogoutApiRequest logoutApiRequest, Action<JObject> callback)
    {
        WWWForm data = new WWWForm();
        data.AddField("key", logoutApiRequest.key);
        data.AddField("data", logoutApiRequest.data);
        data.AddField("signature", logoutApiRequest.signature);
        data.AddField("timeout", logoutApiRequest.timeout.ToString());
        // Debug.Log("key during logout session =>" + logoutApiRequest.key);

        var request = UnityWebRequest.Post($"{baseAddress}/store/set", data);
        yield return request.SendWebRequest();

        // Debug.Log("baseAddress =>" + baseAddress);
        // Debug.Log("key =>" + logoutApiRequest.key);
        // Debug.Log("request URL =>"+ requestURL);
        // Debug.Log("request.isNetworkError =>" + request.isNetworkError);
        // Debug.Log("request.isHttpError =>" + request.isHttpError);
        // Debug.Log("request.isHttpError =>" + request.error);
        // Debug.Log("request.result =>" + request.result);
        // Debug.Log("request.downloadHandler.text =>" + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            callback(Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result));
        }
        else
            callback(null);
    }

    public IEnumerator createSession(LogoutApiRequest logoutApiRequest, Action<JObject> callback)
    {
        WWWForm data = new WWWForm();
        data.AddField("key", logoutApiRequest.key);
        data.AddField("data", logoutApiRequest.data);
        data.AddField("signature", logoutApiRequest.signature);
        data.AddField("timeout", logoutApiRequest.timeout.ToString());
        // Debug.Log("key during create session =>" + logoutApiRequest.key);
        var request = UnityWebRequest.Post($"{baseAddress}/store/set", data);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            callback(Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result));
        }
        else
            callback(null);
    }
}
