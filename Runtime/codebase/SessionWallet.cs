using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.KeyStore.Exceptions;
using Solana.Unity.KeyStore.Services;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class SessionWallet : InGameWallet
    {
        private const string EncryptedKeystoreKey = "SessionKeystore";

        public SessionWallet(RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
        }

        /// <summary>
        /// Returns an instance of Keypair from a mnemonic
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <returns></returns>
        private static Account FromMnemonic(string mnemonic)
        {
            var wallet = new Wallet.Wallet(new Mnemonic(mnemonic));
            return wallet.Account;
        }

        /// <summary>
        /// Returns an instance of Keypair from a secret key
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        private static Account FromSecretKey(string secretKey)
        {
            try
            {
                var wallet = new Wallet.Wallet(new PrivateKey(secretKey).KeyBytes, "", SeedMode.Bip39);
                return wallet.Account;
            }
            catch (ArgumentException)
            {
                return null;
            }

        }

        /// <summary>
        /// Returns an instance of Keypair from a Byte Array
        /// </summary>
        /// <param name="secretByteArray"></param>
        /// <returns></returns>
        private static Account FromByteArray(byte[] secretByteArray)
        {
            var wallet = new Wallet.Wallet(secretByteArray, "", SeedMode.Bip39);
            return wallet.Account;
        }

        /// <summary>
        /// Takes a string as input and checks if it is a valid mnemonic
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static bool IsMnemonic(string secret)
        {
            return secret.Split(' ').Length is 12 or 24;
        }
        /// <summary>
        /// Takes a string as input and checks if it is a valid byte array
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static bool IsByteArray(string secret)
        {
            return secret.StartsWith('[') && secret.EndsWith(']');
        }

        /// <summary>
        /// Takes a string as input and tries to parse it into a Keypair
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static Account ParseByteArray(string secret)
        {
            var parsed = secret
                .Replace(",]", "]")
                .Trim('[', ']')
                .Split(',')
                .Select(byte.Parse).ToArray();

            return FromByteArray(parsed);
        }


        private static string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        private static void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }
    }
}
