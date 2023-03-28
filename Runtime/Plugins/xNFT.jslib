mergeInto(LibraryManager.library, {
  ExternConnectXNFT: async function (callback) {
    if ("xnft" in window && window.xnft != undefined && window.xnft.solana != undefined && window.xnft.solana.publicKey != undefined) {
      try {
        var pubKey = window.xnft.solana.publicKey.toString();
        var lenPubKey = lengthBytesUTF8(pubKey) + 1;
        var strPtr = _malloc(lenPubKey);
        stringToUTF8(pubKey, strPtr, lenPubKey);
        Module.dynCall_vi(callback, strPtr);
      } catch (err) {
        console.error(err.message);
      }
    } else {
      console.log("Not running in Backpack wallet");
    }
  },

  ExternSignTransactionXNFT: async function (transaction, callback) {
    if ("xnft" in window && window.xnft != undefined && window.xnft.solana != undefined) {
      try {
        const messageBase58 = UTF8ToString(transaction);
        const message = solanaWeb3.Message.from(bs58.decode(messageBase58));
        const tx = solanaWeb3.Transaction.populate(message);
        const signedTransaction = await window.xnft.solana.signTransaction(tx);
        var sign = bs58.encode(signedTransaction.signature);
        var lenSign = lengthBytesUTF8(sign) + 1;
        var strPtr = _malloc(lenSign);
        stringToUTF8(sign, strPtr, lenSign);
        console.log(strPtr);
        Module.dynCall_vi(callback, strPtr);
      } catch (err) {
        console.error(err.message);
      }
    } else {
      console.error("Not running in Backpack wallet");
    }
  },
});
