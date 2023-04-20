mergeInto(LibraryManager.library, {
    InitWalletAdapter: async function (callback) {
        // Add UnityWalletAdapter from CDN
        if(window.walletAdapterLib == undefined){
            console.log("Adding WalletAdapterLib")
            var script = document.createElement("script");
            script.src = "https://cdn.jsdelivr.net/gh/nicoeft/unity-wallet-adapter@main/dist/wallet-adapter-lib.js";
            document.head.appendChild(script);
            script.onload = function() {
                console.log("WalletAdapterLib loaded");
                Module.dynCall_vi(callback);
            };
        }
        console.log(window.walletAdapterLib);
    },
     ExternGetWallets: function() {
        try {
            const wallets = window.walletAdapterLib.getWallets();
            var bufferSize = lengthBytesUTF8(wallets) + 1;
            var walletsPtr = _malloc(bufferSize);
            stringToUTF8(wallets, walletsPtr, bufferSize);
            return walletsPtr;
        } catch (err) {
            console.error(err.message);
        }
    },
    ExternConnectWallet: async function (walletNamePtr, callback) {
         try {
                const walletName = UTF8ToString(walletNamePtr)
                var pubKey = await window.walletAdapterLib.connectWallet(walletName);
                var bufferSize = lengthBytesUTF8(pubKey) + 1;
                var pubKeyPtr = _malloc(bufferSize);
                stringToUTF8(pubKey, pubKeyPtr, bufferSize);
                Module.dynCall_vi(callback, pubKeyPtr);
         } catch (err) {
            console.error(err.message);
         }
    },
    ExternSignTransactionWallet: async function (walletNamePtr, transactionPtr, callback) {
         try {
                const walletName = UTF8ToString(walletNamePtr)
                var base64transaction = UTF8ToString(transactionPtr)
                var signedTransaction = await window.walletAdapterLib.signTransaction(walletName, base64transaction);
                var signature = signedTransaction.signature.toString('base64');
                var bufferSize = lengthBytesUTF8(signature) + 1;
                var signaturePtr = _malloc(bufferSize);
                stringToUTF8(signature, signaturePtr, bufferSize);
                Module.dynCall_vi(callback, signaturePtr);          
         } catch (err) {
            console.error(err.message);
         }
    },
    ExternSignMessageWallet: async function (walletNamePtr, messagePtr, callback) {
             try {
                    const walletName = UTF8ToString(walletNamePtr)
                    var base64Message = UTF8ToString(messagePtr)
                    var signature = await window.walletAdapterLib.signMessage(walletName, base64Message);
                    var signatureStr =  signature.toString('base64');
                    var bufferSize = lengthBytesUTF8(signatureStr) + 1;
                    var signaturePtr = _malloc(bufferSize);
                    stringToUTF8(signatureStr, signaturePtr, bufferSize);
                    Module.dynCall_vi(callback, signaturePtr);          
             } catch (err) {
                console.error(err.message);
             }
        },
} );



 