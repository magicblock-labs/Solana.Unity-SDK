---
title: Introduction
pageTitle: Solana.Unity-SDK - Introduction
description: Open-Source Unity-Solana SDK with NFT support & Full RPC coverage.
---

Open-Source Unity-Solana SDK with NFT support & Full RPC coverage. {% .lead %}

{% link-grid %}

{% link-grid-link title="Installation" icon="installation" href="/docs/installation" description="Step-by-step guides to setting up your system and installing the Solana.Unity-SDK." /%}

{% link-grid-link title="Wallet Configuration" icon="presets" href="/docs/configuration" description="Learn how to configure your preferred wallets." /%}

{% link-grid-link title="Core concepts" icon="plugins" href="/docs/associated-token-account" description="Solana.Unity-SDK core concepts." /%}

{% link-grid-link title="Guides" icon="theming" href="/docs/xnft" description="Guides to help you get started." /%}

{% /link-grid %}

---

## Solana.Unity-SDK

Here you'll find the documentation you need to get up and running with the Solana.Unity SDK and start leveraging Solana within your Unity-based games.
Solana.Unity-SDK uses [Solana.Unity-Core](https://github.com/garbles-labs/Solana.Unity-Core) implementation, native .NET Standard 2.0 (Unity compatible) with full RPC API coverage. You can install the SDK with the Unity Package Manager to configure your preferred wallet among the available options.
The project started as a fork of [unity-solana-wallet](https://github.com/allartprotocol/unity-solana-wallet), but it has been detached due to the several changes we made and the upcoming pipeline of wallet integrations, including [SMS](https://github.com/solana-mobile/solana-mobile-stack-sdk), Phantom and Web3auth support ...

## The SDK supports:
- Full JSON RPC API coverage
- Wallet and accounts: Set up of a non-custodial Solana wallet in Unity (sollet and solana-keygen compatible)
- Web3auth support (non-custodial signup/login through social accounts)
- Transaction decoding from base64 and wire format and encoding back into wire format
- Message decoding from base64 and wire format and encoding back into wire format
- Instruction decompilation
- TokenWallet object to send and receive SPL tokens and JIT provisioning of Associated Token Accounts

## Upcoming:
- Wallet support for SMS and Seed Vault.
- Methods to trigger / register custom events to easily integrate custom logics (e.g.: server checks/updates or caching)
- Raindrops
