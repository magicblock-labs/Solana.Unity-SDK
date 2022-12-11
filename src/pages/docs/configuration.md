---
title: Configurations
description: Learn how to configure your preferred wallet
---
Learn how to configure your preferred wallet

The SDK supports a veriety of wallets, including 
* In-game wallet
* In-game wallet with Web3auth authentication
* Phantom and SMS (upcoming). 

Interface
IWalletBase defines the common interface

The WalletBase abstract class implements IWalletBase interface and provides convenient methods shared by all wallet adapters. A few examples are:
Connection to Mainnet/Devnet/Testnet or custom RPC
Login/logout
Account creation
Get balance
Get token accounts
Sign/partially sign a transaction
Send transaction
The complete list of methods is available here:

---

