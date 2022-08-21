using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace Solana.Unity.SDK
{
    public class Web3AuthWallet : WalletBase
    {
        private static bool hasPrivKey;
        private static string privKey;
        private static string ed25519PrivKey;
        
        public static void setPrivKeys(string privKeyParam, string ed25519PrivKeyParam)
        {
            hasPrivKey = true;
            (privKey, ed25519PrivKey) = (privKeyParam, ed25519PrivKeyParam);
            Debug.Log("privKey: " + privKey);
            Debug.Log("ed25519PrivKey: " + ed25519PrivKey);

        }

        protected override Task<Account> _Login(string password = null)
        {
            Web3Auth web3Auth = new();
            web3Auth.Awake();
            Web3AuthSample web3AuthSample = new Web3AuthSample();

            var loginConfigItem = new LoginConfigItem()
            {
                verifier = "your_verifierid_from_web3auth_dashboard",
                typeOfLogin = TypeOfLogin.GOOGLE,
                clientId = "BOKX2WS_5WVzZNu_NUCB141y6KLTPzHqC8A2M-Az0nlw6yW54pS4J8uHs5BVtVG4iDIiQSf2keAN9XPc_MA1Pq4"
            };
            web3Auth.setOptions(new Web3AuthOptions()
            {
                whiteLabel = new WhiteLabelData()
                {
                    name = "Web3Auth Sample App",
                    logoLight = null,
                    logoDark = null,
                    defaultLanguage = "en",
                    dark = true,
                    theme = new Dictionary<string, string>
                    {
                        { "primary", "#123456" }
                    }
                }
            });
            web3AuthSample.login(web3Auth);
            return Task.FromResult(new Account());
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            return Task.FromResult(new Account());
        }
        public override Task<Transaction> SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            return Task.FromResult(transaction);
        }
    }
}
