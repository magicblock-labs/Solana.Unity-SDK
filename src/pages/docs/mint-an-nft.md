---
title: Minting an NFT
description: 
---

For minting an NFT we will interact with the [Token Metadata](https://docs.metaplex.com/programs/token-metadata/) program, see the metaplex [documentation](https://docs.metaplex.com/) for a comprehensive overview.

Firstly, we need to create a new mint account for the NFT we want to mint and an associated token account for owning it.

```csharp
var mint = new Account();
var associatedTokenAccount = AssociatedTokenAccountProgram
    .DeriveAssociatedTokenAccount(Web3.Account, mint.PublicKey);
```

Secondly, let's define the metadata of the NFT.

```csharp
var metadata = new Metadata()
{ 
    name = "Test",
    symbol = "TST",
    uri = "https://y5fi7acw5f5r4gu6ixcsnxs6bhceujz4ijihcebjly3zv3lcoqkq.arweave.net/x0qPgFbpex4ankXFJt5eCcRKJzxCUHEQKV43mu1idBU",
    sellerFeeBasisPoints = 0,
    creators = new List<Creator> { new(Web3.Account.PublicKey, 100, true)}
};
```

We can now construct the transaction, whichi consists of 5 istructions: 
- Creating the Mint Account
- Initilizing the Mint Account
- Creattin the AssociatedTokenAccount
- Minting the NFT
- Creating the Metadata Account
- Creating the Master Edition

```csharp
var transaction = new TransactionBuilder()
    .SetRecentBlockHash(blockHash)
    .SetFeePayer(Web3.Account)
    .AddInstruction(
        SystemProgram.CreateAccount(
            Web3.Account,
            mint.PublicKey,
            minimumRent.Result,
            TokenProgram.MintAccountDataSize,
            TokenProgram.ProgramIdKey))
    .AddInstruction(
        TokenProgram.InitializeMint(
            mint.PublicKey,
            0,
            Web3.Account,
            Web3.Account))
    .AddInstruction(
        AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
            Web3.Account,
            Web3.Account,
            mint.PublicKey))
    .AddInstruction(
        TokenProgram.MintTo(
            mint.PublicKey,
            associatedTokenAccount,
            1,
            Web3.Account))
    .AddInstruction(MetadataProgram.CreateMetadataAccount(
        PDALookup.FindMetadataPDA(mint), 
        mint.PublicKey, 
        Web3.Account, 
        Web3.Account, 
        Web3.Account.PublicKey, 
        metadata,
        TokenStandard.NonFungible, 
        true, 
        true, 
        null,
        metadataVersion: MetadataVersion.V3))
    .AddInstruction(MetadataProgram.CreateMasterEdition(
        maxSupply: null,
        masterEditionKey: PDALookup.FindMasterEditionPDA(mint),
        mintKey: mint,
        updateAuthorityKey: Web3.Account,
        mintAuthority: Web3.Account,
        payer: Web3.Account,
        metadataKey: PDALookup.FindMetadataPDA(mint),
        version: CreateMasterEditionVersion.V3
    )
);
```

Finally, let's sign and send the transaction:

```csharp
var tx = Transaction.Deserialize(transaction.Build(new List<Account> {Web3.Account, mint}));
var res = await Web3.Wallet.SignAndSendTransaction(tx);
Debug.Log(res.Result);
```

The console will print the transaction signature, which should looks something like:

[https://explorer.solana.com/tx/TPSviDzpzTFEyfJkYwmQzqaPJTTsGMZTuPuG9q1LiKrhZnwg5WWHH7ARR8eYAdoB8rt8qcjKwqbcZj43b84Ls5C?cluster=devnet](https://explorer.solana.com/tx/TPSviDzpzTFEyfJkYwmQzqaPJTTsGMZTuPuG9q1LiKrhZnwg5WWHH7ARR8eYAdoB8rt8qcjKwqbcZj43b84Ls5C?cluster=devnet),

You can lookup the mint address in the explorer, which should be similar to this [NFT](https://explorer.solana.com/address/4X199VtLKVJUeLMXzwXzSsFgapVQcrYx9vnqxNDkH2Xa?cluster=devnet)

---

