extern "C"
{
  void WebSocketAllocate(const char * url){}

  int WebSocketAddSubProtocol (int instanceId, const char * subprotocol){}

  void WebSocketFree (int instanceId);

  void WebSocketSetOnOpen (void * callback);

  void WebSocketSetOnMessage (void * callback);

  void WebSocketSetOnError (void * callback);

  void WebSocketSetOnClose (void * callback);
}