---
title: Configurations
description: Learn how to configure your preferred wallet
---
Learn how to configure your preferred wallet

The SDK supports a veriety of wallets, including 

| Wallet      | Support | Type     |
| :---        |    :----:   |          ---: |
| In-game (new or restore)      | ✅       | In-app   |
| In-game (Web3auth)      | ✅       | In-app   |
| Wallet Adapter        | ✅       | External   |
| Mobile Wallet Adapter      | ✅       | External  |
| Seed Vault      | 🏗       | In-app   |


## Interface
`IWalletBase` defines the common [interface](https://github.com/garbles-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/IWalletBase.cs) 

The WalletBase abstract class implements `IWalletBase` interface and provides convenient methods shared by all wallet adapters. A few examples are:

* Connection to Mainnet/Devnet/Testnet or custom RPC
* Login/logout
* Account creation
* Get balance
* Get token accounts
* Sign/partially sign transactions
* Send transactions


{% callout title="Additional methods" %} The complete list of methods is available [here](https://github.com/garbles-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/WalletBase.cs) {% /callout %} 

---
## Wallet Adapter
To configure a wallet following the [Wallet Adapter](https://solana-mobile.github.io/mobile-wallet-adapter/spec/spec.html) standard use the [SolanaWalletAdapterWebGL](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaWalletAdapterWebGL.cs) wallet implementation.

```csharp
WalletBase wallet = new SolanaWalletAdapterWebGL(walletAdapterOptions, RpcCluster.DevNet, ...);
``` 


## SMS

Solana Mobile Stack is a set of libraries for wallets and apps, allowing developers to create rich mobile experiences for the Solana network.
For more information about SMS check out the official [documentation](https://solanamobile.com/developers). 

## Mobile Wallet Adapter
To configure a wallet following the Mobile Wallet Adapter standard use the [SolanaMobileWalletAdapter](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaMobileWalletAdapter.cs) wallet implementation.

```csharp
WalletBase wallet = new SolanaMobileWalletAdapter(solanaMobileWalletOptions, RpcCluster.DevNet, ...);
``` 

## Configuring Deeplinks

Some of the wallet, e.g. Phantom, are currently implemented using DeepLinks. Deep links are URLs that link to a specific piece of content or functionality within an app, in the context of Solana transactions, deep links can be used to sign a transaction by allowing users to approve a transaction using their Solana wallet.

### Enabling deep linking for Android applications

To enable deep linking for Android applications, use an [intent filter](https://developer.android.com/guide/components/intents-filters). An intent filter overrides the standard Android App [Manifest](https://docs.unity3d.com/Manual/android-manifest.html) to include a specific intent filter section for [Activity](https://developer.android.com/reference/android/app/Activity). 

To set up the wallet intent filter:

1. In the Project window, go to Assets > Plugins > Android.
2. Create a new file and call it AndroidManifest.xml. Unity automatically processes this file when you build your application.
3. Copy the [code sample](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Samples~/Solana%20Wallet/Plugins/Android/AndroidManifest.xml) into the new file and save it.

*android:scheme="unitydl" should match the value defined in the wallet configuration* 

See the detailed explanation on the Unity [documentation page](https://docs.unity3d.com/Manual/deep-linking-android.html).

### Enabling deep linking for IOS applications

See the detailed explanation on the Unity [documentation page](https://docs.unity3d.com/Manual/deep-linking-android.html) .

*the defined schema should match the value defined in the wallet configuration* 




