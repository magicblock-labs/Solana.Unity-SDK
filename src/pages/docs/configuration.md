---
title: Configurations
description: Learn how to configure your preferred wallet
---

The in-game wallet is the current default option, with support for Web3auth signup/login. We are adding support for more configurations. SMS support is in the pipeline and will be added soon. 


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

