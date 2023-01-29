---
title: Staking
description: 
---


```csharp
public class CreateAccountAndInitializeStakeExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
           "clerk shoe noise umbrella apple gold alien swap desert rubber truck okay twenty fiscal near talent drastic present leg put balcony leader access glimpse";

        public void Run()
        {
            var wallet = new Wallet.Wallet(new Mnemonic(MnemonicWords));
            rpcClient.RequestAirdrop(wallet.Account.PublicKey, 100_000_000);
            RequestResult<ResponseValue<BlockHash>> blockHash = rpcClient.GetRecentBlockHash();
            ulong minbalanceforexception = rpcClient.GetMinimumBalanceForRentExemption(StakeProgram.StakeAccountDataSize).Result;
            Account fromAccount = wallet.Account;
            Account stakeAccount = wallet.GetAccount(22);

            Authorized authorized = new Authorized()
            {
                Staker = fromAccount,
                Withdrawer = fromAccount
            };
            Lockup lockup = new Lockup()
            {
                Custodian = fromAccount.PublicKey,
                Epoch = 0,
                UnixTimestamp = 0
            };

            Console.WriteLine($"BlockHash >> {blockHash.Result.Value.Blockhash}");

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(SystemProgram.CreateAccount(
                    fromAccount.PublicKey,
                    stakeAccount,
                    minbalanceforexception + 42,
                    StakeProgram.StakeAccountDataSize,
                    StakeProgram.ProgramIdKey))
                .AddInstruction(StakeProgram.Initialize(
                    stakeAccount.PublicKey,
                    authorized,
                    lockup))
                .Build(new List<Account> { fromAccount, stakeAccount });
            Console.WriteLine($"Tx base64: {Convert.ToBase64String(tx)}");
            RequestResult<ResponseValue<SimulationLogs>> txSim = rpcClient.SimulateTransaction(tx);

            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);
            RequestResult<string> firstSig = rpcClient.SendTransaction(tx);
            Console.WriteLine($"First Tx Result: {firstSig.Result}");
        }
    }
```

---

