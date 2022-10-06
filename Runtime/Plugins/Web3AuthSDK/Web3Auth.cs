using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;
using UnityEngine;
using System.Web;
using System.Net;
using System.Collections;

public class Web3Auth: MonoBehaviour
{
    public enum Network
    {
        MAINNET, TESTNET, CYAN
    }

    private Web3AuthOptions web3AuthOptions;
    private Dictionary<string, object> initParams;

    private Web3AuthResponse web3AuthResponse;

    public event Action<Web3AuthResponse> onLogin;
    public event Action onLogout;

    [SerializeField]
    private string clientId;

    [SerializeField]
    private string redirectUri;

    [SerializeField]
    private Web3Auth.Network network;

    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public void Awake()
    {
        this.initParams = new Dictionary<string, object>();

        this.initParams["clientId"] = clientId;
        this.initParams["network"] = network.ToString().ToLower();

        if (!string.IsNullOrEmpty(redirectUri))
            this.initParams["redirectUrl"] = redirectUri;

        Application.deepLinkActivated += onDeepLinkActivated;
        if (!string.IsNullOrEmpty(Application.absoluteURL))
            onDeepLinkActivated(Application.absoluteURL);
    }

    public void setOptions(Web3AuthOptions web3AuthOptions)
    {

        this.web3AuthOptions = web3AuthOptions;
        
        if (this.web3AuthOptions.redirectUrl != null)
            this.initParams["clientId"] = this.web3AuthOptions.clientId;

        if (this.web3AuthOptions.redirectUrl != null)
            this.initParams["redirectUrl"] = this.web3AuthOptions.redirectUrl;

        if (this.web3AuthOptions.whiteLabel != null)
            this.initParams["whiteLabel"] = JsonConvert.SerializeObject(this.web3AuthOptions.whiteLabel);

        if (this.web3AuthOptions.loginConfig != null)
            this.initParams["loginConfig"] = JsonConvert.SerializeObject(this.web3AuthOptions.loginConfig);
        
    }

    private void onDeepLinkActivated(string url)
    {
        this.setResultUrl(new Uri(url));
    }

#if UNITY_STANDALONE || UNITY_EDITOR
    private string StartLocalWebserver()
    {
        HttpListener httpListener = new HttpListener();

        var redirectUrl = $"http://localhost:{Utils.GetRandomUnusedPort()}";

        httpListener.Prefixes.Add($"{redirectUrl}/complete/");
        httpListener.Prefixes.Add($"{redirectUrl}/auth/");
        httpListener.Start();
        httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest), httpListener);

        return redirectUrl + "/complete/";
    }

    private void IncomingHttpRequest(IAsyncResult result)
    {

        // get back the reference to our http listener
        HttpListener httpListener = (HttpListener)result.AsyncState;

        // fetch the context object
        HttpListenerContext httpContext = httpListener.EndGetContext(result);

        // if we'd like the HTTP listener to accept more incoming requests, we'd just restart the "get context" here:
        // httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest),httpListener);
        // however, since we only want/expect the one, single auth redirect, we don't need/want this, now.
        // but this is what you would do if you'd want to implement more (simple) "webserver" functionality
        // in your project.

        // the context object has the request object for us, that holds details about the incoming request
        HttpListenerRequest httpRequest = httpContext.Request;
        HttpListenerResponse httpResponse = httpContext.Response;

        if (httpRequest.Url.LocalPath == "/complete/")
        {

            httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest), httpListener);

            var responseString = @"
                <!DOCTYPE html>
                <html>
                <head>
                  <meta charset=""utf-8"">
                  <meta name=""viewport"" content=""width=device-width"">
                  <title>Web3Auth</title>
                  <link href=""https://fonts.googleapis.com/css2?family=DM+Sans:wght@500&display=swap"" rel=""stylesheet"">
                </head>
                <body style=""padding:0;margin:0;font-size:10pt;font-family: 'DM Sans', sans-serif;"">
                  <div style=""display:flex;align-items:center;justify-content:center;height:100vh;display: none;"" id=""success"">
                    <div style=""text-align:center"">
                       <h2 style=""margin-bottom:0""> Authenticated successfully</h2>
                       <p> You can close this tab/window now </p>
                    </div>
                  </div>
                  <div style=""display:flex;align-items:center;justify-content:center;height:100vh;display: none;"" id=""error"">
                    <div style=""text-align:center"">
                       <h2 style=""margin-bottom:0""> Authentication failed</h2>
                       <p> Please try again </p>
                    </div>
                  </div>
                  <script>
                    if (window.location.hash.trim() == """") {
                        document.querySelector(""#error"").style.display=""flex"";
                    } else {
                        fetch(`http://${window.location.host}/auth/?code=${window.location.hash.slice(1,window.location.hash.length)}`).then(function(response) {
                          console.log(response);
                          document.querySelector(""#success"").style.display=""flex"";
                        }).catch(function(error) {
                          console.log(error);
                          document.querySelector(""#error"").style.display=""flex"";
                        });
                    }
                    
                  </script>
                </body>
                </html>
            ";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            httpResponse.ContentLength64 = buffer.Length;
            System.IO.Stream output = httpResponse.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }

        if (httpRequest.Url.LocalPath == "/auth/")
        {
            var responseString = @"ok";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            httpResponse.ContentLength64 = buffer.Length;
            System.IO.Stream output = httpResponse.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            string code = httpRequest.QueryString.Get("code");
            if (!string.IsNullOrEmpty(code))
            {
                this.setResultUrl(new Uri($"http://localhost#{code}"));
            }

            httpListener.Close();
        }
    }
#endif

    private void request(string  path, LoginParams loginParams = null, Dictionary<string, object> extraParams = null)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        this.initParams["redirectUrl"] = StartLocalWebserver();
#endif
        Dictionary<string, object> paramMap = new Dictionary<string, object>();
        paramMap["init"] = this.initParams;
        paramMap["params"] = loginParams == null ? (object)new Dictionary<string, object>() : (object)loginParams;

        if (extraParams != null && extraParams.Count > 0)
            foreach(KeyValuePair<string, object> item in extraParams)
            {
                (paramMap["params"] as Dictionary<string, object>) [item.Key] = item.Value;
            }

        string hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(paramMap, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

        UriBuilder uriBuilder = new UriBuilder(this.web3AuthOptions.sdkUrl);
        uriBuilder.Path = path;
        uriBuilder.Fragment = hash;

        Utils.LaunchUrl(uriBuilder.ToString());
    }

    public void setResultUrl(Uri uri)
    {
        string hash = uri.Fragment;
        if (hash == null)
            throw new UserCancelledException();

        hash = hash.Remove(0, 1);

        Dictionary<string, string> queryParameters = Utils.ParseQuery(uri.Query);

        if (queryParameters.Keys.Contains("error"))
            throw new UnKnownException(queryParameters["error"]);

        this.web3AuthResponse = JsonConvert.DeserializeObject<Web3AuthResponse>(Encoding.UTF8.GetString(Utils.DecodeBase64(hash)));
        if (!string.IsNullOrEmpty(this.web3AuthResponse.error))
            throw new UnKnownException(web3AuthResponse.error);

        if (string.IsNullOrEmpty(this.web3AuthResponse.privKey) || string.IsNullOrEmpty(this.web3AuthResponse.privKey.Trim('0')))
            this.Enqueue(() => this.onLogout?.Invoke());
        else
            this.Enqueue(() => this.onLogin?.Invoke(this.web3AuthResponse));

#if UNITY_IOS
        Utils.Dismiss();
#endif
    }

    public void login(LoginParams loginParams)
    {
        request("login", loginParams);
    }

    public void logout(Dictionary<string, object> extraParams)
    {
        request("logout", extraParams: extraParams);
    }

    public void logout(Uri redirectUrl = null, string appState = null)
    {
        Dictionary<string, object> extraParams = new Dictionary<string, object>();
        if (redirectUrl != null)
            extraParams["redirectUrl"] = redirectUrl.ToString();

        if (appState != null)
            extraParams["appState"] = appState;

        logout(extraParams);
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    private void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => {
                StartCoroutine(ActionWrapper(action));
            });
        }
    }

    private IEnumerator ActionWrapper(Action a)
    {
        a();
        yield return null;
    }
}
