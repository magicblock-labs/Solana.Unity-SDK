using UnityEngine;

namespace Solana.Unity.SDK.Example
{
    public enum StorageMethod { JSON, SimpleTxt }
    
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class SimpleWallet : InGameWallet
    {
        public StorageMethod storageMethod;
        private const string StorageMethodStateKey = "StorageMethodKey";
        
        public static SimpleWallet Instance;

        public override void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ChangeState(storageMethod.ToString());
            if (PlayerPrefs.HasKey(StorageMethodStateKey))
            {
                string storageMethodString = LoadPlayerPrefs(StorageMethodStateKey);

                if(storageMethodString != storageMethod.ToString())
                {
                    storageMethodString = storageMethod.ToString();
                    ChangeState(storageMethodString);
                }

                if (storageMethodString == StorageMethod.JSON.ToString())
                    StorageMethodReference = StorageMethod.JSON;
                else if (storageMethodString == StorageMethod.SimpleTxt.ToString())
                    StorageMethodReference = StorageMethod.SimpleTxt;
            }
            else
                StorageMethodReference = StorageMethod.SimpleTxt;          
        }

        private void ChangeState(string state)
        {
            SavePlayerPrefs(StorageMethodStateKey, storageMethod.ToString());
        }

        public StorageMethod StorageMethodReference
        {
            get { return storageMethod; }
            set { storageMethod = value; ChangeState(storageMethod.ToString()); }
        }
        
        #region Data Functions

        private static void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }

        private static string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        #endregion

    }
}
