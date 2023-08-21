---
title: Introduction
pageTitle: Solana.Unity-SDK - Overview
description: Open-Source Unity-Solana SDK with NFT support & Full RPC coverage.
---

Open-Source Unity-Solana SDK with NFT support & Full RPC coverage. {% .lead %}

{% link-grid %}

{% link-grid-link title="Installation" icon="installation" href="/docs/installation" description="Step-by-step guides to setting up your system and installing the Solana.Unity-SDK." /%}

{% link-grid-link title="Wallet Configuration" icon="presets" href="/docs/configuration" description="Learn how to set up your game wallets." /%}

{% link-grid-link title="Core concepts" icon="plugins" href="/docs/associated-token-account" description="Solana.Unity-SDK core concepts." /%}

{% link-grid-link title="Guides" icon="theming" href="/docs/xnft" description="Guides to help you get started." /%}

{% /link-grid %}

---

## Solana.Unity-SDK

Solana.Unity SDK is comprehensive set of open-source tools to easily access Solana in your Unity-based games.
You can install the SDK with the Unity Package Manager and set up your preferred wallet among the available options.
Solana.Unity-SDK uses [Solana.Unity-Core](https://github.com/garbles-labs/Solana.Unity-Core) implementation, native .NET Standard 2.0 (Unity compatible) with full RPC API coverage, MPL, native DEXes operations and more.
The project started as a fork of [unity-solana-wallet](https://github.com/allartprotocol/unity-solana-wallet), but it has been detached due to the several changes we made and the upcoming pipeline of wallet integrations, including [SMS](https://github.com/solana-mobile/solana-mobile-stack-sdk) and Raindrops. 

## The SDK supports:
- Full JSON RPC API coverage
- Wallet and accounts: Set up of a non-custodial Solana wallet in Unity (sollet and solana-keygen compatible)
- Phantom and Web3auth support (non-custodial signup/login through social accounts)
- Transaction decoding from base64 and wire format and encoding back into wire format
- Message decoding from base64 and wire format and encoding back into wire format
- Instruction decompilation
- TokenWallet object to send and receive SPL tokens and JIT provisioning of Associated Token Accounts
- Basic UI examples
- NFTs
- Compile games to xNFTs (Backpack)
- Native DEX operations (Orca, Jupiter, ...)

## Upcoming:
- Wallet Seed Vault.
- Methods to trigger / register custom events to easily integrate custom logics (e.g.: server checks/updates or caching)
- Raindrops
