mergeInto(LibraryManager.library, {
	HyperlinkXNFT : function(linkUrl)
	{
		url = UTF8ToString(linkUrl);
    	console.log('Opening link: ' + url);
		window.open(url);
	}
});