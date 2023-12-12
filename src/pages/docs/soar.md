---
title: Soar (Solana On-Chain Achievements and Rankings)
description:
---

👑 Solana On-chain Achievements & Rankings 👑

# Soar

[SOAR](https://github.com/magicblock-labs/SOAR) is a program that provides a seamless solution for managing leaderboards, achievements, players' profiles and automatic rewards distribution on the Solana blockchain. Currently supporting invocation from a TypeScript client and with the [Solana.Unity-SDK](https://github.com/magicblock-labs/Solana.Unity-SDK). The program IDL is public and other clients can be easily auto-generated.
---


## Create a player profile

- Register a new player profile on Soar for a given player:

```csharp
var tx = new Transaction()
{
    FeePayer = Web3.Account,
    Instructions = new List<TransactionInstruction>(),
    RecentBlockHash = await Web3.BlockHash()
};

var playerAccount = SoarPda.PlayerPda(Web3.Account.PublicKey);
var accountsInitUser = new InitializePlayerAccounts()
{
    Payer = Web3.Account,
    User = Web3.Account,
    PlayerAccount = playerAccount,
    SystemProgram = SystemProgram.ProgramIdKey
};
var initPlayerIx = SoarProgram.InitializePlayer(
    accounts: accountsInitUser,
    username: "Peak",
    nftMeta: new PublicKey("BaxBPhbNxqR13QcYPvoTzE9LQZGs71Mu6euywyKHoprc"),
    SoarProgram.ProgramIdKey
);
tx.Add(initPlayerIx);
await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
```

Example of a transaction that creates a new profile on Soar: [tx](https://explorer.solana.com/address/BwL4xvhvDx6D1jtbYHGjJAFq6yeS8RbQDGLrVVZP2TzY)

---

## Register a new Game on Soar
 

```csharp
var tx = new Transaction()
{
    FeePayer = Web3.Account,
    Instructions = new List<TransactionInstruction>(),
    RecentBlockHash = await Web3.BlockHash()
};
var game = new Account();
var gameMeta = new GameAttributes()
{
    Title = "My Game",
    Description = "My Game Description",
    Genre = 0,
    GameType = 0,
    NftMeta = new PublicKey("8PyfKjB46ih1NHdNQLGhEGRDNRPTnFf94bwnQxa9Veux")
};

var initializeGameAccounts = new InitializeGameAccounts()
{
    Creator = Web3.Account,
    Game = game,
    SystemProgram = SystemProgram.ProgramIdKey
};
var initializeGameIx = SoarProgram.InitializeGame(
    accounts: initializeGameAccounts,
    gameMeta: gameMeta,
    gameAuth: new[] { Web3.Account.PublicKey }, // Add other authorities or PDAs which can sign for CPI
    SoarProgram.ProgramIdKey
);
tx.Add(initializeGameIx);

tx.PartialSign(Web3.Account);
tx.PartialSign(game);

var res = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
Debug.Log($"Tx initialize game: {res.Result}");
```

Example of a transaction that creates a new game on Soar: [tx](https://explorer.solana.com/tx/3QdndnvV21M2KRVFT4fBne5dfPW8c4JDbqLyYVqMvDJWuW9b2KafLYEjLiSk4iyZ7WBBrQo1UHYeZvNacQXkjB2a)

---

## Register a new Leaderboard on Soar


```csharp
var tx = new Transaction()
{
    FeePayer = Web3.Account,
    Instructions = new List<TransactionInstruction>(),
    RecentBlockHash = await Web3.BlockHash()
};
var game = new PublicKey("EFf4gsG44gaWUMd6HsEW7pvcRXXGfLxBC5mMeQD4RGDU");
var soarClient = new SoarClient(Web3.Rpc, Web3.WsRpc);
var gameAccount = (await soarClient.GetGameAsync(game)).ParsedResult;
var id = gameAccount.LeaderboardCount + 1;

var leaderboard = SoarPda.LeaderboardPda(game, id);
var topEntries = SoarPda.LeaderboardTopEntriesPda(leaderboard);
var leaderboardMeta = new RegisterLeaderBoardInput()
{
    Description = "A new leaderboard",
    NftMeta = new PublicKey("8PyfKjB46ih1NHdNQLGhEGRDNRPTnFf94bwnQxa9Veux"),
    ScoresToRetain = 5,
    ScoresOrder = false // ascending
};

var addLeaderboardAccounts = new AddLeaderboardAccounts()
{
    Authority = Web3.Account,
    Payer = Web3.Account,
    Game = game,
    Leaderboard = leaderboard,
    TopEntries = topEntries,
    SystemProgram = SystemProgram.ProgramIdKey
};
var createLeaderboardIx = SoarProgram.AddLeaderboard(
    accounts: addLeaderboardAccounts,
    input: leaderboardMeta,
    SoarProgram.ProgramIdKey
);
tx.Add(createLeaderboardIx);
var res = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
Debug.Log($"Create leaderboard: {res.Result}");
```

Example of a transaction that creates a new leaderboard on Soar: [tx](https://explorer.solana.com/tx/L9doNuZxc3wGKQ7fUSKKrvU4buWgUsHwUzyzmFxFLZEo9RXpSUQb4kqaG1747HgcjrUzNjkX9bijrjJXHHN8BPw)


## Submit a score to a leaderboard


```csharp
var tx = new Transaction()
{
    FeePayer = Web3.Account,
    Instructions = new List<TransactionInstruction>(),
    RecentBlockHash = await Web3.BlockHash()
};
var game = new PublicKey("EFf4gsG44gaWUMd6HsEW7pvcRXXGfLxBC5mMeQD4RGDU");
var leaderboard = new PublicKey("4nta83xKooFQDz6tLnJjkCTyuNiReBKBYYw5qXMp1me6");
var playerAccount = SoarPda.PlayerPda(Web3.Account);
var playerScores = SoarPda.PlayerScoresPda(playerAccount, leaderboard);

if (!await IsPdaInitialized(playerScores))
{
    var registerPlayerAccounts = new RegisterPlayerAccounts()
    {
        Payer = Web3.Account,
        User = Web3.Account,
        PlayerAccount = playerAccount,
        Game = game,
        Leaderboard = leaderboard,
        NewList = playerScores,
        SystemProgram = SystemProgram.ProgramIdKey
    };
    var registerPlayerIx = SoarProgram.RegisterPlayer(
        registerPlayerAccounts,
        SoarProgram.ProgramIdKey
    );
    tx.Add(registerPlayerIx);
}

var addLeaderboardAccounts = new SubmitScoreAccounts()
{
    Authority = Web3.Account,
    Payer = Web3.Account,
    PlayerAccount = playerAccount,
    Game = game,
    Leaderboard = leaderboard,
    PlayerScores = playerScores,
    TopEntries = SoarPda.LeaderboardTopEntriesPda(leaderboard),
    SystemProgram = SystemProgram.ProgramIdKey
};
var submitScoreIx = SoarProgram.SubmitScore(
    accounts: addLeaderboardAccounts,
    score: 10,
    SoarProgram.ProgramIdKey
);
tx.Add(submitScoreIx);
var res = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
Debug.Log($"Tx Score submission: {res.Result}");
```

where IsPdaInitialized is a helper function that checks if the playerScores account is initialized:

```csharp
private async UniTask<bool> IsPdaInitialized(PublicKey pda)
{
    var accountInfoAsync = await Web3.Rpc.GetAccountInfoAsync(pda);
    return accountInfoAsync.WasSuccessful && accountInfoAsync.Result?.Value != null;
}
```

Example of a transaction that submit a score to a leaderboard on Soar: [tx](https://explorer.solana.com/tx/3vawEW2kuj44GMHkHQu15aGbeWEafhcqNRyYPxR7yz4KhfGtUqZ8sprYE6zNruyJ9DNXi1ufiL4CdQYBsgkXStio)
