<div align="center">
  <img height="170x" src="https://i.imgur.com/UvulxS0.png" />

  <h1>Solana.Unity SDK</h1>

  <p>
    <strong>Solana.Unity integration Framework</strong>
  </p>

  <p>
    <a href="https://developers.garbles.fun"><img alt="Tutorials" src="https://img.shields.io/badge/docs-tutorials-blueviolet" /></a>
    <a href="https://github.com/garbles-labs/Solana.Unity-SDK/issues"><img alt="Issues" src="https://img.shields.io/github/issues/garbles-labs/Solana.Unity-SDK?color=blueviolet" /></a>
    <a href="https://discord.gg/PDeRXyVURd"><img alt="Discord Chat" src="https://img.shields.io/discord/943797222162726962?color=blueviolet" /></a>
    <a href="https://opensource.org/licenses/MIT"><img alt="License" src="https://img.shields.io/github/license/garbles-labs/Solana.Unity-SDK?color=blueviolet" /></a>
  </p>
</div>
  
Solana.Unity-SDK uses [Solana.Unity-Core](https://github.com/garbles-labs/Solana.Unity-Core) implementation, native .NET Standard 2.0 (Unity compatible) with full RPC API coverage.

Solana.Unity-SDK started as a fork of [unity-solana-wallet](https://github.com/allartprotocol/unity-solana-wallet), but it has been detached due to the several changes we have made and upcoming pipeline of wallet integrations including SMS, Phantom and Web3auth support ...

## Documentation

[Solana.Unity SDK - Documentation](http://developers.garbles.fun/)

## Features
- Full JSON RPC API coverage
- Wallet and accounts: Set up of a non-custodial Solana wallet in Unity (sollet and solana-keygen compatible)
- Transaction decoding from base64 and wire format and encoding back into wire format
- Message decoding from base64 and wire format and encoding back into wire format
- Instruction decompilation 
- TokenWallet object to send and receive SPL tokens and JIT provisioning of Associated Token Accounts 
- Basic UI examples 

## Upcoming
- Wallet support for SMS, Phantom and Web3auth support. 
- Methods to trigger / register custom events to easily integrate custom logics (e.g.: server checks/updates or caching)

## Dependencies
- Solana.Unity.Wallet
- Solana.Unity.Rpc
- Soalana.Unity.KeyStore
- Soalana.Unity.Programs
- Newtonsoft.Json
- Chaos.NaCl.Standard
- Portable.BouncyCastle
- Zxing

## External packages
- Native File Picker
- Standalone File Browser

## Installation

* Open [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) window.
* Click the add **+** button in the status bar.
* The options for adding packages appear.
* Select Add package from git URL from the add menu. A text box and an Add button appear.
* Enter the `https://github.com/garbles-labs/Solana.Unity-SDK.git` Git URL in the text box and click Add.
* You may also install a specific package version by using the URL with the specified version.
  * `https://github.com/garbles-labs/Solana.Unity-SDK.git#X.Y.X`
  * Please note that the version `X.Y.Z` stated here is to be replaced with the version you would like to get.
  * You can find all the available releases [here](https://github.com/garbles-labs/Solana.Unity-SDK.git/releases).
  * The latest available release version is [![Last Release](https://img.shields.io/github/v/release/garbles-labs/Solana.Unity-SDK)](https://github.com/Sgarbles-labs/Solana.Unity-SDK/releases/latest)
* You will find a sample scene with a configured wallet in `Samples/Solana SDK/0.0.1/Simple Wallet/Solana Wallet/1.0.0/Simple Wallet/scenes/wallet_scene.unity`

## Step-by-step instructions
1. If you have an older version of Unity that doesn't have imported Newtonsoft.Json just import it.
2. After importing the wallet Unity will throw unity-plastic error. Just restart Unity.
3. Create a new scene.
4. Import WalletController prefab into your scene.
5. Set Client Source (Mainnet/Testnet/Devnet/Custom uri) and Storage Method (Json/Simple txt) on SimpleWallet script in WalletController prefab.
6. If you use custom URI be careful to use WS/WSS instead of HTTP/HTTPS because WebSocket does not work with HTTP / HTTPS.
7. To save mnemonics in JSON format, select the JSON storage method, and if you want to save it as a regular string, select Simple Txt.
8. If you want to use mnemonics saved in JSON format, you must deserialize it first. You have an example in ReGenerateAccountScreen.cs in the ResolveMnemonicsByType method.
9. Create new Canvas
10. Import WalletHolder prefab into the Canvas or if you want your design just import wallet prefab and customize the scene like we did with WalletHolder.

## Functionalities description
### Login Screen
- If you have already logged in to your wallet, then your mnemonics are stored and encrypted in memory with your password and you can log in with that password. Otherwise you have to create or restore a wallet.

### Create Wallet Screen
- You now have automatically generated mnemonics and to successfully create a wallet you must enter a password with which the mnemonics will be encrypted. I recommend that you use the Save Mnemonics option and save them to a text file. Then press create button to create a wallet.

### Regenerate Wallet Screen
- If you have saved mnemonics and want to recreate a wallet with it, load them by pressing Load Mnemonics button and generate the password again. Now your wallet is regenerated and the amount of SOL and NFT will be reloaded.

### Wallet Screen
- After you successfully logged in / generated / regenerated a wallet you will automatically be transferred to the wallet screen. Now you are shown SOL balance and your NFT's and you are automatically subscribed to the account via the websocket. This allows you to track changes in your account (automatic refreshing of SOL balance when a balance changes, etc..).

### Recieve Screen
- To facilitate testing, there is an Airdrop option in the Recieve section. Click on the Airdrop button, return to the Wallet Screen and wait a few seconds to see the change in SOL balance.

### Transfer Screen
- To complete the transaction enter the wallet pubkey and the amount you want to send. Then return to the wallet screen and wait a few seconds for the SOL Balance to refresh.

## Introduction to WalletBaseComponent.cs
- This class is located at Packages -> Solana Wallet -> Runtime -> codebase -> WalletBaseComponent.cs

### Create account
```C#
public async void CreateAccount(Account account, string toPublicKey = "", long ammount = 1000)
```
- First create keypair(private key and public key),
- Then create blockHash from activeRpcClient,
- Initialize transaction 
- Send transaction

### Start connection
```C#
public SolanaRpcClient StartConnection(EClientUrlSource clientUrlSource, string customUrl = "")
```
- For starting RPC connection call StartConnection and forward clientSource.
- Function returns new connected RPC client.
- Call example 
 ```C#
 StartConnection(clientSource);
 ```
  
### Generate wallet with mnemonics
```C#
 public Wallet GenerateWalletWithMenmonic(string mnemonics)
 ```
 - First check forwarded mnemonics validity.
 - Encrypt mnemonics with password
 - Create new wallet from mnemonics
 - Subscribe to WebSocket
 - Save mnemonics and encrypted mnemonics in memory
 - Call example
 ```C#
 SimpleWallet.instance.GenerateWalletWithMenmonic(_simpleWallet.LoadPlayerPrefs(_simpleWallet.MnemonicsKey));
 ```
### Login check with mnemonics and password
 ```C#
  public bool LoginCheckMnemonicAndPassword(string password)
 ```
 - Try to encrypt decrypted mnemonics with typed password.
 - Return true or false
 - Call example 
  ```C#
  private void LoginChecker()
  {
      if (_simpleWallet.LoginCheckMnemonicAndPassword(_passwordInputField.text))
      {
          SimpleWallet.instance.GenerateWalletWithMenmonic(_simpleWallet.LoadPlayerPrefs(_simpleWallet.MnemonicsKey));
          MainThreadDispatcher.Instance().Enqueue(() => { _simpleWallet.StartWebSocketConnection(); }); 
          manager.ShowScreen(this, "wallet_screen");
          this.gameObject.SetActive(false);
      }
      else
      {
          SwitchButtons("TryAgain");
      }
  }
 ```
 ### Get sol amount
  ```C#
  public async Task<double> GetSolAmmount(Account account)
 ```
 - Returns sol amount of forwarded account
 - Call example 
  ```C#
  double sol = await SimpleWallet.instance.GetSolAmmount(SimpleWallet.instance.wallet.GetAccount(0));
 ```
 ### Transfer sol
 ```C#
 public async Task<RequestResult<string>> TransferSol(string toPublicKey, long ammount = 10000000)
 ```
 - Executes sol transaction from one account to another one for forwarded amount.
 - Call example 
 ```C#
 private async void TransferSol()
 {
     RequestResult<string> result = await SimpleWallet.instance.TransferSol(toPublic_txt.text, long.Parse(ammount_txt.text));
     HandleResponse(result);
 }
 ```
 ### Transfer token
 ```C#
 public async Task<RequestResult<string>> TransferToken(string sourceTokenAccount, string toWalletAccount, Account sourceAccountOwner, string tokenMint, long ammount = 1)
 ```
 - Executes SOL transaction from one account to another one
 - Call example
 ```C#
 private async void TransferToken()
 {
     RequestResult<string> result = await SimpleWallet.instance.TransferToken(
                         transferTokenAccount.pubkey,
                         toPublic_txt.text,
                         SimpleWallet.instance.wallet.GetAccount(0),
                         transferTokenAccount.Account.Data.Parsed.Info.Mint,
                         long.Parse(ammount_txt.text));

     HandleResponse(result);
 }
```
### Request airdrop
```C#
public async Task<string> RequestAirdrop(Account account, ulong ammount = 1000000000)
```
- Send 1 sol to our wallet (this is for testing).
- Call example
```C#
airdrop_btn.onClick.AddListener(async () => {
            await SimpleWallet.instance.RequestAirdrop(SimpleWallet.instance.wallet.GetAccount(0));
        });
```
### Get owned token accounts
```C#
public async Task<TokenAccount[]> GetOwnedTokenAccounts(Account account)
```
- Returns array of tokens on the account
- Call example 
```C#
TokenAccount[] result = await SimpleWallet.instance.GetOwnedTokenAccounts(SimpleWallet.instance.wallet.GetAccount(0));
```
### Delete wallet and clear key
```C#
public void DeleteWalletAndClearKey()
```
- Unsubscribe from WebSocket events
- Delete used wallet

### Start WebSocket connection
```C#
public void StartWebSocketConnection()
```
- Starts WebSocket connection when user is logged in.

## Introduction to WebsocketService.cs
- This class is located at Packages -> Solana Wallet -> Runtime -> UnityWebSocket -> WebSocketService.cs
### Start connection
```C#
public void StartConnection(string address)
```
- For WebSocket to work we must first create a connection calling StartConnection from WebSocketService.cs and forward address
- In this function we create new WebSocket, then subscribe to events and open WebSocket connection.
- Call example
```C#
 webSocketService.StartConnection(GetWebsocketConnectionURL(clientSource));
```
### Subscribe to wallet account events
```C#
 public void SubscribeToWalletAccountEvents(string pubKey)
```
- To subscribe Account on WebSocket events call function SubscribeToWalletAccountEvents and forward Wallet Pub key
- First set subscriptionTypeReference to know which event we are processing (in this case it is accountSubscribe).
- Then call SendParameter and forward parameter for account subscription.
- Call example 
```C#
 webSocketService.SubscribeToWalletAccountEvents(wallet.Account.GetPublicKey);
```
### Unsubscribe to wallet account events
```C#
 public void UnSubscribeToWalletAccountEvents()
```
- To unsubscribe Account from WebSocket events call function UnsubscribeToWalletAccountEvents
- First set subscriptionTypeReference to know which event we are processing (in this case it is accountUnsubscribe).
- Then call SendParameter and forward parameter for account unsubscription.
- Call example 
 ```C#
 public void StartWebSocketConnection()
 {
     if (webSocketService.Socket != null) return;

     webSocketService.StartConnection(GetWebsocketConnectionURL(clientSource));
 }
```
### On Message
 ```C#
 private void OnMessage(object sender, MessageEventArgs e)
```
- To respond to websocket events we use WebSocket actions that we call in OnMessage function
- Depending on the SubscriptionTypeReference, we deserialize the message into a model.
- Invoke WebSocketAction
- Then subscribe the desired functionality to the action
```C#
 WebSocketActions.WebSocketAccountSubscriptionAction += CheckSubscription;
```

### Close connection
```C#
 public void CloseConnection()
 {
     if (_socket == null) return;

     _socket.CloseAsync();
 }
```
-To close WebSocket connection call CloseConnection from WebSocketService.cs

## Introduction to Nft.cs
- This class is located at Packages -> Solana Wallet -> Runtime -> codebase -> nft -> Nft.cs
### Try get nft data
```C#
public static async Task<Nft> TryGetNftData(string mint, SolanaRpcClient connection, bool tryUseLocalContent = true)
```
- Returns all data for one NFT and save file to persistance data path
- Call example 
```C#
Nft.Nft nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SimpleWallet.instance.activeRpcClient, true);
```
### Try load nft from local
```C#
public static Nft TryLoadNftFromLocal(string mint)
```
- Returns nft data from local machine if it exists.
- Call example
```C#
if (tryUseLocalContent)
{ 
    Nft nft = TryLoadNftFromLocal(mint);
    if (nft != null)
    {
        return nft;
    }
}
```
### Create address
```C#
public static Solana.Unity.Wallet.PublicKey CreateAddress(List<byte[]> seed, string programId)
```
- Create NFT's public key from seed and programId
- Call example
```C#
try
{
     seeds[3] = new[] { (byte)nonce };
     publicKey = CreateAddress(seeds, programId);
     return publicKey;
}
```

### Find program address
```C#
public static Solana.Unity.Wallet.PublicKey FindProgramAddress(string mintPublicKey, string programId = "metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s")
```
- Returns metaplex data pubkey from mint pubkey and programId
- Call example 
```C#
Solana.Unity.Wallet.PublicKey metaplexDataPubKey = FindProgramAddress(mint);
```
### Get metaplex Json data
```C#
public static async Task<T> GetMetaplexJsonData<T>(string jsonUrl)
```
- Returns metaplex json data from forwarded jsonUrl

### Resize
```C#
private static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
```
- Compress nft image to target height and width.
- Call example
```C#
Texture2D compressedTexture = Resize(texture, 75, 75);
```

## Contribution

Thank you for your interest in contributing to Solana.Unity-SDK!
Please see the [CONTRIBUTING.md](./CONTRIBUTING.md) to learn how or reach out on Discord.

### Thanks 💜

<div align="center">
  <a href="https://github.com/garbles-labs/Solana.Unity-SDK/graphs/contributors">
    <img src="https://contrib.rocks/image?repo=garbles-labs/Solana.Unity-SDK" width="100%" />
  </a>
</div>
