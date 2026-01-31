using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Solana.Unity.SDK;
using UnityEngine;
using Random = UnityEngine.Random;
using WebSocket = NativeWebSocket.WebSocket;
using WebSocketState = NativeWebSocket.WebSocketState;

// ReSharper disable once CheckNamespace

public class LocalAssociationScenario : IDisposable
{
    private readonly TimeSpan _overallTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _keyExchangeTimeout = TimeSpan.FromSeconds(20);

    private readonly AndroidJavaObject _currentActivity;
    private readonly int _port;
    private readonly MobileWalletAdapterSession _session;
    private IWebSocket _webSocket;
    private MobileWalletAdapterClient _client;

    private bool _isConnecting;
    private bool _disposed;

    private TaskCompletionSource<bool> _wsConnected;
    private TaskCompletionSource<Response<object>> _responseTcs;
    private TaskCompletionSource<Response<object>> _tcs;
    private CancellationToken _cancellationToken;

    public LocalAssociationScenario()
    {
        _currentActivity = GetCurrentActivity();
        _port = RandomPort();
        _session = new MobileWalletAdapterSession();
    }

    private static AndroidJavaObject GetCurrentActivity()
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    }

    public async Task<Response<object>> StartAndExecute(List<Action<IAdapterOperations>> actions,
        CancellationToken ct = default)
    {
        if (actions == null || actions.Count == 0)
            throw new ArgumentException("Actions required");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_overallTimeout);

        _cancellationToken = ct;
        _tcs = new TaskCompletionSource<Response<object>>();

        StartActivityForAssociation(_session.AssociationToken, _port);

        Debug.Log("[MWA] Waiting for websocket connection");
        await Task.Run(async () =>
        {
            try
            {
                Debug.Log("[MWA Connect Thread] Started");
                _isConnecting = true;
            
                await ConnectWithBackoffAsync();
            
                Debug.Log("[MWA Connect Thread] Completed");
                _isConnecting = false;
            
                var helloReq = _session.CreateHelloReq();
                await _webSocket.Send(helloReq);

                Debug.Log("[MWA] Hello sent. Waiting for pubkey...");

                await WaitForKeyExchangeAsync(cts.Token);

                Debug.Log("[MWA] Pubkey received, session is encrypted");
            
                var queue = new Queue<Action<IAdapterOperations>>(actions);
                Response<object> lastResponse = null;

                while (queue.Count > 0)
                {
                    _responseTcs = new TaskCompletionSource<Response<object>>();
                
                    var action = queue.Dequeue();
                    Debug.Log($"[MWA] Invoking action {action.Method.Name}");
                    action.Invoke(_client);
                
                    lastResponse = await _responseTcs.Task;

                    ct.ThrowIfCancellationRequested();
                }

                _tcs.TrySetResult(lastResponse ?? new Response<object>());
            }
            catch (OperationCanceledException)
            {
                _tcs.TrySetResult(new Response<object>
                {
                    Error = new Response<object>.ResponseError { Message = "Timeout or cancelled" } 
                });
            }
            catch (Exception ex)
            {
                Debug.Log($"[MWA] Association failed: {ex}");
                _tcs.TrySetException(ex);
            }
            finally
            {
                await CleanupAsync();
            }
        }, cts.Token);
        
        return await _tcs.Task;
    }

    private static int RandomPort()
    {
        return Random.Range(WebSocketsTransportContract.WebsocketsLocalPortMin,
            WebSocketsTransportContract.WebsocketsLocalPortMax + 1);
    }

    private static IWebSocket CreateWebSocket(int port)
    {
        var webSocketUri = WebSocketsTransportContract.WebsocketsLocalScheme + "://" +
                           WebSocketsTransportContract.WebsocketsLocalHost + ":" + port +
                           WebSocketsTransportContract.WebsocketsLocalPath;
        
        Debug.Log($"[MWA] Websocket created with URI {webSocketUri}");
        return WebSocket.Create(webSocketUri, WebSocketsTransportContract.WebsocketsProtocol);
    }

    private void StartActivityForAssociation(string associationToken, int port)
    {
        var intent = LocalAssociationIntentCreator.CreateAssociationIntent(associationToken, port);
        _currentActivity.Call("startActivityForResult", intent, 0);
        Debug.Log($"[MWA] Launched intent for port {port}, token {associationToken}");
    }

    private async Task ConnectWithBackoffAsync()
    {
        const int maxAttempts = 12;
        const int delayStart = 400;
        const int delayCap = 3000;

        var attempt = 0;
        var delayMs = delayStart;

        // Short delay to give wallet time to start websocket
        Debug.Log($"[MWA] Start delay");
        await Task.Delay(500, _cancellationToken);
        Debug.Log($"[MWA] Delay over");
        
        do
        {
            if (_webSocket != null)
            {
                _webSocket.OnOpen -= OnWsOpen;
                _webSocket.OnError -= OnWsError;
                _webSocket.OnClose -= OnWsClose;
                _webSocket.OnMessage -= OnWsMessage;
                _webSocket = null;
            }
        
            _webSocket = CreateWebSocket(_port);
            _webSocket.OnOpen += OnWsOpen;
            _webSocket.OnError += OnWsError;
            _webSocket.OnClose += OnWsClose;
            _webSocket.OnMessage += OnWsMessage;
            
            var startTime = DateTime.UtcNow;
            _wsConnected = new TaskCompletionSource<bool>();
            
            attempt++;
            Debug.Log($"[MWA] Connect attempt {attempt}, state: {_webSocket.State}");
            _webSocket.Connect();
            
            var success = await _wsConnected.Task;
            Debug.Log($"[MWA] Connect attempt {attempt} result, state: {_webSocket.State}");
            
            if (success)
                return;
            
            var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            if (duration < delayMs)
            {
                await Task.Delay(delayMs - duration, _cancellationToken);
            }
            
            delayMs = Math.Min(delayMs * 2, delayCap);
            
        } while (_webSocket.State != WebSocketState.Open && !_cancellationToken.IsCancellationRequested &&
                 attempt < maxAttempts);

        throw new TimeoutException("WebSocket connect timed out after max attempts");
    }

    private void OnWsOpen()
    {
        Debug.Log("[MWA] WS Opened");

        if (_isConnecting)
        {
            _wsConnected.TrySetResult(true);
        }
    }

    private void OnWsClose(WebSocketCloseCode closeCode)
    {
        Debug.Log($"[MWA] WS Closed: {closeCode}");
        if (closeCode == WebSocketCloseCode.Normal) 
            return;
        
        if (!_isConnecting)
        {
            _tcs?.TrySetException(new Exception($"WS closed unexpectedly: {closeCode}"));
        }
        else
        {
            _wsConnected.TrySetResult(false);
        }
    }

    private static void OnWsError(string message)
    {
        Debug.Log($"[MWA] WS Error: {message}");
    }

    private void OnWsMessage(byte[] bytes)
    {
        try
        {
            // First message expected: raw pubkey for ECDH
            if (_client == null)
            {
                _session.GenerateSessionEcdhSecret(bytes);
                var messageSender = new MobileWalletAdapterWebSocket(_webSocket, _session);
                _client = new MobileWalletAdapterClient(messageSender);

                Debug.Log("[MWA] Key exchange complete → encrypted session ready");
            }
            // All other should be encrypted messages
            else
            {
                var decrypted = _session.DecryptSessionPayload(bytes);
                var json = System.Text.Encoding.UTF8.GetString(decrypted);
                _client.Receive(json);

                Debug.Log($"[MWA] Received: {json}");

                var response = JsonConvert.DeserializeObject<Response<object>>(json);
                _responseTcs.TrySetResult(response);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[MWA] Message handler error: {ex}");
            _responseTcs.TrySetException(ex);
        }
    }

    private Task WaitForKeyExchangeAsync(CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            var start = DateTime.UtcNow;
            while (_client == null)
            {
                if (ct.IsCancellationRequested || DateTime.UtcNow - start > _keyExchangeTimeout)
                    throw new TimeoutException("Key exchange timed out");

                await Task.Delay(200, ct);
            }
        }, ct);
    }

    private async Task CleanupAsync()
    {
        if (_webSocket is { State: WebSocketState.Open })
            await _webSocket.Close();

        if (_webSocket != null)
        {
            _webSocket.OnOpen -= OnWsOpen;
            _webSocket.OnMessage -= OnWsMessage;
            _webSocket.OnError -= OnWsError;
            _webSocket.OnClose -= OnWsClose;
            _webSocket = null;
        }
        
        _client = null;
        _disposed = true;
    }

    void IDisposable.Dispose()
    {
        if (_disposed) return;
        _ = CleanupAsync();
    }
}
