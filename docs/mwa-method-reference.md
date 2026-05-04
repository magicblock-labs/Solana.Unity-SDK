# Mobile Wallet Adapter — Method Reference

All public methods on `SolanaMobileWalletAdapter`. Methods marked with a lock require exclusive access — calling them while another operation is in flight throws `OperationInFlightException`.

## Configuration

```csharp
var options = new SolanaMobileWalletAdapterOptions
{
    identityUri = "https://yourgame.com/",
    iconUri = "/icon.png",
    name = "My Game",
    keepConnectionAlive = true,              // enable auth token caching (default: true)
    Cache = new PlayerPrefsAuthorizationCache(), // or your own IAuthorizationCache
    Verbosity = LogVerbosity.Default,        // Release | Default | Verbose
};

var adapter = new SolanaMobileWalletAdapter(options, RpcCluster.DevNet);
```

---

## Authentication

### Login

```csharp
Task<Account> Login(string password = null)
```

Authorize with the wallet. On first call, opens the wallet app for user approval. On subsequent calls, attempts silent reconnect using the cached auth token — if the cache is valid, no wallet prompt is shown.

Returns the connected `Account` with the wallet's public key.

### LoginWithSignIn

```csharp
Task<(Account Account, SignInResult SignInResult)> LoginWithSignIn(SignInPayload signInPayload)
```

Authorize and sign a proof message in a single wallet interaction (Sign-In With Solana). If the wallet doesn't natively support SIWS, the SDK falls back to a separate authorize + sign flow.

```csharp
var payload = new SignInPayload
{
    Domain = "yourgame.com",
    Statement = "Sign in to My Game",
};

var (account, signInResult) = await adapter.LoginWithSignIn(payload);
// signInResult.Signature contains the ed25519 proof signature
```

### Reconnect

```csharp
Task<ReconnectResult> Reconnect()
```

Restore a cached session without opening the wallet. Returns one of:

| Result | Meaning |
|---|---|
| `ReconnectResult.SilentSuccess` | Cached token rebound. `.Account` contains the wallet account. |
| `ReconnectResult.NoCachedSession` | No valid cache — call `Login()` instead. |
| `ReconnectResult.Failed` | Transport or wallet error. `.Error` contains the exception. |

### Disconnect

```csharp
Task Disconnect()
```

Clear local state: empties the authorization cache and clears the in-memory auth token. Does not contact the wallet. Fires `OnWalletDisconnected`.

### Deauthorize

```csharp
Task<DeauthorizeResult> Deauthorize()
```

Revoke the session at the wallet level and clear local state. Returns one of:

| Result | Meaning |
|---|---|
| `DeauthorizeResult.FullyRevoked` | Wallet acknowledged revocation and local cache cleared. |
| `DeauthorizeResult.LocalOnly` | Local cache cleared but wallet was unreachable. `.WalletPackage` identifies the wallet. |
| `DeauthorizeResult.Failed` | Cache clear itself failed. `.Error` contains the exception. |

### CloneAuthorization

```csharp
Task<string> CloneAuthorization()
```

Create a second independent auth token from the current session. Useful for parallel operations. Returns the new token as a string. Requires an active connection.

---

## Signing

### SignTransaction

```csharp
Task<Transaction> SignTransaction(Transaction transaction)
```

Sign a single transaction. Returns the signed transaction with the wallet's signature attached.

### SignAllTransactions

```csharp
Task<Transaction[]> SignAllTransactions(Transaction[] transactions)
```

Sign a batch of transactions. Returns all transactions with signatures.

### SignMessage

```csharp
Task<byte[]> SignMessage(byte[] message)
Task<byte[]> SignMessage(string message)
```

Sign an arbitrary message. Returns the raw 64-byte ed25519 signature. The string overload encodes as UTF-8.

### SignAndSendTransactions

```csharp
Task<SignAndSendTxResult> SignAndSendTransactions(
    Transaction[] transactions,
    SendOptions options = null)
```

Sign and broadcast transactions in a single wallet interaction. Returns one of:

| Result | Meaning |
|---|---|
| `SignAndSendTxResult.Success` | All signed and submitted. `.Signatures` contains one signature per transaction. |
| `SignAndSendTxResult.UserDenied` | User rejected the signing prompt. |
| `SignAndSendTxResult.InvalidPayloads` | One or more payloads rejected. `.Valid` is a per-transaction boolean array. |
| `SignAndSendTxResult.NotSubmitted` | Signed but RPC submission failed. `.PartialSignatures` available. |
| `SignAndSendTxResult.TooManyPayloads` | Batch exceeds wallet limit. `.MaxTransactionsPerRequest` contains the max. |
| `SignAndSendTxResult.ChainNotSupported` | Wallet doesn't support the requested chain. |
| `SignAndSendTxResult.AuthRevoked` | Auth token invalidated — reconnect and retry. |
| `SignAndSendTxResult.WalletUnreachable` | Transport or connection failure. |

`SendOptions` fields (all optional): `Commitment`, `SkipPreflight`, `MinContextSlot`, `MaxRetries`. If `MinContextSlot` is not set, the SDK auto-fetches the current block slot.

### SignAndSendTransaction

```csharp
Task<RequestResult<string>> SignAndSendTransaction(
    Transaction transaction,
    bool skipPreflight = false,
    Commitment commitment = Commitment.Confirmed)
```

Single-transaction convenience wrapper. Returns `RequestResult<string>` with the base64 signature on success or an error reason.

---

## Capabilities

### GetCapabilities

```csharp
Task<CapabilitiesResult> GetCapabilities()
```

Query the wallet's supported features and limits. Returns `CapabilitiesResult` with:

- `MaxTransactionsPerRequest`
- `MaxMessagesPerRequest`
- `SupportedTransactionVersions`
- `SupportsCloneAuthorization`
- `SupportsSignAndSendTransactions`
- `Features` (string array of feature identifiers)

---

## Events

| Event | Fired when |
|---|---|
| `OnWalletDisconnected` | `Disconnect()` or `Deauthorize()` completes |
| `OnWalletReconnected` | `Reconnect()` silently restores a session |

---

## Cross-Platform Wrapper

`SolanaWalletAdapter` is the cross-platform wrapper that routes to MWA on Android, WebGL adapter on web, and Phantom deep link on iOS. The following MWA-specific methods are available through it:

- `DisconnectWallet()` — delegates to `Disconnect()`
- `ReconnectWallet()` — delegates to `Reconnect()` via `Login()`
- `GetCapabilities()` — delegates to MWA `GetCapabilities()`

Other MWA-specific methods (`Deauthorize()`, `Reconnect()`, `LoginWithSignIn()`, `CloneAuthorization()`, `SignAndSendTransactions()`) are only available by casting to or directly using `SolanaMobileWalletAdapter`.
