using Solana.Unity.SDK.Example;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiveScreen : SimpleScreen
{
    public Button airdrop_btn;
    public Button close_btn;

    public TextMeshProUGUI publicKey_txt;
    public RawImage qrCode_img;

    private void Start()
    {
        airdrop_btn.onClick.AddListener(async () => {
            await SimpleWallet.Instance.Wallet.RequestAirdrop();
        });

        close_btn?.onClick.AddListener(() =>
        {
            manager.ShowScreen(this, "wallet_screen");
        });
    }

    public override void ShowScreen(object data = null)
    {
        base.ShowScreen();
        gameObject.SetActive(true);

        CheckAndToggleAirdrop();

        GenerateQR();
        publicKey_txt.text = SimpleWallet.Instance.Wallet.Account.PublicKey;
    }

    private void CheckAndToggleAirdrop()
    {
        airdrop_btn.gameObject.SetActive(!SimpleWallet.Instance.Wallet.ActiveRpcClient.ToString().Contains("api.mainnet"));
    }

    private void GenerateQR()
    {
        Texture2D tex = QRGenerator.GenerateQRTexture(SimpleWallet.Instance.Wallet.Account.PublicKey, 256, 256);
        qrCode_img.texture = tex;
    }

    public override void HideScreen()
    {
        base.HideScreen();
        gameObject.SetActive(false);
    }
}