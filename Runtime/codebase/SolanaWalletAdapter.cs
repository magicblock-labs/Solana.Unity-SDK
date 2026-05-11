using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SolanaMobileStack;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    
    [Serializable]
    public class SolanaWalletAdapterOptions
    {
        public SolanaMobileWalletAdapterOptions solanaMobileWalletAdapterOptions;
        public SolanaWalletAdapterWebGLOptions solanaWalletAdapterWebGLOptions;
        public PhantomWalletOptions phantomWalletOptions;
    }
    
    public class SolanaWalletAdapter: WalletBase
    {
        private readonly WalletBase _internalWallet;

        public event Action OnWalletDisconnected;
        public event Action OnWalletReconnected;

        public SolanaWalletAdapter(SolanaWalletAdapterOptions options, RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false, IAuthorizationCache authCache = null) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            #if UNITY_ANDROID
            #pragma warning disable CS0618
            var mwaOptions = options.solanaMobileWalletAdapterOptions ?? new SolanaMobileWalletAdapterOptions();
            if (authCache != null)
                mwaOptions.Cache ??= authCache;
            _internalWallet = new SolanaMobileWalletAdapter(mwaOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #elif UNITY_WEBGL
            #pragma warning disable CS0618
            _internalWallet = new SolanaWalletAdapterWebGL(options.solanaWalletAdapterWebGLOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #elif UNITY_IOS
            #pragma warning disable CS0618
            _internalWallet = new PhantomDeepLink(options.phantomWalletOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #else
            #endif

            #if UNITY_ANDROID
            if (_internalWallet is SolanaMobileWalletAdapter mobileAdapter)
            {
                mobileAdapter.OnWalletDisconnected += () => OnWalletDisconnected?.Invoke();
                mobileAdapter.OnWalletReconnected += () => OnWalletReconnected?.Invoke();
            }
            #endif
        }

        protected override Task<Account> _Login(string password = null)
        {
            if (_internalWallet != null)
                return _internalWallet.Login(password);
            throw new NotImplementedException();
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            if (_internalWallet != null)
                return _internalWallet.SignTransaction(transaction);
            throw new NotImplementedException();
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            if (_internalWallet != null)
                return _internalWallet.SignAllTransactions(transactions);
            throw new NotImplementedException();
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            if (_internalWallet != null)
                return _internalWallet.SignMessage(message);
            throw new NotImplementedException();
        }
        
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }

        public override void Logout()
        {
            base.Logout();
            _internalWallet?.Logout();
        }

        public async Task Disconnect()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
            {
                await mobileAdapter.Disconnect();
                return;
            }
            if (_internalWallet != null)
                throw new NotImplementedException();
        }

        public async Task DisconnectWallet()
        {
            await Disconnect();
        }

        public async Task<ReconnectResult> Reconnect()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.Reconnect();
            if (_internalWallet != null)
                throw new NotImplementedException();
            return null;
        }

        public async Task ReconnectWallet()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
            {
                await mobileAdapter.ReconnectWallet();
                return;
            }
            if (_internalWallet != null)
                throw new NotImplementedException();
        }

        public async Task<DeauthorizeResult> Deauthorize()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.Deauthorize();
            if (_internalWallet != null)
                throw new NotImplementedException();
            return null;
        }

        public async Task<(Account, SignInResult)> LoginWithSignIn(SignInPayload signInPayload)
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.LoginWithSignIn(signInPayload);
            if (_internalWallet != null)
                throw new NotImplementedException();
            throw new NotImplementedException();
        }

        public async Task<SignAndSendTxResult> SignAndSendTransactions(
            Transaction[] transactions, SendOptions options = null)
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.SignAndSendTransactions(transactions, options);
            if (_internalWallet != null)
                throw new NotImplementedException();
            throw new NotImplementedException();
        }

        public async Task<string> CloneAuthorization()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.CloneAuthorization();
            if (_internalWallet != null)
                throw new NotImplementedException();
            return null;
        }

        public Task<byte[]> SignMessage(string message)
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return mobileAdapter.SignMessage(message);
            if (_internalWallet != null)
                throw new NotImplementedException();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the connected wallet's supported features and limits.
        /// </summary>
        /// <returns>
        /// A <see cref="CapabilitiesResult"/> containing wallet feature limits
        /// (MaxTransactionsPerRequest, MaxMessagesPerRequest,
        /// SupportedTransactionVersions, SupportsCloneAuthorization)
        /// when running on Android with a connected SolanaMobileWalletAdapter.
        /// Returns null when _internalWallet is null or not configured.
        /// Throws <see cref="NotImplementedException"/> when _internalWallet
        /// is non-null but is not a SolanaMobileWalletAdapter (e.g. WebGL,
        /// iOS). Callers must handle the null return case.
        /// </returns>
        public async Task<CapabilitiesResult> GetCapabilities()
        {
            var mobileAdapter = _internalWallet as SolanaMobileWalletAdapter;
            if (mobileAdapter != null)
                return await mobileAdapter.GetCapabilities();
            if (_internalWallet != null)
                throw new NotImplementedException();
            return null;
        }
    }
}