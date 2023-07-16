---
title: DEX integration with Orca
description:
---

Orca is natively supported in the SDK. Utility methods are provided to easily build the transactions needed to make swaps, open positions, manage cash and interact with Whirlpools.

# Orca

Orca is the easiest place to trade cryptocurrency on the Solana blockchain. For a detailed description refer to the official [Orca documentation](https://docs.orca.so/orca-for-traders/master) and [Orca Developer Portal](https://orca-so.gitbook.io/orca-developer-portal/orca/welcome).


---


## Perform a Swap

- Create an IDex istance, providing a default account and and the RPC client istance:

```csharp
IDex dex = new OrcaDex(
    Web3.Account, 
    Web3.Rpc
)
```

- Create an IDex istance:

```csharp
TokenData tokenA = await dex.GetTokenBySymbol("USDC");
TokenData tokenB = await dex.GetTokenBySymbol("ORCA");
```

- Find the whirlpool:

```csharp
Pool whirlpool = await dex.FindWhirlpoolAddress(tokenA.MintAddress, tokenB.MintAddress)
```

- Get a swap quote for 1 USDC:

```csharp
SwapQuote swapQuote = await dex.GetSwapQuoteFromWhirlpool(
    whirlpool.Address, 
    DecimalUtil.ToUlong(1, tokenA.Decimals),
    tokenA.MintAddress,
    slippageTolerance: 0.1,
);
```

```csharp
var quote = DecimalUtil.FromBigInteger(swapQuote.EstimatedAmountOut, tokenB.Decimals);
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
await Webs.Wallet.SignAndSendTransaction(tx);
```


---

## Open a position and increase the liquidity of the ORCA/USDC whirlpool

An example of adding 5 ORCA and 5 USDC to the liquidity of the pool, minting a metaplex NFT representing the position 

```csharp
OrcaDex dex = new OrcaDex(
    Web3.Account, 
    Web3.Rpc
);

var orcaToken = await dex.GetTokenBySymbol("ORCA");
var usdcToken = await dex.GetTokenBySymbol("USDC");

Debug.Log($"Token A: {orcaToken}");
Debug.Log($"Token A: {usdcToken}");

var whirlpool = await dex.FindWhirlpoolAddress(
    usdcToken.MintAddress, 
    orcaToken.MintAddress
);

Debug.Log($"Whirlpool: {whirlpool.Address}");

Account mint = new Account();

Transaction tx = await dex.OpenPositionWithLiquidity(
    whirlpool.Address,
    mint,
    -3712,
    -256,
    DecimalUtil.ToUlong(0.5, orcaToken.Decimals),
    DecimalUtil.ToUlong(0.9, usdcToken.Decimals),
    slippageTolerance: 0.3,
    withMetadata: true,
    commitment: Commitment.Confirmed
);

tx.PartialSign(Web3.Account);
tx.PartialSign(mint);

var res = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
Debug.Log(res.Result);
```



