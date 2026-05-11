using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Solana.Unity.SolanaMobileStack;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{

    [Preserve]
    public class JsonRpc20Client
    {
        private delegate void MessageHandler(string message);

        private event MessageHandler MessageEvent;

        private readonly IMessageSender _messageSender;

        protected JsonRpc20Client(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        protected Task<T> SendRequest<T>(JsonRequest jsonRequest)
        {
            var message = JsonConvert.SerializeObject(jsonRequest);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            _messageSender.Send(messageBytes);
            var authTaskCompletionSource = new TaskCompletionSource<T>();

            // Register the message listener
            RegisterListener(authTaskCompletionSource);
            return authTaskCompletionSource.Task;
        }

        protected Task<JToken> SendRequestRaw(JsonRequest jsonRequest)
        {
            var message = JsonConvert.SerializeObject(jsonRequest);
            UnityEngine.Debug.Log($"[MWA Wire] → {message}");
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            _messageSender.Send(messageBytes);
            var rawTaskCompletionSource = new TaskCompletionSource<JToken>();

            // Register the message listener (raw variant — returns the JToken result field)
            RegisterRawListener(rawTaskCompletionSource);
            return rawTaskCompletionSource.Task;
        }

        public void Receive(string message)
        {

            MessageEvent?.Invoke(message);
        }

        private void RegisterListener<T>(TaskCompletionSource<T> task)
        {
            var listener = new Action<string>(msg => Receiver(task, msg));
            MessageEvent += listener.Invoke;
            task.Task.ContinueWith(_ => { MessageEvent -= listener.Invoke; });
        }

        private void RegisterRawListener(TaskCompletionSource<JToken> task)
        {
            var listener = new Action<string>(msg => ReceiverRaw(task, msg));
            MessageEvent += listener.Invoke;
            task.Task.ContinueWith(_ => { MessageEvent -= listener.Invoke; });
        }

        private static void Receiver<T>(TaskCompletionSource<T> task, string message)
        {
            try
            {
                var authorizationResult = JsonConvert.DeserializeObject<Response<T>>(message);
                if (authorizationResult.Error != null)
                {
                    task.SetException(new JsonRpcException(
                        (int)authorizationResult.Error.Code,
                        authorizationResult.Error.Message,
                        null));
                }
                else
                {
                    task.SetResult(authorizationResult.Result);
                }
            }
            catch (JsonException e)
            {
                task.SetException(e);
            }

        }

        private static void ReceiverRaw(TaskCompletionSource<JToken> task, string message)
        {
            try
            {
                var envelope = JObject.Parse(message);
                var errorToken = envelope["error"];
                if (errorToken != null && errorToken.Type != JTokenType.Null)
                {
                    var code = errorToken["code"]?.ToObject<int>() ?? 0;
                    var errorMessage = errorToken["message"]?.ToString() ?? "Unknown JSON-RPC error";
                    var data = errorToken["data"];
                    task.SetException(new JsonRpcException(code, errorMessage, data));
                }
                else
                {
                    var resultToken = envelope["result"];
                    if (resultToken == null || resultToken.Type == JTokenType.Null)
                        task.SetException(new JsonRpcException(0, "JSON-RPC response has null result", null));
                    else
                        task.SetResult(resultToken);
                }
            }
            catch (JsonException e)
            {
                task.SetException(e);
            }
        }
    }
}