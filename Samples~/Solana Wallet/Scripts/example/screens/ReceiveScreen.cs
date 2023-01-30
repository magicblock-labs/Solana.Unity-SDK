using Solana.Unity.SDK;
using Solana.Unity.SDK.Example;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;

// ReSharper disable once CheckNamespace

public class ReceiveScreen : SimpleScreen
{
    public Button airdrop_btn;
    public Button close_btn;

    public TextMeshProUGUI publicKey_txt;
    public RawImage qrCode_img;

    private void Start()
    {
        airdrop_btn.onClick.AddListener(async () => {
            await WalletH.Instance.Wallet.RequestAirdrop();
        });

        close_btn.onClick.AddListener(() =>
        {
            manager.ShowScreen(this, "wallet_screen");
        });
    }
    
    private void OnEnable()
    {
        var isDevnet = WalletH.Instance.Wallet?.RpcCluster == RpcCluster.DevNet;
        airdrop_btn.enabled = isDevnet;
        airdrop_btn.interactable = isDevnet;
    }

    public override void ShowScreen(object data = null)
    {
        base.ShowScreen();
        gameObject.SetActive(true);

        CheckAndToggleAirdrop();

        GenerateQR();
        publicKey_txt.text = WalletH.Instance.Wallet.Account.PublicKey;
    }

    private void CheckAndToggleAirdrop()
    {
        airdrop_btn.gameObject.SetActive(!WalletH.Instance.Wallet.ActiveRpcClient.ToString().Contains("api.mainnet"));
    }

    private void GenerateQR()
    {
        Texture2D tex = QRGenerator.GenerateQRTexture(WalletH.Instance.Wallet.Account.PublicKey, 256, 256);
        qrCode_img.texture = tex;
    }

    public void CopyPublicKeyToClipboard()
    {
        Clipboard.Copy(WalletH.Instance.Wallet.Account.PublicKey.ToString());
        gameObject.GetComponent<Toast>()?.ShowToast("Public Key copied to clipboard", 3);
    }

    public override void HideScreen()
    {
        base.HideScreen();
        gameObject.SetActive(false);
    }
    
    public void OnClose()
    {
        var wallet = GameObject.Find("wallet");
        wallet.SetActive(false);
    }
}