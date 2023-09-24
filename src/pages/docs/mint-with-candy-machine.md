---
title: Minting an NFT with a Candy Machine
description: 
---

The Metaplex Protocol Candy Machine is the leading minting and distribution program for fair NFT collection launches on Solana. Much like its name suggests, you can think of a Candy Machine as a temporary structure which is first loaded by creators and then unloaded by buyers. It allows creators to bring their digital assets on-chain in a secure and customisable way.

[Read the full documentation](https://docs.metaplex.com/programs/candy-machine/overview)

![Editor Tool Location](https://docs.metaplex.com/assets/candy-machine-v3/CandyMachine.png)

## Candy Machine vs Normal Minting: 

Advantages:

- The candy machine is the perfect tool if you want to mint NFTs without an authoritative server, it allows you to create configurable flows and allows to:
    - Accept payments in SOL, NFTs or any Solana token.
    - Restrict your launch via start/end dates, mint limits, third party signers, etc.
    - Protect your launch against bots via configurable bot taxes and gatekeepers like Captchas.
    - Restrict minting to specific NFT/Token holders or to a curated list of wallets.
    - Create multiple minting groups with different sets of rules.
    - Reveal your NFTs after the launch whilst allowing your users to verify that information.
    - And many more feautures!

Disadvantage:

- The candy machine is not the right tool if you don't know in advance the number of NFTs in the collection or if you need dynamic minting.

For minting an NFT we will interact with the [Token Metadata](https://docs.metaplex.com/programs/token-metadata/) program, see the metaplex [documentation](https://docs.metaplex.com/) for a comprehensive overview.

## How to Create a Candy Machine:

- With the Unity Editor, follow [this tutorial](/docs/candy-machine)
- With the Sugar CLI, follow the [official documentation](https://docs.metaplex.com/programs/candy-machine/how-to-guides/my-first-candy-machine-part1)

## How to mint from a Candy Machine:

The following example assumes you created a Candy Machine, and uses the [sol-payment](https://docs.metaplex.com/programs/candy-machine/available-guards/sol-payment) guard for minting.

----

Firstly, we need to create a new mint account for the NFT we want to mint and an associated token account for owning it.

```csharp
var mint = new Account();
var associatedTokenAccount = AssociatedTokenAccountProgram
    .DeriveAssociatedTokenAccount(Web3.Account, mint.PublicKey);
```

In this examples, we will uses the following CM and guard:

```csharp
var candyMachineKey = new PublicKey("5SQCxpkvhAPwahXmMg53PQHND3JbEZk9WEfq3jceuUY3");
var candyGuardKey = new PublicKey("5FrkJp9jnArgYv1p9S4tsSUMuHCCBrGu8HM3JNKGV5bM");
```

Secondly, let's retrieve the necessary information.

```csharp
var candyMachineClient = new CandyMachineClient(Web3.Rpc, null, CandyMachineCommands.CandyMachineProgramId);
var stateRequest = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
var candyMachine = stateRequest.ParsedResult;
var candyMachineCreator = CandyMachineCommands.GetCandyMachineCreator(candyMachineKey);
var collectionNft = await Nft.TryGetNftData(candyMachine.CollectionMint, Web3.Rpc);
```

We can now construct the transaction: 

```csharp
var mintNftAccounts = new Solana.Unity.Metaplex.CandyGuard.Program.MintV2Accounts {
    CandyMachineAuthorityPda = candyMachineCreator,
    Payer = Web3.Account,
    Minter = Web3.Account,
    CandyMachine = candyMachineKey,
    NftMetadata = PDALookup.FindMetadataPDA(mint),
    NftMasterEdition = PDALookup.FindMasterEditionPDA(mint),
    SystemProgram = SystemProgram.ProgramIdKey,
    TokenMetadataProgram = MetadataProgram.ProgramIdKey,
    SplTokenProgram = TokenProgram.ProgramIdKey,
    CollectionDelegateRecord = PDALookup.FindDelegateRecordPDA(
        candyMachine.Authority, 
        candyMachine.CollectionMint, 
        candyMachineCreator, 
        MetadataDelegateRole.Collection
    ),
    CollectionMasterEdition = PDALookup.FindMasterEditionPDA(candyMachine.CollectionMint),
    CollectionMetadata = PDALookup.FindMetadataPDA(candyMachine.CollectionMint),
    CollectionMint = candyMachine.CollectionMint,
    CollectionUpdateAuthority = collectionNft.metaplexData.data.updateAuthority,
    NftMint = mint,
    NftMintAuthority = Web3.Account,
    Token = associatedTokenAccount,
    TokenRecord = PDALookup.FindTokenRecordPDA(associatedTokenAccount, mint),
    SplAtaProgram = AssociatedTokenAccountProgram.ProgramIdKey,
    SysvarInstructions = SysVars.InstructionAccount,
    RecentSlothashes = new PublicKey("SysvarS1otHashes111111111111111111111111111"),
    CandyGuard = candyGuardKey,
    CandyMachineProgram = CandyMachineCommands.CandyMachineProgramId
};

// Use the sol payment Guard
CandyGuardMintSettings mintSettings = new CandyGuardMintSettings()
{
    SolPayment = new CandyGuardMintSettings.SolPaymentMintSettings()
    {
        Destination = candyMachine.Authority
    },
};

// Build the Transaction
var mintSettingsAccounts = mintSettings.GetMintArgs(Web3.Account, mint, candyMachineKey, candyGuardKey);
var computeInstruction = ComputeBudgetProgram.SetComputeUnitLimit(800_000);
var candyMachineInstruction = CandyGuardProgram.MintV2(
    mintNftAccounts,
    Array.Empty<byte>(),
    null,
    CandyMachineCommands.CandyGuardProgramId
);

candyMachineInstruction = new TransactionInstruction {
    Data = candyMachineInstruction.Data,
    ProgramId = candyMachineInstruction.ProgramId,
    Keys = candyMachineInstruction.Keys.Select(k => {
        if (k.PublicKey == mint.PublicKey) {
            return AccountMeta.Writable(mint, true);
        }
        return k;
    }).Concat(mintSettingsAccounts).ToList()
};

var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
var transaction = new TransactionBuilder()
    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
    .SetFeePayer(Web3.Account)
    .AddInstruction(computeInstruction)
    .AddInstruction(candyMachineInstruction)
    .Build(new List<Account> { Web3.Account, mint });
var tx = Transaction.Deserialize(transaction);
```

Finally, let's sign and send the transaction:

```csharp
var res = await Web3.Wallet.SignAndSendTransaction(tx);
Debug.Log(res.Result);
```

The console will print the transaction signature, which you can investigate in the inspector and should looks similar to this [transaction](https://explorer.solana.com/tx/5V8PcL1Av16P8L1wxdcMnM2sp3wQmvbBHYbZcksksCbNWPJmaXxvx4QVAvwmy1VsL7mJMSUUcBdEeUHS387xuYtA?cluster=devnet),

You can lookup the mint address in the explorer, which should be similar to this [NFT](https://explorer.solana.com/address/6FdPEzdwLRbA9V8uGuDGfih8qviT4CoRjZGKmT7eC7uj?cluster=devnet)

---

