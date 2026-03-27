using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Solana.Unity.SDK;
using UnityEngine;
using Random = UnityEngine.Random;
using WebSocket = NativeWebSocket.WebSocket;
using WebSocketState = NativeWebSocket.WebSocketState;

// ReSharper disable once CheckNamespace

public class LocalAssociationScenario
{
    private readonly TimeSpan _clientTimeoutMs;
    private readonly MobileWalletAdapterSession _session;
    private readonly int _port;
    private readonly IWebSocket _webSocket;
    private AndroidJavaObject _nativeLocalAssociationScenario;
    private TaskCompletionSource<Response<object>> _startAssociationTaskCompletionSource;

    private bool _didConnect;
    private bool _handledEncryptedMessage;
    private MobileWalletAdapterClient _client;
    private readonly AndroidJavaObject _currentActivity;
    private Queue<Action<IAdapterOperations>> _actions;
    private Queue<Func<IAdapterOperations, Task>> _asyncActions;
    private bool _useAsyncActions;

    public LocalAssociationScenario(int clientTimeoutMs = 9000)
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _clientTimeoutMs = TimeSpan.FromSeconds(clientTimeoutMs);
        _port = Random.Range(WebSocketsTransportContract.WebsocketsLocalPortMin, WebSocketsTransportContract.WebsocketsLocalPortMax + 1);
        _session = new MobileWalletAdapterSession();
        var webSocketUri = WebSocketsTransportContract.WebsocketsLocalScheme + "://" + WebSocketsTransportContract.WebsocketsLocalHost + ":" + _port + WebSocketsTransportContract.WebsocketsLocalPath;
        _webSocket = WebSocket.Create(webSocketUri, WebSocketsTransportContract.WebsocketsProtocol);
        _webSocket.OnOpen += () =>
        {
            if(_didConnect)return;
            _didConnect = true;
            var helloReq = _session.CreateHelloReq();
            _webSocket.Send(helloReq);
            ListenKeyExchange();
        };
        _webSocket.OnClose += (e) =>
        {
            if (!_didConnect) return;
            _webSocket.Connect(awaitConnection: false);
        };
        _webSocket.OnError += (e) =>
        {
            Debug.Log("WebSocket Error: " + e);
        };
        _webSocket.OnMessage += ReceivePublicKeyHandler;
    }


    public Task<Response<object>> StartAndExecute(List<Action<IAdapterOperations>> actions)
    {
        if (actions == null || actions.Count == 0)
            throw new ArgumentException("Actions must be non-null and non-empty");
        _actions = new Queue<Action<IAdapterOperations>>(actions);
        var intent = LocalAssociationIntentCreator.CreateAssociationIntent(
            _session.AssociationToken, 
            _port);
        _currentActivity.Call("startActivityForResult", intent, 0);
        _currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(TryConnectWs));
        _startAssociationTaskCompletionSource = new TaskCompletionSource<Response<object>>();
        return _startAssociationTaskCompletionSource.Task;
    }

    /// <summary>
    /// Async-compatible overload of <see cref="StartAndExecute"/>.
    /// Accepts async delegate actions (<see cref="Func{IAdapterOperations, Task}"/>) and properly
    /// awaits each one before executing the next, preventing fire-and-forget race conditions.
    /// </summary>
    public Task<Response<object>> StartAndExecuteAsync(List<Func<IAdapterOperations, Task>> asyncActions)
    {
        if (asyncActions == null || asyncActions.Count == 0)
            throw new ArgumentException("Actions must be non-null and non-empty");
        _asyncActions = new Queue<Func<IAdapterOperations, Task>>(asyncActions);
        _useAsyncActions = true;
        var intent = LocalAssociationIntentCreator.CreateAssociationIntent(
            _session.AssociationToken, 
            _port);
        _currentActivity.Call("startActivityForResult", intent, 0);
        _currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(TryConnectWs));
        _startAssociationTaskCompletionSource = new TaskCompletionSource<Response<object>>();
        return _startAssociationTaskCompletionSource.Task;
    }
    
    private async void TryConnectWs()
    {
        var timeout = _clientTimeoutMs;
        while (_webSocket.State != WebSocketState.Open && !_didConnect && timeout.TotalSeconds > 0)
        {
            await _webSocket.Connect(awaitConnection: false);
            var timeDelta = TimeSpan.FromMilliseconds(500);
            timeout -= timeDelta;
            await Task.Delay(timeDelta);
        }
        if (_webSocket.State != WebSocketState.Open)
        {
            Debug.Log("Error: timeout");
        }
    }

    private async void ListenKeyExchange()
    {
        while (!_handledEncryptedMessage)
        {
            var timeDelta = TimeSpan.FromMilliseconds(300);
            await Task.Delay(timeDelta);
        }
    }

    private void HandleEncryptedSessionPayload(byte[] e)
    {
        if (!_didConnect)
        {
            throw new InvalidOperationException("Invalid message received; terminating session");
        }

        var de = _session.DecryptSessionPayload(e);
        var message = System.Text.Encoding.UTF8.GetString(de);
        _client.Receive(message);
        var receivedResponse = JsonConvert.DeserializeObject<Response<object>>(message);;
        ExecuteNextAction(receivedResponse);
    }


    private void ReceivePublicKeyHandler(byte[] m)
    {
        try
        {
            _session.GenerateSessionEcdhSecret(m);
            var messageSender = new MobileWalletAdapterWebSocket(_webSocket, _session);
            _client = new MobileWalletAdapterClient(messageSender);
            _webSocket.OnMessage -= ReceivePublicKeyHandler;
            _webSocket.OnMessage += HandleEncryptedSessionPayload;
            
            // Executing the first action
            ExecuteNextAction();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void ExecuteNextAction(Response<object> response = null)
    {
        if (_actions.Count == 0 || response is { Failed: true })
            CloseAssociation(response);

        if (_useAsyncActions)
        {
            // Properly await the async action before proceeding
            ExecuteNextActionAsync(response);
        }
        else
        {
            var action = _actions.Dequeue();
            action.Invoke(_client);
        }
    }

    private async void ExecuteNextActionAsync(Response<object> response = null)
    {
        if (_asyncActions.Count == 0 || response is { Failed: true })
        {
            CloseAssociation(response);
            return;
        }
        var action = _asyncActions.Dequeue();
        try
        {
            await action.Invoke(_client);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MWA] Async action failed: {e}");
            CloseAssociation(new Response<object> { Error = new Response<object>.ResponseError { Message = e.Message } });
        }
    }

    private async void CloseAssociation(Response<object> response)
    {
        _webSocket.OnMessage -= HandleEncryptedSessionPayload;
        _handledEncryptedMessage = true;
        await _webSocket.Close();
        _startAssociationTaskCompletionSource.SetResult(response);
    }
}
