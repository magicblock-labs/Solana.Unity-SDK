int WebSocketAllocate(const char * url){}

int WebSocketAddSubProtocol (int instanceId, const char * subprotocol){}

void WebSocketFree (int instanceId){}

void WebSocketSetOnOpen (void * callback){}

void WebSocketSetOnMessage (void * callback){}

void WebSocketSetOnError (void * callback){}

void WebSocketSetOnClose (void * callback){}

int WebSocketConnect (int instanceId){}

int WebSocketClose (int instanceId, int code, char * reason){}

int WebSocketSend (int instanceId, void * dataPtr, int dataLength){}

int WebSocketSendText (int instanceId, char * message){}

int WebSocketGetState (int instanceId){}
