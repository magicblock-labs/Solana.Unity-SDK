using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public void Receive(string message)
        {

            MessageEvent?.Invoke(message);
        }

        /// <summary>
        /// Register a listener for the message event
        /// </summary>
        /// <param name="task"></param>
        /// <typeparam name="T"></typeparam>
        private void RegisterListener<T>(TaskCompletionSource<T> task)
        {
            var listener = new Action<string>(msg => Receiver(task, msg));
            MessageEvent += listener.Invoke;
            task.Task.ContinueWith(_ => { MessageEvent -= listener.Invoke; });
        }

        /// <summary>
        /// Wrap the receiver listener
        /// </summary>
        /// <param name="task"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        private static void Receiver<T>(TaskCompletionSource<T> task, string message)
        {
            try
            {
                var authorizationResult = JsonConvert.DeserializeObject<Response<T>>(message);
                if (authorizationResult.Error != null)
                {
                    task.SetException(new Exception(authorizationResult.Error.Message));
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
    }
}