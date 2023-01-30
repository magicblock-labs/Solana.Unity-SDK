using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

public class WalletHolder : MonoBehaviour
{
    public Button toggleWallet_btn;
    public Button subscriptionImage;
    public TextMeshProUGUI subscription_txt;
    public GameObject wallet;
    void Start()
    {
        wallet.SetActive(false);
        toggleWallet_btn.onClick.AddListener(() => {
            wallet.SetActive(!wallet.activeSelf);
        });
    }
}
