using System;

// ReSharper disable once CheckNamespace

namespace  Solana.Unity.SDK
{
    [Serializable]
    public class PhantomMessagePayload
    {
        public string message;
        public string session;
        public string display;

        public PhantomMessagePayload(string message, string session, string display)
        {
            this.message = message;
            this.session = session;
            this.display = display;
        }
    }
}