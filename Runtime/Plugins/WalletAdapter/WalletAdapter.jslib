mergeInto(LibraryManager.library, {
    ExternConnectWallet: async function (walletNamePtr, callback) {
        console.log("ExternConnectWallet called");
         try {
                console.log("window.walletAdapterLib: ", window.walletAdapterLib);
                const walletName = UTF8ToString(walletNamePtr)
                var pubKey = await window.walletAdapterLib.connectWallet(walletName);
                console.log("pubKey: " + pubKey);
                var lenPubKey = lengthBytesUTF8(pubKey) + 1;
                var strPtr = _malloc(lenPubKey);
                stringToUTF8(pubKey, strPtr, lenPubKey);
                console.log("strPtr: " + strPtr);
                Module.dynCall_vi(callback, strPtr);
         } catch (err) {
            console.error(err.message);
         }
    },
    
    ExternSignTransactionWallet: async function (walletNamePtr, transactionPtr, callback) {
        console.log("ExternSignTransactionWallet called");
         try {
                console.log("window.walletAdapterLib: ", window.walletAdapterLib);
                const walletName = UTF8ToString(walletNamePtr)
                var base64transaction = UTF8ToString(transactionPtr)
                console.log("base64transaction: " + base64transaction);
                var signedTransaction = await window.walletAdapterLib.signTransaction(walletName, base64transaction);
                var signature = signedTransaction.signature.toString('base64');
                console.log("signature: " + signature);
                var lenSign = lengthBytesUTF8(signature) + 1;
                var strPtr = _malloc(lenSign);
                stringToUTF8(signature, strPtr, lenSign);
                console.log("strPtr: " + strPtr);
                Module.dynCall_vi(callback, strPtr);          
         } catch (err) {
            console.error(err.message);
         }
    },
    
    
    
    ExternSignMessageWallet: function(walletNamePtr, messagePtr) {
       const walletName = UTF8ToString(walletNamePtr)
       const message = UTF8ToString(messagePtr)
       window.walletAdapterLib.signMessage(walletId, message);
    },
    DisconnectWalletByNameInternal: function (walletName) {
       const walletId = Pointer_stringify(walletName);
       
       window.unityConfig.disconnectWalletByName(walletId);
    },

    ConnectWalletByNameInternal: function(walletName) {
        const walletId = Pointer_stringify(walletName);

        window.unityConfig.connectWalletByName(walletId);
    },
    
    GetWalletConfigInternal: function(){
      const r = window.unityConfig.getWalletDataJsonStr();
      
      return window.unityConfig.createUnityStr(r);
    },
    
    RequestMintMetadataInternal: function(mint){
        const mintStr = Pointer_stringify(mint);
        
        window.unityConfig.requestMintMetadata(mintStr);
    },
    
   
    
    SendTransactionInternal: function(walletName, base64tx) {
        const walletId = Pointer_stringify(walletName);
        const base64transaction = Pointer_stringify(base64tx);

        window.unityConfig.sentTransactionWithWalletByName(walletId, base64transaction);
    },
} );
