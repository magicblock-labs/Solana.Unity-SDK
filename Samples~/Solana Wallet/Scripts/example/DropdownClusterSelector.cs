using Solana.Unity.SDK;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace

public class DropdownClusterSelector : MonoBehaviour
{
    void OnEnable()
    {
        int rpcDefault = PlayerPrefs.GetInt("rpcCluster", 0);
        RpcNodeDropdownSelected(rpcDefault);
        Web3.OnWalletInstance += () => RpcNodeDropdownSelected(rpcDefault);
        GetComponent<TMP_Dropdown>().value = rpcDefault;
    }
    
    public void RpcNodeDropdownSelected(int value)
    {
        if(Web3.Instance == null) return;
        Web3.Instance.rpcCluster = (RpcCluster) value;
        Web3.Instance.customRpc = value switch
        {
            (int) RpcCluster.MainNet => "https://rpc.magicblock.gg/solana-mainnet/",
            (int) RpcCluster.TestNet => "https://rpc.magicblock.gg/solana-testnet/",
            _ => "https://rpc.magicblock.gg/solana-devnet/"
        };
        Web3.Instance.webSocketsRpc = value switch
        {
            (int) RpcCluster.MainNet => "wss://red-boldest-uranium.solana-mainnet.quiknode.pro/190d71a30ba3170f66df5e49c8c88870737cd5ce/",
            (int) RpcCluster.TestNet => "wss://polished-omniscient-pond.solana-testnet.quiknode.pro/05d6e963dcc26cb1969f8c8e304dc49ed53324d9/",
            _ => "wss://late-wild-film.solana-devnet.quiknode.pro/8374da8d09b67ce47c9307c1863212e5710b7c69/"
        };
        PlayerPrefs.SetInt("rpcCluster", value);
        PlayerPrefs.Save();
        Web3.Instance.LoginXNFT().AsUniTask().Forget();
    }
}
