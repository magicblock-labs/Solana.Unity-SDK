namespace Solana.Unity.SolanaMobileStack
{
    public enum MobileWalletAdapterErrorCode
    {
        AuthorizationFailed = -1,

        InvalidPayloads = -2,

        NotSigned = -3,

        NotSubmitted = -4,

        NotCloned = -5,

        TooManyPayloads = -6,

        ChainNotSupported = -7,

        AttestOriginAndroid = -100,

        MethodNotFound = -32601,

        InvalidParams = -32602,
    }
}
