---
title: DEX integration with Orca
description:
---

Orca is natively supported in the SDK. Utility methods are provided to easily build the transactions needed to make swaps, open positions, manage cash and interact with Whirlpools.

# Orca

Orca is the easiest place to trade cryptocurrency on the Solana blockchain. For a detailed description refer to the official [Orca documentation](https://docs.orca.so/orca-for-traders/master)


---


## Perform a Swap

- Create an IDex istance, providing a default account and and the RPC client istance:

```csharp
IDex dex = new OrcaDex(
    WalletH.Account, 
    WalletH.Rpc
)
```

- Create an IDex istance:

```csharp
TokenData tokenA = await dex.GetTokenBySymbol("USDC");
TokenData tokenB = await dex.GetTokenBySymbol("ORCA");
```

- Find the whirlpool:

```csharp
PublicKey whirlpool = await dex.FindWhirlpoolAddress(tokenA.MintAddress, tokenB.MintAddress)
```

- Get a swap quote for 1 USDC:

```csharp
SwapQuote swapQuote = await dex.GetSwapQuoteFromWhirlpool(
    whirlpool, 
    1 * Math.Pow(10, tokenA.Decimals),
    tokenA.MintAddress,
    slippageTolerance: 0.1,
);
```

```csharp
var quote = (double)swapQuote.EstimatedAmountOut/Math.Pow(10, tokenB.Decimals);
Debug.Log(quote); // Amount of espected Orca token to receive
```

- Create the swap transaction:

```csharp
Transaction tx = await dex.SwapWithQuote(
    whirlpool,
    swapQuote
);
```

- Sign and send the swap transaction:

```csharp
await WalletH.Base.SignAndSendTransaction(tx);
```


---

## Open a position and increase the liquidity of the ORCA/USDC whirlpool

An example of adding 5 ORCA and 5 USDC to the liquidity of the pool, minting a metaplex NFT representing the position 

```csharp
OrcaDex dex = new OrcaDex(
    WalletH.Account, 
    WalletH.Rpc
);

var orcaToken = await dex.GetTokenBySymbol("ORCA");
var usdcToken = await dex.GetTokenBySymbol("USDC");

var whirlpool = await dex.FindWhirlpoolAddress(
  usdcToken.MintAddress, 
  orcaToken.MintAddress
);

Account mint = new Account();

Transaction tx = await dex.OpenPositionWithLiquidity(
    whirlpool,
    mint,
    -1792,
    1152,
    5*Math.Pow(10, tokenA.Decimals),
    5*Math.Pow(10, tokenB.Decimals),
    commitment: Commitment.Confirmed
);

var txSer = tx.Build(new List<Account>() {
  WalletH.Account, 
  mint
});

await WalletH.Base.SignAndSendTransaction(tx);
```



