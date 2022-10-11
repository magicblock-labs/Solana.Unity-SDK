mergeInto(LibraryManager.library, {

    ExternCopyToPastebin: async function (message) {
        console.log(UTF8ToString(message));
        try {
            window.navigator.clipboard.writeText(UTF8ToString(message));
        } catch (err) {
            console.error('Unable to copy to clipboard');
        }
    },

});
