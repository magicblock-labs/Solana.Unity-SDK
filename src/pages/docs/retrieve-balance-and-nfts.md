---
title: Events, Retrieve Balance and NFTs
description: Learn how to retrieve balance and observe NFT changes using Unity SDK.
---

The Unity SDK uses delegates to offer an event-based system that allows users to observe balance changes and NFTs.

You can register for events from anywhere. Here are some examples:


On Login:

```csharp
private void OnEnable()
{
    Web3.OnLogin += OnLogin;
}

private void OnDisable()
{
    Web3.OnLogin -= OnLogin;
}

private void OnLogin(Account account)
{
    Debug.Log(account.PublicKey);
}
```

---

On Balance Change:

```csharp
private void OnEnable()
{
    Web3.OnBalanceChange += OnBalanceChange;
}

private void OnDisable()
{
    Web3.OnBalanceChange -= OnBalanceChange;
}

private void OnBalanceChange(double solBalance)
{
    Debug.Log($"Balance changed to {solBalance}");
}
```

---

NFTs:

```csharp
private void OnEnable()
{
    Web3.OnNFTsUpdate += OnNFTsUpdate;
}

private void OnDisable()
{
    Web3.OnNFTsUpdate -= OnNFTsUpdate;
}

private void OnNFTsUpdate(List<Nft> nfts, int total)
{
    Debug.Log($"NFTs updated. Total: {total}");
}
```

---

