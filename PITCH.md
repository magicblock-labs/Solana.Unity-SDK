# 🎮 Unity Mobile Wallet Adapter SDK – API Parity & UX Improvements

> **Applicant:** Pratik Kale  
> **GitHub:** https://github.com/Pratikkale26/Solana.Unity-SDK  
> **Proposal for:** Solana Seeker – Unity Mobile Wallet Adapter SDK RFP  

---

## 🧠 TL;DR

This proposal delivers full API parity for the Unity Mobile Wallet Adapter SDK, introduces a persistent authorization cache layer, and improves wallet lifecycle UX (connect, disconnect, reconnect).

The core SDK implementation is already completed and submitted as a Draft PR, significantly reducing execution risk. This grant focuses on productionizing the work through documentation, example apps, and final integration.

---

## 🔴 Problem

The current Mobile Wallet Adapter (MWA) implementation in the Unity SDK lacks key functionality already available in the React Native SDK.

As a result, Unity-based Solana mobile games suffer from poor wallet UX:

- Missing `deauthorize` and `get_capabilities` APIs  
- No persistent auth token storage (tokens are lost on app restart)  
- Users must re-approve wallet connections every session  
- No clean disconnect or silent reconnect flow  

This leads to:
- Friction in player onboarding  
- Poor user retention  
- Inconsistent developer experience compared to React Native  

---

## ✅ Solution

This proposal introduces a complete upgrade to the Unity MWA stack, aligning it with React Native capabilities while improving developer ergonomics.

### 1. API Parity

Added missing methods:
- `deauthorize` (revoke wallet session)
- `get_capabilities` (wallet feature detection)

Implemented in:
- `IAdapterOperations`
- `MobileWalletAdapterClient`

---

### 2. Extensible Auth Cache Layer

Introduced a pluggable caching system:

```text
IMwaAuthCache
├── PlayerPrefsAuthCache   (default)
└── EncryptedAuthCache     (extensible template)
```

Developers can inject custom secure storage:

```csharp
var wallet = new SolanaWalletAdapter(options, authCache: new MySecureCache());
```

---

### 3. Seamless Reconnect UX

* `Login()` attempts silent `reauthorize` using cached token
* Falls back to full authorization only if needed

👉 Eliminates repeated wallet approval popups

---

### 4. Clean Disconnect Flow

```csharp
await wallet.DisconnectWallet();   // revokes token + clears cache
await wallet.ReconnectWallet();    // silent or full auth

wallet.OnWalletDisconnected += () => ShowConnectScreen();
wallet.OnWalletReconnected  += () => RestoreGameState();

await wallet.GetCapabilities();
```

---

## 🔬 Proof of Work

The full implementation is already completed and submitted as a Draft PR:

👉 [https://github.com/magicblock-labs/Solana.Unity-SDK/pull/264](https://github.com/magicblock-labs/Solana.Unity-SDK/pull/264)

This includes:

* API parity implementation
* Auth cache system
* Wallet lifecycle improvements
* Unit test coverage for cache layer

This significantly reduces delivery risk and ensures the grant funds are used for production readiness rather than initial development.

---

## 🛠 Scope of Work (Grant Deliverables)

The grant will fund the completion and productionization of this work:

| Deliverable                                                             | Timeline |
| ----------------------------------------------------------------------- | -------- |
| 📄 SDK Documentation (installation, API reference, cache customization) | Week 1–3 |
| 📱 Open-source Unity Android Example App (demonstrating full MWA flow)  | Week 4–5 |
| 🔧 PR review cycles, fixes, and upstream merge support                  | Ongoing  |

---

## 💰 Budget — $6,500 USD (in SKR)

| Item                                                           | Cost       |
| -------------------------------------------------------------- | ---------- |
| Core SDK implementation (API parity, auth cache, lifecycle UX) | $3,500     |
| PR review rounds + integration polish                          | $800       |
| Full SDK documentation                                         | $1,200     |
| Open-source Example Android App                                | $700       |
| Community support + post-merge maintenance                     | $800       |
| Buffer / contingency                                           | $500       |
| **Total**                                                      | **$6,500** |

---

## 🚀 Impact

This upgrade improves the developer experience for Unity game developers building on Solana Mobile by:

* Enabling persistent wallet sessions across app restarts
* Reducing friction in player onboarding
* Providing API parity with the React Native SDK
* Making wallet lifecycle management predictable and production-ready

By aligning Unity tooling with Solana Mobile standards, this work helps unlock broader adoption of Solana in mobile gaming.

---

## 👤 About Me

**Pratik Kale**
Full-Stack Developer & Solana Builder

* GitHub: [https://github.com/Pratikkale26](https://github.com/Pratikkale26)
* Twitter: [https://x.com/PratikKale26](https://x.com/PratikKale26)

I focus on building developer tooling and infrastructure in the Solana ecosystem, with an emphasis on improving developer experience and real-world usability.
