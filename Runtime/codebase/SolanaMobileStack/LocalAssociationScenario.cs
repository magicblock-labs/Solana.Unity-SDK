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
    private const string TAG = "[LocalAssoc]";

    private readonly TimeSpan _clientTimeoutMs;
    private readonly MobileWalletAdapterSession _session;
    private readonly int _port;
    private readonly IWebSocket _webSocket;
    private AndroidJavaObject _nativeLocalAssociationScenario;
    private TaskCompletionSource<Response<object>> _startAssociationTaskCompletionSource;

    private bool _didConnect;
    private bool _closed;
    private bool _handledEncryptedMessage;
    private MobileWalletAdapterClient _client;
    private readonly AndroidJavaObject _currentActivity;
    private Queue<Action<IAdapterOperations>> _actions;
    private int _totalActions;
    private int _executedActions;
    private const int ResponseTimeoutSeconds = 45;

    public LocalAssociationScenario(int clientTimeoutMs = 9000)
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _clientTimeoutMs = TimeSpan.FromSeconds(clientTimeoutMs);
        _port = Random.Range(WebSocketsTransportContract.WebsocketsLocalPortMin, WebSocketsTransportContract.WebsocketsLocalPortMax + 1);
        _session = new MobileWalletAdapterSession();
        var webSocketUri = WebSocketsTransportContract.WebsocketsLocalScheme + "://" + WebSocketsTransportContract.WebsocketsLocalHost + ":" + _port + WebSocketsTransportContract.WebsocketsLocalPath;
        _webSocket = WebSocket.Create(webSocketUri, WebSocketsTransportContract.WebsocketsProtocol);

        Debug.Log($"{TAG} Constructor | port={_port} wsUri={webSocketUri} timeout={clientTimeoutMs}ms associationToken={_session.AssociationToken}");

        _webSocket.OnOpen += () =>
        {
            if(_didConnect)return;
            _didConnect = true;
            Debug.Log($"{TAG} OnOpen | connected port={_port}");
            var helloReq = _session.CreateHelloReq();
            Debug.Log($"{TAG} OnOpen | sending HELLO_REQ len={helloReq.Length}");
            _webSocket.Send(helloReq);
            ListenKeyExchange();
        };
        _webSocket.OnClose += (e) =>
        {
            Debug.Log($"{TAG} OnClose | close_code={e} didConnect={_didConnect} closed={_closed} executedActions={_executedActions} pending_rpc={_client?.PendingRequests ?? 0} port={_port}");
            if (!_didConnect || _closed) return;

            // If actions have been dequeued and either:
            //   (a) all actions completed (queue empty), OR
            //   (b) wallet disconnected while RPC requests are still pending (wallet crashed)
            // then close the session instead of reconnecting.
            if (_executedActions > 0 && (_actions.Count == 0 || (_client != null && _client.PendingRequests > 0)))
            {
                var hasPendingRpc = _client != null && _client.PendingRequests > 0;
                var reason = hasPendingRpc ? "wallet_crashed_during_pending_rpc" : "all_actions_complete";
                Debug.Log($"{TAG} OnClose | WALLET_DISCONNECTED reason={reason} pending_rpc={_client?.PendingRequests ?? 0} remaining_actions={_actions.Count} port={_port}");

                if (hasPendingRpc)
                {
                    // Wallet crashed mid-RPC — return error so SIWS failure propagates
                    CloseAssociation(new Response<object>
                    {
                        JsonRpc = "2.0",
                        Error = new Response<object>.ResponseError
                        {
                            Code = -1,
                            Message = $"Wallet disconnected while {_client.PendingRequests} RPC request(s) pending — wallet likely crashed (check logcat for wallet FATAL EXCEPTION)"
                        }
                    });
                }
                else
                {
                    CloseAssociation(new Response<object> { JsonRpc = "2.0" });
                }
                return;
            }

            Debug.Log($"{TAG} OnClose | RECONNECTING port={_port}");
            _webSocket.Connect(awaitConnection: false);
        };
        _webSocket.OnError += (e) =>
        {
            Debug.Log($"{TAG} OnError | error={e} port={_port}");
        };
        _webSocket.OnMessage += ReceivePublicKeyHandler;
    }


    public Task<Response<object>> StartAndExecute(List<Action<IAdapterOperations>> actions)
    {
        if (actions == null || actions.Count == 0)
            throw new ArgumentException("Actions must be non-null and non-empty");

        Debug.Log($"{TAG} StartAndExecute | ENTRY action_count={actions.Count} port={_port}");

        _actions = new Queue<Action<IAdapterOperations>>(actions);
        _totalActions = actions.Count;
        _executedActions = 0;
        var intent = LocalAssociationIntentCreator.CreateAssociationIntent(
            _session.AssociationToken,
            _port);

        Debug.Log($"{TAG} StartAndExecute | launching intent associationToken={_session.AssociationToken} port={_port}");

        _currentActivity.Call("startActivityForResult", intent, 0);
        _currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(TryConnectWs));
        _startAssociationTaskCompletionSource = new TaskCompletionSource<Response<object>>();
        return _startAssociationTaskCompletionSource.Task;
    }

    private async void TryConnectWs()
    {
        Debug.Log($"{TAG} TryConnectWs | START timeout={_clientTimeoutMs.TotalSeconds}s port={_port}");
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
            Debug.Log($"{TAG} TryConnectWs | TIMEOUT wsState={_webSocket.State} didConnect={_didConnect} port={_port}");
        }
        else
        {
            Debug.Log($"{TAG} TryConnectWs | CONNECTED wsState={_webSocket.State} port={_port}");
        }
    }

    private async void ListenKeyExchange()
    {
        Debug.Log($"{TAG} ListenKeyExchange | START waiting for encrypted message");
        while (!_handledEncryptedMessage)
        {
            var timeDelta = TimeSpan.FromMilliseconds(300);
            await Task.Delay(timeDelta);
        }
        Debug.Log($"{TAG} ListenKeyExchange | DONE encrypted session established");
    }

    private void HandleEncryptedSessionPayload(byte[] e)
    {
        Debug.Log($"{TAG} HandleEncryptedSessionPayload | ENTRY encrypted_len={e.Length} didConnect={_didConnect} port={_port}");
        if (!_didConnect)
        {
            Debug.LogError($"{TAG} HandleEncryptedSessionPayload | ERROR not connected, terminating encrypted_len={e.Length}");
            throw new InvalidOperationException("Invalid message received; terminating session");
        }

        try
        {
            var de = _session.DecryptSessionPayload(e);
            var message = System.Text.Encoding.UTF8.GetString(de);
            Debug.Log($"{TAG} HandleEncryptedSessionPayload | decrypted_len={message.Length} message={message}");

            _client.Receive(message);
            var receivedResponse = JsonConvert.DeserializeObject<Response<object>>(message);
            Debug.Log($"{TAG} HandleEncryptedSessionPayload | parsed wasSuccessful={receivedResponse?.WasSuccessful ?? false} failed={receivedResponse?.Failed ?? true} error={receivedResponse?.Error?.Message ?? "null"} error_code={receivedResponse?.Error?.Code.ToString() ?? "null"}");

            ExecuteNextAction(receivedResponse);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{TAG} HandleEncryptedSessionPayload | FATAL type={ex.GetType().Name} msg={ex.Message} encrypted_len={e.Length} port={_port} stack={ex.StackTrace}");
            try { CloseAssociation(new Response<object>
            {
                JsonRpc = "2.0",
                Error = new Response<object>.ResponseError
                {
                    Code = -3,
                    Message = $"Session payload error: {ex.Message}"
                }
            }); } catch (Exception) { }
        }
    }


    private void ReceivePublicKeyHandler(byte[] m)
    {
        try
        {
            Debug.Log($"{TAG} ReceivePublicKeyHandler | ENTRY payload_len={m.Length}");
            _session.GenerateSessionEcdhSecret(m);
            var messageSender = new MobileWalletAdapterWebSocket(_webSocket, _session);
            _client = new MobileWalletAdapterClient(messageSender);
            _webSocket.OnMessage -= ReceivePublicKeyHandler;
            _webSocket.OnMessage += HandleEncryptedSessionPayload;
            Debug.Log($"{TAG} ReceivePublicKeyHandler | ECDH complete, client created, encrypted channel ready payload_len={m.Length}");

            // Executing the first action
            ExecuteNextAction();
        }
        catch (Exception e)
        {
            Debug.Log($"{TAG} ReceivePublicKeyHandler | EXCEPTION type={e.GetType().Name} msg={e.Message}");
            Console.WriteLine(e);
        }
    }

    private void ExecuteNextAction(Response<object> response = null)
    {
        Debug.Log($"{TAG} ExecuteNextAction | remaining_actions={_actions.Count} has_response={response != null} response_failed={response?.Failed ?? false} response_error={response?.Error?.Message ?? "null"}");

        if (_actions.Count == 0 || response is { Failed: true })
        {
            Debug.Log($"{TAG} ExecuteNextAction | CLOSING reason={(_actions.Count == 0 ? "no_more_actions" : "response_failed")}");
            CloseAssociation(response);
            return;
        }
        var action = _actions.Dequeue();
        _executedActions++;
        Debug.Log($"{TAG} ExecuteNextAction | DEQUEUED action_index={_executedActions}/{_totalActions} remaining_after={_actions.Count}");
        action.Invoke(_client);

        // If this was the last action and it didn't send any RPC (no-op),
        // close the session immediately. If it DID send an RPC, the response
        // will trigger HandleEncryptedSessionPayload → ExecuteNextAction → close.
        if (_actions.Count == 0 && _client.PendingRequests == 0)
        {
            Debug.Log($"{TAG} ExecuteNextAction | LAST_ACTION_NOOP no pending RPC after last action, closing immediately port={_port}");
            CloseAssociation(new Response<object> { JsonRpc = "2.0" });
        }
        // If the last action sent an RPC, start a timeout to catch wallets that
        // never respond (e.g. Phantom sign_messages → SeedVault delegation hang).
        else if (_actions.Count == 0 && _client.PendingRequests > 0)
        {
            Debug.Log($"{TAG} ExecuteNextAction | RESPONSE_TIMER_START pending_rpc={_client.PendingRequests} timeout={ResponseTimeoutSeconds}s port={_port}");
            StartResponseTimeout();
        }
    }

    private async void StartResponseTimeout()
    {
        await Task.Delay(TimeSpan.FromSeconds(ResponseTimeoutSeconds));
        if (_closed) return;
        Debug.Log($"{TAG} RESPONSE_TIMEOUT | no response after {ResponseTimeoutSeconds}s, pending_requests={_client?.PendingRequests ?? 0} executedActions={_executedActions} port={_port} — closing with error");
        CloseAssociation(new Response<object>
        {
            JsonRpc = "2.0",
            Error = new Response<object>.ResponseError
            {
                Code = -2,
                Message = $"Wallet did not respond to RPC within {ResponseTimeoutSeconds}s — SIWS fallback timed out (likely SeedVault delegation issue)"
            }
        });
    }

    private async void CloseAssociation(Response<object> response)
    {
        Debug.Log($"{TAG} CloseAssociation | START was_successful={response?.WasSuccessful ?? true} error={response?.Error?.Message ?? "null"} port={_port}");
        _closed = true;
        _webSocket.OnMessage -= HandleEncryptedSessionPayload;
        _handledEncryptedMessage = true;
        await _webSocket.Close();
        Debug.Log($"{TAG} CloseAssociation | DONE ws_closed port={_port}");
        _startAssociationTaskCompletionSource.SetResult(response);
    }
}
