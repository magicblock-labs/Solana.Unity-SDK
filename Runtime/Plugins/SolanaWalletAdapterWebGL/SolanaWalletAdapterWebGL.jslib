mergeInto(LibraryManager.library, {
  InitWalletAdapter: async function (callback, rpcClusterPtr) {
    const isXnft = Boolean(
      "xnft" in window &&
        window.xnft != undefined &&
        window.xnft.solana != undefined &&
        window.xnft.solana.publicKey != undefined
    );
    window.rpcCluster = UTF8ToString(rpcClusterPtr);
    // Add UnityWalletAdapter from CDN
    if (window.walletAdapterLib == undefined) {
      var script = document.createElement("script");
      script.src =
        "https://cdn.jsdelivr.net/npm/@magicblock-labs/unity-wallet-adapter@1.2.1";
      document.head.appendChild(script);
      script.onload = function () {
        {{{ makeDynCall('vi', 'callback') }}}(isXnft);
      };
    } else {
      window.walletAdapterLib.refreshWalletAdapters();
      {{{ makeDynCall('vi', 'callback') }}}(isXnft);
    }
  },
  ExternGetWallets: async function (callback) {
    try {
      const wallets = await window.walletAdapterLib.getWallets();
      var bufferSize = lengthBytesUTF8(wallets) + 1;
      var walletsPtr = _malloc(bufferSize);
      stringToUTF8(wallets, walletsPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(walletsPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternConnectWallet: async function (walletNamePtr, callback) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var pubKey;
      if (walletName === "XNFT") {
        pubKey = window.xnft.solana.publicKey.toString();
      } else {
        pubKey = await window.walletAdapterLib.connectWallet(walletName);
      }
      if (pubKey == undefined) {
        throw new Error("Unable to connect to: " + walletName);
      }
      var bufferSize = lengthBytesUTF8(pubKey) + 1;
      var pubKeyPtr = _malloc(bufferSize);
      stringToUTF8(pubKey, pubKeyPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(pubKeyPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignTransactionWallet: async function (
    walletNamePtr,
    transactionPtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64transaction = UTF8ToString(transactionPtr);
      let signedTransaction;
      if (walletName === "XNFT") {
        const transaction =
          window.walletAdapterLib.getTransactionFromStr(base64transaction);
        signedTransaction = await window.xnft.solana.signTransaction(
          transaction
        );
      } else {
        signedTransaction = await window.walletAdapterLib.signTransaction(
          walletName,
          base64transaction
        );
      }
      let txStr = Buffer.from(signedTransaction.serialize()).toString("base64");
      var bufferSize = lengthBytesUTF8(txStr) + 1;
      var txPtr = _malloc(bufferSize);
      stringToUTF8(txStr, txPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(txPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignMessageWallet: async function (
    walletNamePtr,
    messagePtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64Message = UTF8ToString(messagePtr);
      let signatureStr;
      if (walletName === "XNFT") {
        const messageBytes = Uint8Array.from(atob(base64Message), (c) =>
          c.charCodeAt(0)
        );
        var signedMessage = await window.xnft.solana.signMessage(
          messageBytes
        );
        if (typeof signedMessage === 'object' && signedMessage !== null && 'signature' in signedMessage) {
            signedMessage = signedMessage.signature;
        }
        signatureStr = btoa(String.fromCharCode(...signedMessage));
      } else {
        var signature = await window.walletAdapterLib.signMessage(
          walletName,
          atob(base64Message)
        );
        if(signature instanceof Uint8Array) {
          signatureStr = btoa(String.fromCharCode(...signature));
        } else {
          signatureStr = signature.toString("base64");
        }
      }
      var bufferSize = lengthBytesUTF8(signatureStr) + 1;
      var signaturePtr = _malloc(bufferSize);
      stringToUTF8(signatureStr, signaturePtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(signaturePtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignAllTransactionsWallet: async function (
    walletNamePtr,
    transactionsPtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64transactionsStr = UTF8ToString(transactionsPtr);
      var base64transactions = base64transactionsStr.split(",");
      let signedTransactions;
      if (walletName === "XNFT") {
        let transactions = [];
        for (var i = 0; i < base64transactions.length; i++) {
          const transaction = window.walletAdapterLib.getTransactionFromStr(
            base64transactions[i]
          );
          transactions.push(transaction);
        }
        signedTransactions = await window.xnft.solana.signAllTransactions(
          transactions
        );
      } else {
        signedTransactions = await window.walletAdapterLib.signAllTransactions(
          walletName,
          base64transactions
        );
      }
      var serializedSignedTransactions = [];
      for (var i = 0; i < signedTransactions.length; i++) {
        var signedTransaction = signedTransactions[i];
        var txStr = Buffer.from(signedTransaction.serialize()).toString("base64");
        serializedSignedTransactions.push(txStr);
      }
      var txsStr = serializedSignedTransactions.join(",");
      var bufferSize = lengthBytesUTF8(txsStr) + 1;
      var txsPtr = _malloc(bufferSize);
      stringToUTF8(txsStr, txsPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(txsPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSubscribeWalletEvents: function (walletNamePtr, callback) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      window.unityWalletEventUnsubs = window.unityWalletEventUnsubs || {};

      if (window.unityWalletEventUnsubs[walletName]) {
        window.unityWalletEventUnsubs[walletName]();
        delete window.unityWalletEventUnsubs[walletName];
      }

      const toPkString = (v) => {
        try {
          if (!v) return null;
          if (typeof v === "string") return v;
          if (Array.isArray(v) && v.length > 0) {
            const first = v[0];
            if (!first) return null;
            return typeof first === "string" ? first : (first.toString ? first.toString() : null);
          }
          return v.toString ? v.toString() : null;
        } catch (_) {
          return null;
        }
      };

      const emit = (evt, data) => {
        const payload = {
          event: evt,
          walletName: walletName,
          publicKey: data && data.publicKey ? toPkString(data.publicKey) : toPkString(data),
          account: data && data.account ? toPkString(data.account) : null,
          accounts: data && data.accounts ? data.accounts.map(toPkString).filter(Boolean) : null,
          error: data && data.message ? String(data.message) : null,
        };
        const json = JSON.stringify(payload);
        const ptr = _malloc(lengthBytesUTF8(json) + 1);
        stringToUTF8(json, ptr, lengthBytesUTF8(json) + 1);
        {{{ makeDynCall('vi', 'callback') }}}(ptr);
      };

      const tryResolveProvider = () => {
        if (walletName === "XNFT" && window.xnft && window.xnft.solana) {
          return window.xnft.solana;
        }

        if (window.walletAdapterLib) {
          if (typeof window.walletAdapterLib.getWalletAdapter === "function") {
            const a = window.walletAdapterLib.getWalletAdapter(walletName);
            if (a) return a;
          }
          if (typeof window.walletAdapterLib.getWalletByName === "function") {
            const w = window.walletAdapterLib.getWalletByName(walletName);
            if (w && w.adapter) return w.adapter;
            if (w) return w;
          }
          const pools = [window.walletAdapterLib.walletAdapters, window.walletAdapterLib.wallets];
          for (const pool of pools) {
            if (!Array.isArray(pool)) continue;
            for (const item of pool) {
              if (!item) continue;
              if (item.name === walletName) return item.adapter || item;
              if (item.adapter && item.adapter.name === walletName) return item.adapter;
              if (item.wallet && item.wallet.name === walletName) return item.wallet.adapter || item.wallet;
            }
          }
        }

        if (window.solana && walletName.toLowerCase().includes("phantom")) {
          return window.solana;
        }

        return null;
      };

      const provider = tryResolveProvider();
      if (!provider || typeof provider.on !== "function") {
        return;
      }

      const subscriptions = [];
      const on = (evt, fn) => {
        try {
          provider.on(evt, fn);
          subscriptions.push([evt, fn]);
        } catch (_) {}
      };

      on("accountsChanged", (accounts) => emit("accountsChanged", { accounts }));
      on("accountChanged", (account) => emit("accountChanged", { account }));
      on("disconnect", (info) => emit("disconnect", info));
      on("connect", (info) => emit("connect", info));
      on("change", (info) => emit("change", info));

      window.unityWalletEventUnsubs[walletName] = () => {
        if (typeof provider.off === "function") {
          subscriptions.forEach(([evt, fn]) => {
            try {
              provider.off(evt, fn);
            } catch (_) {}
          });
        }
      };
    } catch (err) {
      console.error(err && err.message ? err.message : err);
    }
  },

  ExternUnsubscribeWalletEvents: function (walletNamePtr) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      if (!window.unityWalletEventUnsubs || !window.unityWalletEventUnsubs[walletName]) {
        return;
      }
      window.unityWalletEventUnsubs[walletName]();
      delete window.unityWalletEventUnsubs[walletName];
    } catch (err) {
      console.error(err && err.message ? err.message : err);
    }
  },
});

