mergeInto(LibraryManager.library, {
	HyperlinkXNFT : function(linkUrl)
	{
		url = UTF8ToString(linkUrl);
		window.xnft.openWindow(url);
	}
});