# Mobile Wallet Adapter — Authorization Cache Guide

The SDK caches the wallet's auth token so users don't have to re-approve on every app launch. This guide covers the default cache, how to replace it, and how the SDK validates cached sessions.

## Default: PlayerPrefsAuthorizationCache

Out of the box, the SDK uses `PlayerPrefsAuthorizationCache` which stores the auth token as JSON in Unity's `PlayerPrefs`. No setup needed — it works automatically when `keepConnectionAlive = true` (the default).

```csharp
var options = new SolanaMobileWalletAdapterOptions
{
    keepConnectionAlive = true, // default — enables caching
};
```

### Scoped Keys

If your app manages multiple wallet identities, you can scope each cache to a separate PlayerPrefs key:

```csharp
var cacheA = new PlayerPrefsAuthorizationCache("wallet-a");
var cacheB = new PlayerPrefsAuthorizationCache("wallet-b");
```

Default key: `SolanaUnity.MWA.AuthorizationRecord.v1`
Scoped key: `SolanaUnity.MWA.AuthorizationRecord.v1.wallet-a`

## Custom Cache

Implement `IAuthorizationCache` to store the auth token wherever you want — encrypted storage, a database, or in-memory for testing:

```csharp
public interface IAuthorizationCache
{
    Task<AuthorizationRecord> GetAsync();
    Task SetAsync(AuthorizationRecord record);
    Task ClearAsync();
}
```

Inject it via options:

```csharp
var options = new SolanaMobileWalletAdapterOptions
{
    Cache = new MySecureCache(),
};
```

### Example: Custom-Key PlayerPrefs Cache

From the [demo app](https://github.com/Zurcusa/unity-solana-mwa-example):

```csharp
public class DemoAuthorizationCache : IAuthorizationCache
{
    private const string Key = "SolanaDemo.MWA.Auth";

    public Task<AuthorizationRecord> GetAsync()
    {
        string json = PlayerPrefs.GetString(Key, null);
        if (string.IsNullOrEmpty(json))
            return Task.FromResult<AuthorizationRecord>(null);
        try
        {
            return Task.FromResult(
                JsonConvert.DeserializeObject<AuthorizationRecord>(json));
        }
        catch
        {
            return Task.FromResult<AuthorizationRecord>(null);
        }
    }

    public Task SetAsync(AuthorizationRecord record)
    {
        if (record == null) return Task.CompletedTask;
        PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(record));
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }
}
```

## Cache Validation

The SDK validates cached records before using them. A cached session is cleared automatically when:

1. **Schema version mismatch** — the cached record's `SchemaVersion` doesn't match the SDK's `ExpectedSchemaVersion` (currently 2). This handles upgrades where the record format changes.

2. **Chain mismatch** — the cached record's `Chain` (e.g. `solana:devnet`) doesn't match the adapter's current `RpcCluster`. Prevents a devnet token from being sent to a mainnet wallet.

3. **Wallet rejection** — the wallet reports `auth_token_rejected` during a reconnect attempt. The SDK clears the cache and falls through to a fresh authorization.

When any of these occur, `Login()` transparently falls through to a fresh wallet authorization — no error is surfaced to the user.

## AuthorizationRecord Fields

The cached record contains:

| Field | Purpose |
|---|---|
| `SchemaVersion` | Format version for upgrade detection |
| `AuthToken` | Bearer token for subsequent wallet calls |
| `AccountAddress` | Cached public key (base58) |
| `AccountLabel` | Wallet-provided account name |
| `Chain` | CAIP-2 chain identifier at time of authorization |
| `CachedAtUnixSeconds` | Timestamp for diagnostics (not used for expiry) |
| `Chains`, `Features` | Wallet-scoped metadata |
| `WalletUriBase`, `WalletIcon`, `AccountIcon` | Wallet branding metadata |
