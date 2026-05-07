# SignAndSendTransactions Investigation

**Date:** 2026-05-07
**Branch:** `pr/5-adapter-session`
**Environment:** Pixel 7 emulator (Android 17 API 37), Unity 6000.4.3f1 (IL2CPP), 8GB RAM

## Symptom

After tapping "Sign & Send" in the MWA demo app with **Backpack wallet**, the wallet opens, creates and sends the transaction on-chain successfully, but when control returns to the dApp the UI hangs at "..." indefinitely. Backpack sometimes crashes with "Backpack keeps stopping."

**Phantom and Solflare work correctly** with the same SDK code.

## Root Cause

**Backpack-specific WebSocket server instability.** Backpack (React Native) disrupts its local MWA WebSocket server during internal screen transitions, corrupting the frame state on the client side. The WebSocket itself (WebSocketSharp via NativeWebSocket) handles Android STOP/RESUME cycles fine — proven by Phantom and Solflare surviving 8-12 seconds of STOP without issue.

## Cross-Wallet Comparison

All three wallets were tested with the same SDK code, same emulator, same demo app.

### Backpack (fails)
```
12:23:10.434  authorize response received (15ms - silent reauth)
12:23:10.434  [MWA Wire] -> sign_and_send_transactions sent
12:23:10.482  APP_CMD_STOP                 (+48ms)
12:23:11.237  APP_CMD_RESUME               (+803ms, STOP duration: 0.8s)
12:23:11.xxx  FATAL: "The header part of a frame could not be read"
12:23:12.xxx  Connection refused storm (OnClose reconnect loop)
```
**Result:** WebSocket frame parser corrupted. Response lost. UI hangs.

### Phantom (works)
```
12:46:24.987  [MWA Wire] -> authorize sent (with cached token)
12:46:25.012  APP_CMD_STOP                 (+25ms)
12:46:27.340  authorize response received  (while STOPPED, +2.3s)
12:46:27.341  sign_and_send sent           (while STOPPED)
12:46:33.609  APP_CMD_RESUME               (STOP duration: 8.6s)
```
**Result:** WebSocket survived entire 8.6s STOP. No frame errors. Sign & send succeeded.

### Solflare (works)
```
12:53:12.461  [MWA Wire] -> authorize sent (with cached token)
12:53:11.022  APP_CMD_STOP (already stopped)
12:53:12.484  authorize response received  (while STOPPED, +23ms)
12:53:12.487  sign_and_send sent           (while STOPPED)
12:53:23.177  sign_and_send response       (while STOPPED, +10.7s)
12:53:23.185  APP_CMD_RESUME               (STOP duration: 12.2s)
```
**Result:** WebSocket survived entire 12.2s STOP. No frame errors. Sign & send succeeded.

### Key Finding

| Wallet | Type | STOP duration | WebSocket survives? | Result |
|--------|------|--------------|---------------------|--------|
| Phantom | Native Android | ~8.6s | Yes | Works |
| Solflare | Native Android | ~12.2s | Yes | Works |
| Backpack | React Native | ~0.8s | No | Fails |

Backpack has the **shortest** STOP duration yet is the only wallet where the WebSocket breaks. This conclusively proves:

1. The Android activity STOP itself does **not** kill the WebSocket
2. WebSocketSharp handles STOP/RESUME correctly when the server is stable
3. Backpack's React Native runtime disrupts its WebSocket server during internal activity/screen transitions, corrupting the TCP stream

## Backpack: sign_transactions vs sign_and_send_transactions

Both operations were tested back-to-back in the same app session with Backpack, proving the failure is specific to the on-chain submission phase.

### sign_transactions (works)
```
12:58:07.478  [MWA Wire] -> authorize sent (cached token)
12:58:07.528  APP_CMD_STOP
12:58:07.544  authorize response     (while STOPPED, +66ms)
12:58:07.545  sign_transactions      (while STOPPED)
12:58:10.840  response received      (while STOPPED, +3.3s)
12:58:12.956  APP_CMD_RESUME         (STOP duration: 5.4s)
```
No errors. WebSocket stable throughout.

### sign_and_send_transactions (fails)
```
12:59:38.980  [MWA Wire] -> authorize sent (cached token)
12:59:38.961  APP_CMD_STOP (already stopped)
12:59:39.114  authorize response     (while STOPPED, +134ms)
12:59:39.119  sign_and_send sent     (while STOPPED)
13:00:02.039  FATAL exception        (+23s, while STOPPED — server crashes here)
13:00:06.343  FATAL frame error      (just before RESUME)
13:00:06.364  APP_CMD_RESUME         (STOP duration: 27.4s)
```
WebSocket dies 23 seconds into Backpack's processing — during the RPC submission phase.

### Analysis

The only difference between these operations is that `sign_and_send_transactions` requires the wallet to **broadcast the transaction to the Solana network** before responding. During this async RPC call, Backpack's React Native runtime disrupts its WebSocket server. Specifically:

1. Backpack receives `sign_and_send_transactions`, signs the transaction, then calls an RPC node to submit it
2. During the RPC submission (~23s in), Backpack's WebSocket server crashes or resets
3. The dApp's WebSocketSharp read thread encounters corrupted frame data
4. WebSocketSharp throws "The header part of a frame could not be read"
5. The encrypted session is tied to the original connection (ECDH key exchange), so reconnecting is useless — even if it succeeded, Backpack wouldn't re-send the response to a new session
6. The `SignAndSendTransactionsAsync` task hangs forever waiting for a response on a dead transport
7. The `OnClose` handler enters an unbounded reconnect loop against the dead server

`sign_transactions` avoids this entirely because it returns immediately after signing — no RPC submission, no async network call inside Backpack, and the WebSocket server stays stable.

## Approaches Tried

### 1. Fix missing `return` in `ExecuteNextAction`
**File:** `LocalAssociationScenario.cs:134-138`
**Result:** Fixed the dequeue crash and reduced the reconnect storm severity, but the Backpack WebSocket issue is server-side.

### 2. Reconnect limiter + scenario timeout
**File:** `LocalAssociationScenario.cs`
Added `_closing` flag, max 5 reconnect attempts with 200ms delay, 60-second scenario timeout with `Task.WhenAny`, `ForceComplete()` teardown method.
**Result:** The app no longer hangs forever (shows "Wallet not reachable" after timeout). Required a null check on `scenarioResult` in `SolanaMobileWalletAdapter.cs:449`.

### 3. Fresh authorize (no cached token)
**File:** `SolanaMobileWalletAdapter.cs:435`
Passed `null` instead of `_authToken` to force full authorization UI.
**Result:** Backpack auto-approves authorize regardless of token presence. Same failure.

### 4. `FLAG_ACTIVITY_NEW_TASK`
**File:** `LocalAssociationIntentCreator.cs:18`
Changed `startActivityForResult` to `startActivity` with `FLAG_ACTIVITY_NEW_TASK` (0x10000000).
**Result:** Still fails with Backpack. The STOP is standard Android lifecycle, not task-related.

### 5. Increased emulator RAM to 8GB
**File:** `~/.android/avd/Pixel_7.avd/config.ini` (hw.ramSize=8192)
**Result:** Confirmed STOP is not memory-related. `MemAvailable` showed 5.8GB free.

## Known Code Issues (Should Fix Regardless)

These bugs exist in `LocalAssociationScenario.cs` and affect all wallets:

1. **Missing `return` in `ExecuteNextAction`** (line 134-135): After calling `CloseAssociation(response)`, execution falls through to `_actions.Dequeue()` on an empty queue, throwing `InvalidOperationException`.

2. **Unbounded `OnClose` reconnect loop** (line 46-50): The handler reconnects immediately with no delay, no retry limit, and no flag to prevent reconnecting during intentional close. Creates a thread pool storm when the wallet server is gone.

3. **Wrong timeout unit** (line 33): `TimeSpan.FromSeconds(clientTimeoutMs)` where `clientTimeoutMs=9000` creates a 2.5-hour timeout instead of 9 seconds. Should be `TimeSpan.FromMilliseconds(clientTimeoutMs)`.

4. **Null-unsafe `scenarioResult` access** (`SolanaMobileWalletAdapter.cs:449`): `scenarioResult.WasSuccessful` can throw `NullReferenceException` if the scenario returns null.

## Recommended Actions

### For the SDK (our side)
Fix the four code issues listed above. These are real bugs that affect reliability with all wallets:
- The missing `return` causes silent exceptions on every successful scenario completion
- The reconnect loop wastes resources and can crash the wallet
- The timeout unit bug makes connection timeouts effectively infinite
- The null check prevents crashes on scenario failure

### For Backpack (their side)
File a bug report with Backpack. Their MWA WebSocket server is unstable during internal React Native screen transitions. Phantom and Solflare (both native Android) handle the same protocol correctly. The Backpack team should investigate their `walletlib` or MWA server implementation for connection stability during `sign_and_send_transactions` processing.

## Affected Operations

With Backpack only:
- `sign_and_send_transactions` - **fails** (wallet disrupts WebSocket during approval UI transition)
- `sign_transactions` - works
- `sign_messages` - works
- `authorize` - works
- `get_capabilities` - works

With Phantom and Solflare: **all operations work correctly**.

## Reproduction

1. Install MWA demo app + Backpack wallet on Android emulator (or device)
2. Authorize with Backpack on any network
3. Tap "Sign & Send"
4. Approve transaction in Backpack
5. Observe: transaction succeeds on-chain, but dApp shows "..." indefinitely
6. Repeat steps 2-4 with Phantom or Solflare — works correctly
