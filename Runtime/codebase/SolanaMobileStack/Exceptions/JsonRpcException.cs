using System;
using Newtonsoft.Json.Linq;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class JsonRpcException : Exception
    {
        public int Code { get; }

        public JToken? Data { get; }

        public JsonRpcException(int code, string message, JToken? data) : base(message)
        {
            Code = code;
            Data = data;
        }
    }
}
