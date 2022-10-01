mergeInto(LibraryManager.library, {

    ExternConnectPhantom: async function (callback) {
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
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
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
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
                window.alert('Phantom error: ' + err.message);
                console.error(err.message);
            }
        } else {
            window.alert('Please install phantom browser extension.');
        }
    },

    ExternSignAndSendTransaction: async function (inputTransaction) {
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
            try {
                const {signature} = await window.phantom.solana.request({
                    method: 'signAndSendTransaction',
                    params: {
                        message: UTF8ToString(inputTransaction),
                    },
                });

                console.log('Signed and send resulting in signature: ' + signature);

                window.unityInstance.SendMessage('PhantomWallet', 'OnTransactionSignedAndSent', signature);
            } catch (err) {
                window.alert('Phantom error: ' + err.message);
                console.error(err.message);
            }
        } else {
            window.alert('Please install phantom browser extension.');
        }
    },

});
