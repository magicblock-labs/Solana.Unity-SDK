# Mobile Wallet Adapter — Quick Start

Get from zero to a connected wallet in a Unity Android app.

## Prerequisites

- Unity 2022.3 LTS or later (Unity 6 recommended)
- Android Build Support module installed via Unity Hub
- An MWA-compatible wallet on the target device ([Phantom](https://phantom.app/) or [Solflare](https://solflare.com/))
- An Android phone or Solana Seeker

## Install the SDK

Add the SDK via Unity Package Manager using a git URL:

1. Open **Window > Package Manager**
2. Click **+ > Add package from git URL**
3. Enter: `https://github.com/magicblock-labs/Solana.Unity-SDK.git`

For local development, clone the SDK and reference it in `Packages/manifest.json`:
```json
"com.solana.unity_sdk": "file:/path/to/your/Solana.Unity-SDK"
```

## Configure Android Build

1. **File > Build Settings** — switch platform to Android
2. **Player Settings > Other Settings**:
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64**

## Connect to a Wallet

```csharp
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;

public class WalletConnect : MonoBehaviour
{
    private SolanaMobileWalletAdapter _adapter;

    private void Start()
    {
        var options = new SolanaMobileWalletAdapterOptions
        {
            identityUri = "https://yourgame.com/",
            iconUri = "/icon.png",
            name = "My Game",
        };

        _adapter = new SolanaMobileWalletAdapter(
            options, RpcCluster.DevNet);
    }

    public async void OnConnectPressed()
    {
        var account = await _adapter.Login();
        Debug.Log($"Connected: {account.PublicKey}");
    }
}
```

`Login()` opens the wallet app for user approval on first call. On subsequent launches, it silently reconnects using the cached auth token — no wallet prompt needed.

## Next Steps

- [Method Reference](mwa-method-reference.md) — full API documentation
- [Cache Guide](mwa-cache-guide.md) — customize authorization token storage
- [Migration Guide](mwa-migration-v1-to-v2.md) — upgrading from the v1 API
- [Example App](https://github.com/Zurcusa/unity-solana-mwa-example) — full demo with all methods
