---
title: DEX integration with Jupiter
description:
---

Jupiter is natively supported in the SDK. Utility methods are provided to easily get swap quotes, build and send the transactions needed to perform swaps.

# Jupiter

Jupiter is the key liquidity aggregator for Solana, offering the widest range of tokens and best route discovery between any token pair. For a detailed description refer to the official [Jupiter documentation](https://station.jup.ag/).


---


## Perform a Swap

- Create an IDex istance, providing a default account:

```csharp
IDexAggregator dex = new JupiterDexAg(Web3.Account);
```

- Create an IDex istance:

```csharp
TokenData tokenA = await dex.GetTokenBySymbol("SOL");
TokenData tokenB = await dex.GetTokenBySymbol("USDC");
```


- Get a swap quote for 1 SOL:

```csharp
SwapQuoteAg swapQuote = await dex.GetSwapQuote(
    tokenA.MintAddress,
    tokenB.MintAddress,
    DecimalUtil.ToUlong(1, tokenA.Decimals)
);
```

```csharp
var quote = DecimalUtil.FromBigInteger(swapQuote.OutputAmount, tokenB.Decimals);
Debug.Log(quote); // Amount of espected USDC token to receive
```

- Display the route path:

```csharp
Debug.Log(string.Join(" -> ", swapQuote.RoutePlan.Select(p => p.SwapInfo.Label)));

// Lifinity V2 -> Whirlpool
```

- Create the swap transaction:

```csharp
Transaction tx = await dex.Swap(swapQuote);
```

- Sign and send the swap transaction:

```csharp
await Web3.Wallet.SignAndSendTransaction(tx);
```


## Use the Jupiter Payments API

The Jupiter Payments API is also available, enabling you to utilize Jupiter + SolanaPay for facilitating user payments with any SPL token, allowing pricing in USDC or other tokens.

- Create an IDex istance, providing a default account:

```csharp
IDexAggregator dex = new JupiterDexAg(Web3.Account);
```

- Create an IDex istance:

```csharp
TokenData tokenA = await dex.GetTokenBySymbol("SOL");
TokenData tokenB = await dex.GetTokenBySymbol("USDC");
```


- Get a swap quote for the amount of SOL needed for obtaining 5 UDSC:

```csharp
SwapQuoteAg swapQuote = await dex.GetSwapQuote(
    tokenA.MintAddress,
    tokenB.MintAddress,
    DecimalUtil.ToUlong(5, tokenB.Decimals),
    SwapMode.ExactOut
);
```

```csharp
var quote = DecimalUtil.FromBigInteger(swapQuote.InputAmount, tokenA.Decimals);
Debug.Log(quote); // Amount of espected SOL token to pay
```

- Create the swap transaction:

```csharp
Transaction tx = await dex.Swap(swapQuote);
```

- Sign and send the swap transaction:

```csharp
await Web3.Wallet.SignAndSendTransaction(tx);
```
