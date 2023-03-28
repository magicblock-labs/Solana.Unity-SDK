mergeInto(LibraryManager.library, {

    ExternConnectPhantom: async function (callback) {
        if ('phantom' in window && window.phantom != undefined && window.phantom.solana != undefined) {
            try {
                const resp = await window.phantom.solana.connect();
                var pubKey = resp.publicKey.toString();
                console.log(pubKey);
                var lenPubKey = lengthBytesUTF8(pubKey) + 1;
                var strPtr = _malloc(lenPubKey);
                stringToUTF8(pubKey, strPtr, lenPubKey);
                Module.dynCall_vi(callback, strPtr);
            } catch (err) {
                window.alert('Phantom error: ' + err.toString());
                console.error(err.message);
            }
        } else {
            window.alert('Please install phantom browser extension.');
        }
    },

    ExternSignTransaction: async function (transaction, callback) {
        if ('phantom' in window && window.phantom != undefined && window.phantom.solana != undefined) {
            try {
               const signedTransaction = await window.phantom.solana.request({
                  method: 'signTransaction',
                  params: {
                     message: UTF8ToString(transaction),
                  },
               });
                console.log(signedTransaction);
                var sign = signedTransaction.signature;
                var lenSign = lengthBytesUTF8(sign) + 1;
                var strPtr = _malloc(lenSign);
                stringToUTF8(sign, strPtr, lenSign);
                Module.dynCall_vi(callback, strPtr);
            } catch (err) {
                console.error(err.message);
            }
        } else {
            window.alert('Please install phantom browser extension.');
        }
    },


    ExternSignMessage: async function (message, callback) {
        if ('phantom' in window && window.phantom != undefined && window.phantom.solana != undefined) {
            try {
               const messageBase64String = UTF8ToString(message);
               const messageBytes = Uint8Array.from(atob(messageBase64String), (c) => c.charCodeAt(0));
               const signedMessage = await window.phantom.solana.request({
                  method: 'signMessage',
                  params: {
                     message: messageBytes
                  },
               });
                console.log(signedMessage);
                var sign = signedMessage.signature;
                var lenSign = lengthBytesUTF8(sign) + 1;
                var strPtr = _malloc(lenSign);
                stringToUTF8(sign, strPtr, lenSign);
                Module.dynCall_vi(callback, strPtr);
            } catch (err) {
                console.error(err.message);
            }
        } else {
            window.alert('Please install phantom browser extension.');
        }
    },
    
});
