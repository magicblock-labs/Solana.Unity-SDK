using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Solana.Unity.SDK
{
    public class SignatureStatus {
        public string confirmationStatus;
        public long confirmations;
        public object error;
        public long slot;
    }
}