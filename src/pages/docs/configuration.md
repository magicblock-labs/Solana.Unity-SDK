---
title: Configurations
description: Learn how to configure your preferred wallet
---
Learn how to configure your preferred wallet

The SDK supports a veriety of wallets, including 
* In-game wallet (new or restored)
* In-game wallet with Web3auth authentication
* Phantom
* SMS (upcoming)

## Interface
`IWalletBase` defines the common [interface](https://github.com/garbles-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/IWalletBase.cs) 

The WalletBase abstract class implements `IWalletBase` interface and provides convenient methods shared by all wallet adapters. A few examples are:
* Connection to Mainnet/Devnet/Testnet or custom RPC
* Login/logout
* Account creation
* Get balance
* Get token accounts
* Sign/partially sign a transaction
* Send transaction

The complete list of methods is available [here](https://github.com/garbles-labs/Solana.Unity-SDK/blob/main/Runtime/codebase/WalletBase.cs)

---

