# Mobile Wallet Adapter — Migration from v1 to v2

## Logout → Disconnect / Deauthorize

`Logout()` is deprecated. Replace it with one of two methods depending on intent:

| Old | New | What it does |
|---|---|---|
| `Logout()` | `Disconnect()` | Clears local cache and auth token. The wallet-side session remains valid — next `Login()` can silently reconnect. |
| `Logout()` | `Deauthorize()` | Revokes the session at the wallet AND clears local state. Next `Login()` requires a fresh wallet approval. |

```csharp
// Before
adapter.Logout();

// After — if you want silent reconnect next time
await adapter.Disconnect();

// After — if you want to fully revoke access
var result = await adapter.Deauthorize();
```

## Authorization Cache

v1 stored auth tokens in bare PlayerPrefs keys (`pk`, `authToken`). v2 introduces `IAuthorizationCache` with structured `AuthorizationRecord` storage.

The migration is automatic — on first launch after upgrading, the SDK reads legacy keys and migrates them to the new cache format. No action needed.

To customize storage, inject your own `IAuthorizationCache`:

```csharp
var options = new SolanaMobileWalletAdapterOptions
{
    Cache = new MyCustomCache(),
};
```

See the [Cache Guide](mwa-cache-guide.md) for details.

## New Methods

These methods are new in v2 and have no v1 equivalent:

| Method | Purpose |
|---|---|
| `Reconnect()` | Explicitly restore a cached session (returns typed result) |
| `Deauthorize()` | Revoke wallet-side authorization (returns typed result) |
| `SignAndSendTransactions()` | Sign + broadcast in one wallet interaction (returns typed result) |
| `GetCapabilities()` | Query wallet feature support |
| `LoginWithSignIn()` | Authorize + SIWS proof in one step |
| `CloneAuthorization()` | Create a second auth token for parallel operations |
| `SignMessage(string)` | Convenience overload (v1 only had byte[]) |

## Authorize / Reauthorize → AuthorizeAsync

The low-level client methods `Authorize()` and `Reauthorize()` are replaced by `AuthorizeAsync()` with CAIP-2 chain identifiers instead of cluster strings. Most apps use `Login()` and `Reconnect()` instead of calling these directly.
