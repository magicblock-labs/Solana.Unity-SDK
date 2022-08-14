mergeInto(LibraryManager.library, {

    ExternConnectPhantom: async function () {
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
            try {
                const resp = await window.phantom.solana.connect();
                console.log(resp.publicKey.toString());
                window.unityInstance.SendMessage('PhantomWallet', 'OnPhantomConnected', resp.publicKey.toString());
            } catch (err) {
                window.alert(err.toString());
            }
        } else {
            window.alert("Please install phantom browser extension.");
        }
    },

    ExternSignTransaction: async function (transaction) {
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
            try {
               const signedTransaction = await window.phantom.solana.request({
                  method: "signTransaction",
                  params: {
                     message: UTF8ToString(transaction),
                  },
               });
                console.log(signedTransaction);
                window.unityInstance.SendMessage('PhantomWallet', 'OnTransactionSigned', signedTransaction.signature);
            } catch (err) {
                console.error(err.message);
            }
        } else {
            window.alert("Please install phantom browser extension.");
        }
    },

    ExternSignAndSendTransaction: async function (inputTransaction) {
        if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
            try {
                console.log("Attempting to sign transaction: " + UTF8ToString(inputTransaction));
            
                const {signature} = await window.phantom.solana.request({
                    method: "signAndSendTransaction",
                    params: {
                        message: UTF8ToString(inputTransaction),
                    },
                });

                console.log("Signed and send resulting in signature: " + signature);

                window.unityInstance.SendMessage('PhantomWallet', 'OnTransactionSignedAndSent', signature);
            } catch (err) {
                console.error(err.message);
            }
        } else {
            window.alert("Please install phantom browser extension.");
        }
    },

});
