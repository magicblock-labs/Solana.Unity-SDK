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

The WalletBase abstract class implements `IWalletBase` interface and provides convenient methods shared by all wallet adapters. 

A few examples are:

* Connection to Mainnet/Devnet/Testnet or custom RPC
* Login/logout
* Account creation
* Get balance
* Get token accounts
* Sign/partially sign transactions
* Send transactions

## Login example
You can attach the [Web3.cs](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/Web3.cs) script 
(../Runtime/Codebase/Web3.cs) to any game object on the scene, then call Web3.Instance.LoginWalletAdapter();

![Package manager](/Web3.png)

{% callout title="Additional methods" %} The complete list of methods is available [here](https://github.com/garbles-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/WalletBase.cs) {% /callout %} 


---
## Wallet Adapter
To configure a wallet following the [Wallet Adapter](https://solana-mobile.github.io/mobile-wallet-adapter/spec/spec.html) standard use the [SolanaWalletAdapter](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaWalletAdapter.cs) wallet implementation.

```csharp
WalletBase wallet = new SolanaWalletAdapter(walletAdapterOptions, RpcCluster.DevNet, ...);
``` 

## SMS

Solana Mobile Stack is a set of libraries for wallets and apps, allowing developers to create rich mobile experiences for the Solana network.
For more information about SMS check out the official [documentation](https://solanamobile.com/developers). 

## Mobile Wallet Adapter

To establish a wallet configuration in accordance with the Mobile Wallet Adapter standard, employ the [SolanaWalletAdapter](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaWalletAdapter.cs) implementation as demonstrated above. This adapter intelligently detects the target platform during development, seamlessly utilizing the appropriate underlying implementation. For example, when targeting WebGL, the [SolanaWalletAdapterWebGL](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaWalletAdapterWebGL/SolanaWalletAdapterWebGL.cs) is employed; likewise, when building for Android/iOS, the [SolanaMobileWalletAdapter](https://github.com/magicblock-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/SolanaMobileStack/SolanaMobileWalletAdapter.cs) is automatically chosen.


## Configuring Deeplinks

Some of the wallet on IOS, e.g. Phantom, are currently implemented using DeepLinks. Deep links are URLs that link to a specific piece of content or functionality within an app, in the context of Solana transactions, deep links can be used to sign a transaction by allowing users to approve a transaction using their Solana wallet.


### Enabling deep linking for IOS applications

See the detailed explanation on the Unity [documentation page](https://docs.unity3d.com/Manual/deep-linking-android.html) .

*the defined schema should match the value defined in the SolanaWalletAdapter wallet configuration* 




