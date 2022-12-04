function createUnityInstance(canvas, config, onProgress) {
  onProgress = onProgress || function () {};


  function showBanner(msg, type) {
    // Only ever show one error at most - other banner messages after that should get ignored
    // to avoid noise.
    if (!showBanner.aborted && config.showBanner) {
      if (type == 'error') showBanner.aborted = true;
      return config.showBanner(msg, type);
    }

    // Fallback to console logging if visible banners have been suppressed
    // from the main page.
    switch(type) {
      case 'error': console.error(msg); break;
      case 'warning': console.warn(msg); break;
      default: console.log(msg); break;
    }
  }

  function errorListener(e) {
    var error = e.reason || e.error;
    var message = error ? error.toString() : (e.message || e.reason || '');
    var stack = (error && error.stack) ? error.stack.toString() : '';

    // Do not repeat the error message if it's present in the stack trace.
    if (stack.startsWith(message)) {
      stack = stack.substring(message.length);
    }

    message += '\n' + stack.trim();

    if (!message || !Module.stackTraceRegExp || !Module.stackTraceRegExp.test(message))
      return;

    var filename = e.filename || (error && (error.fileName || error.sourceURL)) || '';
    var lineno = e.lineno || (error && (error.lineNumber || error.line)) || 0;

    errorHandler(message, filename, lineno);
  }

  var Module = {
    canvas: canvas,
    webglContextAttributes: {
      preserveDrawingBuffer: false,
    },
    cacheControl: function (url) {
      return url == Module.dataUrl ? "must-revalidate" : "no-store";
    },
    streamingAssetsUrl: "StreamingAssets",
    downloadProgress: {},
    deinitializers: [],
    intervals: {},
    setInterval: function (func, ms) {
      var id = window.setInterval(func, ms);
      this.intervals[id] = true;
      return id;
    },
    clearInterval: function(id) {
      delete this.intervals[id];
      window.clearInterval(id);
    },
    preRun: [],
    postRun: [],
    print: function (message) {
      console.log(message);
    },
    printErr: function (message) {
      console.error(message);

      if (typeof message === 'string' && message.indexOf('wasm streaming compile failed') != -1) {
        if (message.toLowerCase().indexOf('mime') != -1) {
          showBanner('HTTP Response Header "Content-Type" configured incorrectly on the server for file ' + Module.codeUrl + ' , should be "application/wasm". Startup time performance will suffer.', 'warning');
        } else {
          showBanner('WebAssembly streaming compilation failed! This can happen for example if "Content-Encoding" HTTP header is incorrectly enabled on the server for file ' + Module.codeUrl + ', but the file is not pre-compressed on disk (or vice versa). Check the Network tab in browser Devtools to debug server header configuration.', 'warning');
        }
      }
    },
    locateFile: function (url) {
      return (
        url == "build.wasm" ? this.codeUrl :
        url
      );
    },
    disabledCanvasEvents: [
      "contextmenu",
      "dragstart",
    ],
  };

  for (var parameter in config)
    Module[parameter] = config[parameter];

  Module.streamingAssetsUrl = new URL(Module.streamingAssetsUrl, document.URL).href;

  // Operate on a clone of Module.disabledCanvasEvents field so that at Quit time
  // we will ensure we'll remove the events that we created (in case user has
  // modified/cleared Module.disabledCanvasEvents in between)
  var disabledCanvasEvents = Module.disabledCanvasEvents.slice();

  function preventDefault(e) {
    e.preventDefault();
  }

  disabledCanvasEvents.forEach(function (disabledCanvasEvent) {
    canvas.addEventListener(disabledCanvasEvent, preventDefault);
  });

  window.addEventListener("error", errorListener);
  window.addEventListener("unhandledrejection", errorListener);

  // Clear the event handlers we added above when the app quits, so that the event handler
  // functions will not hold references to this JS function scope after
  // exit, to allow JS garbage collection to take place.
  Module.deinitializers.push(function() {
    Module['disableAccessToMediaDevices']();
    disabledCanvasEvents.forEach(function (disabledCanvasEvent) {
      canvas.removeEventListener(disabledCanvasEvent, preventDefault);
    });
    window.removeEventListener("error", errorListener);
    window.removeEventListener("unhandledrejection", errorListener);

    for (var id in Module.intervals)
    {
      window.clearInterval(id);
    }
    Module.intervals = {};
  });

  Module.QuitCleanup = function () {
    for (var i = 0; i < Module.deinitializers.length; i++) {
      Module.deinitializers[i]();
    }
    Module.deinitializers = [];
    // After all deinitializer callbacks are called, notify user code that the Unity game instance has now shut down.
    if (typeof Module.onQuit == "function")
      Module.onQuit();
    };

  // Safari does not automatically stretch the fullscreen element to fill the screen.
  // The CSS width/height of the canvas causes it to remain the same size in the full screen
  // window on Safari, resulting in it being a small canvas with black borders filling the
  // rest of the screen.
  var _savedElementWidth = "";
  var _savedElementHeight = "";
  // Safari uses webkitfullscreenchange event and not fullscreenchange
  document.addEventListener("webkitfullscreenchange", function(e) {
    // Safari uses webkitCurrentFullScreenElement and not fullscreenElement.
    var fullscreenElement = document.webkitCurrentFullScreenElement;
    if (fullscreenElement === canvas) {
      if (canvas.style.width) {
        _savedElementWidth = canvas.style.width;
        _savedElementHeight = canvas.style.height;
        canvas.style.width = "100%";
        canvas.style.height = "100%";
      }
    } else {
      if (_savedElementWidth) {
        canvas.style.width = _savedElementWidth;
        canvas.style.height = _savedElementHeight;
        _savedElementWidth = "";
        _savedElementHeight = "";
      }
    }
  });

  var unityInstance = {
    Module: Module,
    SetFullscreen: function () {
      if (Module.SetFullscreen)
        return Module.SetFullscreen.apply(Module, arguments);
      Module.print("Failed to set Fullscreen mode: Player not loaded yet.");
    },
    SendMessage: function () {
      if (Module.SendMessage)
        return Module.SendMessage.apply(Module, arguments);
      Module.print("Failed to execute SendMessage: Player not loaded yet.");
    },
    Quit: function () {
      return new Promise(function (resolve, reject) {
        Module.shouldQuit = true;
        Module.onQuit = resolve;
      });
    },
  };


  Module.SystemInfo = (function () {

    var browser, browserVersion, os, osVersion, canvas, gpu;

    var ua = navigator.userAgent + ' ';
    var browsers = [
      ['Firefox', 'Firefox'],
      ['OPR', 'Opera'],
      ['Edg', 'Edge'],
      ['SamsungBrowser', 'Samsung Browser'],
      ['Trident', 'Internet Explorer'],
      ['MSIE', 'Internet Explorer'],
      ['Chrome', 'Chrome'],
      ['CriOS', 'Chrome on iOS Safari'],
      ['FxiOS', 'Firefox on iOS Safari'],
      ['Safari', 'Safari'],
    ];

    function extractRe(re, str, idx) {
      re = RegExp(re, 'i').exec(str);
      return re && re[idx];
    }
    for(var b = 0; b < browsers.length; ++b) {
      browserVersion = extractRe(browsers[b][0] + '[\/ ](.*?)[ \\)]', ua, 1);
      if (browserVersion) {
        browser = browsers[b][1];
        break;
      }
    }
    if (browser == 'Safari') browserVersion = extractRe('Version\/(.*?) ', ua, 1);
    if (browser == 'Internet Explorer') browserVersion = extractRe('rv:(.*?)\\)? ', ua, 1) || browserVersion;

    // These OS strings need to match the ones in Runtime/Misc/SystemInfo.cpp::GetOperatingSystemFamily()
    var oses = [
      ['Windows (.*?)[;\)]', 'Windows'],
      ['Android ([0-9_\.]+)', 'Android'],
      ['iPhone OS ([0-9_\.]+)', 'iPhoneOS'],
      ['iPad.*? OS ([0-9_\.]+)', 'iPadOS'],
      ['FreeBSD( )', 'FreeBSD'],
      ['OpenBSD( )', 'OpenBSD'],
      ['Linux|X11()', 'Linux'],
      ['Mac OS X ([0-9_\.]+)', 'MacOS'],
      ['bot|google|baidu|bing|msn|teoma|slurp|yandex', 'Search Bot']
    ];
    for(var o = 0; o < oses.length; ++o) {
      osVersion = extractRe(oses[o][0], ua, 1);
      if (osVersion) {
        os = oses[o][1];
        osVersion = osVersion.replace(/_/g, '.');
        break;
      }
    }
    var versionMappings = {
      'NT 5.0': '2000',
      'NT 5.1': 'XP',
      'NT 5.2': 'Server 2003',
      'NT 6.0': 'Vista',
      'NT 6.1': '7',
      'NT 6.2': '8',
      'NT 6.3': '8.1',
      'NT 10.0': '10'
    };
    osVersion = versionMappings[osVersion] || osVersion;

    // TODO: Add mobile device identifier, e.g. SM-G960U

    canvas = document.createElement("canvas");
    if (canvas) {
      gl = canvas.getContext("webgl2");
      glVersion = gl ? 2 : 0;
      if (!gl) {
        if (gl = canvas && canvas.getContext("webgl")) glVersion = 1;
      }

      if (gl) {
        gpu = (gl.getExtension("WEBGL_debug_renderer_info") && gl.getParameter(0x9246 /*debugRendererInfo.UNMASKED_RENDERER_WEBGL*/)) || gl.getParameter(0x1F01 /*gl.RENDERER*/);
      }
    }

    var hasThreads = typeof SharedArrayBuffer !== 'undefined';
    var hasWasm = typeof WebAssembly === "object" && typeof WebAssembly.compile === "function";
    return {
      width: screen.width,
      height: screen.height,
      userAgent: ua.trim(),
      browser: browser || 'Unknown browser',
      browserVersion: browserVersion || 'Unknown version',
      mobile: /Mobile|Android|iP(ad|hone)/.test(navigator.appVersion),
      os: os || 'Unknown OS',
      osVersion: osVersion || 'Unknown OS Version',
      gpu: gpu || 'Unknown GPU',
      language: navigator.userLanguage || navigator.language,
      hasWebGL: glVersion,
      hasCursorLock: !!document.body.requestPointerLock,
      hasFullscreen: !!document.body.requestFullscreen || !!document.body.webkitRequestFullscreen, // Safari still uses the webkit prefixed version
      hasThreads: hasThreads,
      hasWasm: hasWasm,
      // This should be updated when we re-enable wasm threads. Previously it checked for WASM thread
      // support with: var wasmMemory = hasWasm && hasThreads && new WebAssembly.Memory({"initial": 1, "maximum": 1, "shared": true});
      // which caused Chrome to have a warning that SharedArrayBuffer requires cross origin isolation.
      hasWasmThreads: false,
    };
  })();

  function errorHandler(message, filename, lineno) {
    // Unity needs to rely on Emscripten deferred fullscreen requests, so these will make their way to error handler
    if (message.indexOf('fullscreen error') != -1)
      return;

    if (Module.startupErrorHandler) {
      Module.startupErrorHandler(message, filename, lineno);
      return;
    }
    if (Module.errorHandler && Module.errorHandler(message, filename, lineno))
      return;
    console.log("Invoking error handler due to\n" + message);

    // Support Firefox window.dump functionality.
    if (typeof dump == "function")
      dump("Invoking error handler due to\n" + message);

    if (errorHandler.didShowErrorMessage)
      return;
    var message = "An error occurred running the Unity content on this page. See your browser JavaScript console for more info. The error was:\n" + message;
    if (message.indexOf("DISABLE_EXCEPTION_CATCHING") != -1) {
      message = "An exception has occurred, but exception handling has been disabled in this build. If you are the developer of this content, enable exceptions in your project WebGL player settings to be able to catch the exception or see the stack trace.";
    } else if (message.indexOf("Cannot enlarge memory arrays") != -1) {
      message = "Out of memory. If you are the developer of this content, try allocating more memory to your WebGL build in the WebGL player settings.";
    } else if (message.indexOf("Invalid array buffer length") != -1  || message.indexOf("Invalid typed array length") != -1 || message.indexOf("out of memory") != -1 || message.indexOf("could not allocate memory") != -1) {
      message = "The browser could not allocate enough memory for the WebGL content. If you are the developer of this content, try allocating less memory to your WebGL build in the WebGL player settings.";
    }
    alert(message);
    errorHandler.didShowErrorMessage = true;
  }


  Module.abortHandler = function (message) {
    errorHandler(message, "", 0);
    return true;
  };

  Error.stackTraceLimit = Math.max(Error.stackTraceLimit || 0, 50);

  function progressUpdate(id, e) {
    if (id == "symbolsUrl")
      return;
    var progress = Module.downloadProgress[id];
    if (!progress)
      progress = Module.downloadProgress[id] = {
        started: false,
        finished: false,
        lengthComputable: false,
        total: 0,
        loaded: 0,
      };
    if (typeof e == "object" && (e.type == "progress" || e.type == "load")) {
      if (!progress.started) {
        progress.started = true;
        progress.lengthComputable = e.lengthComputable;
      }
      progress.total = e.total;
      progress.loaded = e.loaded;
      if (e.type == "load")
        progress.finished = true;
    }
    var loaded = 0, total = 0, started = 0, computable = 0, unfinishedNonComputable = 0;
    for (var id in Module.downloadProgress) {
      var progress = Module.downloadProgress[id];
      if (!progress.started)
        return 0;
      started++;
      if (progress.lengthComputable) {
        loaded += progress.loaded;
        total += progress.total;
        computable++;
      } else if (!progress.finished) {
        unfinishedNonComputable++;
      }
    }
    var totalProgress = started ? (started - unfinishedNonComputable - (total ? computable * (total - loaded) / total : 0)) / started : 0;
    onProgress(0.9 * totalProgress);
  }

Module.fetchWithProgress = function () {
  /**
   * Estimate length of uncompressed content by taking average compression ratios
   * of compression type into account.
   * @param {Response} response A Fetch API response object
   * @param {boolean} lengthComputable Return wether content length was given in header.
   * @returns {number}
   */
  function estimateContentLength(response, lengthComputable) {
    if (!lengthComputable) {
      // No content length available
      return 0;
    }

    var compression = response.headers.get("Content-Encoding");
    var contentLength = parseInt(response.headers.get("Content-Length"));
    
    switch (compression) {
    case "br":
      return Math.round(contentLength * 5);
    case "gzip":
      return Math.round(contentLength * 4);
    default:
      return contentLength;
    }
  }


  function fetchWithProgress(resource, init) {
    var onProgress = function () { };
    if (init && init.onProgress) {
      onProgress = init.onProgress;
    }

    return fetch(resource, init).then(function (response) {
      var reader = (typeof response.body !== "undefined") ? response.body.getReader() : undefined;
      var lengthComputable = typeof response.headers.get('Content-Length') !== "undefined";
      var estimatedContentLength = estimateContentLength(response, lengthComputable);
      var body = new Uint8Array(estimatedContentLength);
      var trailingChunks = [];
      var receivedLength = 0;
      var trailingChunksStart = 0;

      if (!lengthComputable) {
        console.warn("[UnityCache] Response is served without Content-Length header. Please reconfigure server to include valid Content-Length for better download performance.");
      }

      function readBodyWithProgress() {
        if (typeof reader === "undefined") {
          // Browser does not support streaming reader API
          // Fallback to Respone.arrayBuffer()
          return response.arrayBuffer().then(function (buffer) {
            onProgress({
              type: "progress",
              total: buffer.length,
              loaded: 0,
              lengthComputable: lengthComputable
            });
            
            return new Uint8Array(buffer);
          });
        }
        
        // Start reading memory chunks
        return reader.read().then(function (result) {
          if (result.done) {
            return concatenateTrailingChunks();
          }

          if ((receivedLength + result.value.length) <= body.length) {
            // Directly append chunk to body if enough memory was allocated
            body.set(result.value, receivedLength);
            trailingChunksStart = receivedLength + result.value.length;
          } else {
            // Store additional chunks in array to append later
            trailingChunks.push(result.value);
          }

          receivedLength += result.value.length;
          onProgress({
            type: "progress",
            total: Math.max(estimatedContentLength, receivedLength),
            loaded: receivedLength,
            lengthComputable: lengthComputable
          });

          return readBodyWithProgress();
        });
      }

      function concatenateTrailingChunks() {
        if (receivedLength === estimatedContentLength) {
          return body;
        }

        if (receivedLength < estimatedContentLength) {
          // Less data received than estimated, shrink body
          return body.slice(0, receivedLength);
        }

        // More data received than estimated, create new larger body to prepend all additional chunks to the body
        var newBody = new Uint8Array(receivedLength);
        newBody.set(body, 0);
        var position = trailingChunksStart;
        for (var i = 0; i < trailingChunks.length; ++i) {
          newBody.set(trailingChunks[i], position);
          position += trailingChunks[i].length;
        }

        return newBody;
      }

      return readBodyWithProgress().then(function (parsedBody) {
        onProgress({
          type: "load",
          total: parsedBody.length,
          loaded: parsedBody.length,
          lengthComputable: lengthComputable
        });

        response.parsedBody = parsedBody;
        return response;
      });
    });
  }

  return fetchWithProgress;
}();
  Module.UnityCache = function () {
  var UnityCacheDatabase = { name: "UnityCache", version: 3 };
  var RequestStore = { name: "RequestStore", version: 1 };
  var WebAssemblyStore = { name: "WebAssembly", version: 1 };
  var indexedDB = window.indexedDB || window.mozIndexedDB || window.webkitIndexedDB || window.msIndexedDB;

  /**
   * A request cache that uses the browser Index DB to cache large requests
   */
  function UnityCache() {
    var cache = this;

    function upgradeDatabase(e) {
      var database = e.target.result;
      if (!database.objectStoreNames.contains(WebAssemblyStore.name))
        database.createObjectStore(WebAssemblyStore.name);

      if (!database.objectStoreNames.contains(RequestStore.name)) {
        var objectStore = database.createObjectStore(RequestStore.name, { keyPath: "url" });
        ["version", "company", "product", "updated", "revalidated", "accessed"].forEach(function (index) { objectStore.createIndex(index, index); });
      }
    }

    cache.isConnected = new Promise(function (resolve, reject) {
      try {
        // Workaround for WebKit bug 226547:
        // On very first page load opening a connection to IndexedDB hangs without triggering onerror.
        // Add a timeout that triggers the error handling code.
        cache.openDBTimeout = setTimeout(function () {
          if (typeof cache.database != "undefined")
            return;

          reject(new Error("Could not connect to database: Timeout."));
        }, 2000);

        function clearOpenDBTimeout() {
          if (!cache.openDBTimeout) {
            return;
          }

          clearTimeout(cache.openDBTimeout);
          cache.openDBTimeout = null;
        }

        var openRequest = indexedDB.open(UnityCacheDatabase.name, UnityCacheDatabase.version);

        openRequest.onupgradeneeded = function (e) {
          upgradeDatabase(e);
        };

        openRequest.onsuccess = function (e) {
          clearOpenDBTimeout();
          cache.database = e.target.result;
          resolve();
        };

        openRequest.onerror = function (error) {
          clearOpenDBTimeout();
          cache.database = null;
          reject(new Error("Could not connect to database."));
        };
      } catch (error) {
        clearOpenDBTimeout();
        cache.database = null;
        reject(new Error("Could not connect to database."));
      }
    });
  };

  /**
   * Name and version of unity cache database
   */
  UnityCache.UnityCacheDatabase = UnityCacheDatabase;
  /**
   * Name and version of request store database
   */
  UnityCache.RequestStore = RequestStore;
  /**
   * Name and version of web assembly store database
   */
  UnityCache.WebAssemblyStore = WebAssemblyStore;

  var instance = null;

  /**
   * Singleton accessor. Returns unity cache instance
   * @returns {UnityCache}
   */
  UnityCache.getInstance = function () {
    if (!instance) {
      instance = new UnityCache();
    }

    return instance;
  }

  /**
   * Destroy unity cache instance. Returns a promise that waits for the
   * database connection to be closed.
   * @returns {Promise<void>}
   */
  UnityCache.destroyInstance = function () {
    if (!instance) {
      return Promise.resolve();
    }

    return instance.close().then(function () {
      instance = null;
    });
  }

  /**
   * Clear the unity cache. 
   * @returns {Promise<void>} A promise that resolves when the cache is cleared.
   */
  UnityCache.clearCache = function () {
    return UnityCache.destroyInstance().then(function () {
      return new Promise(function (resolve, reject) {
        var request = indexedDB.deleteDatabase(UnityCacheDatabase.name);
        request.onsuccess = function () {
          resolve();
        }
        request.onerror = function () {
          reject(new Error("Could not delete database."));
        }
        request.onblocked = function () {
          reject(new Error("Database blocked."));
        }
      });
    });
  }

  /**
   * Execute an operation on the cache
   * @param {string} store The name of the store to use
   * @param {string} operation The operation to to execute on the cache
   * @param {Array} parameters Parameters for the operation
   * @returns {Promise} A promise to the cache entry
   */
  UnityCache.prototype.execute = function (store, operation, parameters) {
    return this.isConnected.then(function () {
      return new Promise(function (resolve, reject) {
        try {
          // Failure during initialization of database -> reject Promise
          if (this.database === null) {
            reject(new Error("indexedDB access denied"))
            return;
          }

          // Create a transaction for the request
          var accessMode = ["put", "delete", "clear"].indexOf(operation) != -1 ? "readwrite" : "readonly";
          var transaction = this.database.transaction([store], accessMode)
          var target = transaction.objectStore(store);
          if (operation == "openKeyCursor") {
            target = target.index(parameters[0]);
            parameters = parameters.slice(1);
          }

          // Make a request to the database
          var request = target[operation].apply(target, parameters);
          request.onsuccess = function (e) {
            resolve(e.target.result);
          };
          request.onerror = function (error) {
            reject(error);
          };
        } catch (error) {
          reject(error);
        }
      }.bind(this));
    }.bind(this));
  };

  /**
   * Load a request from the cache.
   * @param {string} url The url of the request 
   * @returns {Promise<Object>} A promise that resolves to the cached result or null if request is not in cache.
   */
  UnityCache.prototype.loadRequest = function (url) {
    return this.execute(RequestStore.name, "get", [url]);
  }

  /**
   * Store a request in the cache
   * @param {Object} request The request to store
   * @returns {Promise<void>} A promise that resolves when the request is stored in the cache.
   */
  UnityCache.prototype.storeRequest = function (request) {
    return this.execute(RequestStore.name, "put", [request]);
  }

  /**
   * Close database connection.
   */
  UnityCache.prototype.close = function () {
    return this.isConnected.then(function () {
      if (!this.database) {
        return;
      }

      this.database.close();
      this.database = null;
    }.bind(this));
  }

  return UnityCache;
}();
  Module.cachedFetch = function () {
  var UnityCache = Module.UnityCache;
  var RequestStore = UnityCache.RequestStore;
  var fetchWithProgress = Module.fetchWithProgress;

  function log(message) {
    console.log("[UnityCache] " + message);
  }

  function resolveURL(url) {
    resolveURL.link = resolveURL.link || document.createElement("a");
    resolveURL.link.href = url;
    return resolveURL.link.href;
  }

  function isCrossOriginURL(url) {
    var originMatch = window.location.href.match(/^[a-z]+:\/\/[^\/]+/);
    return !originMatch || url.lastIndexOf(originMatch[0], 0);
  }

  /**
   * A response restored from the unity cache.
   * Implements the same interface as a fetch API Response
   */
  function CachedResponse(options) {
    options = options || {};
    this.headers = new Headers();
    Object.keys(options.headers).forEach(function (key) {
      this.headers.set(key, options.headers[key]);
    }.bind(this));
    this.redirected = options.redirected;
    this.status = options.status;
    this.statusText = options.statusText;
    this.type = options.type;
    this.url = options.url;
    this.parsedBody = options.parsedBody;

    Object.defineProperty(this, "ok", {
      get: function () {
        return this.status >= 200 && this.status <= 299;
      }.bind(this)
    });
  }

  /**
   * Takes a Response stream and reads it to completion. It returns a promise that resolves with an ArrayBuffer.
   * @returns {Promise<ArrayBuffer>}
   */
  CachedResponse.prototype.arrayBuffer = function () {
    return Promise.resolve(this.parsedBody.buffer);
  }

  /**
   * Takes a Response stream and reads it to completion. It returns a promise that resolves with a Blob.
   * @returns {Promise<Blob>}
   */
  CachedResponse.prototype.blob = function () {
    return this.arrayBuffer().then(function (buffer) {
      return new Blob([buffer]);
    });
  }
  
  // TODO: Implement Body.formData()
  // Takes a Response stream and reads it to completion. It returns a promise that resolves with a FormData object.
  
  /**
   * Takes a Response stream and reads it to completion. It returns a promise that resolves with the result of parsing the body text as JSON, which is a JavaScript value of datatype object, string, etc.
   * @returns {Promise<Object>}
   */
  CachedResponse.prototype.json = function () {
    return this.text().then(function (text) {
      return JSON.parse(text);
    });
  }
  
  /**
   * Takes a Response stream and reads it to completion. It returns a promise that resolves with a USVString (text).
   * @returns {Promise<string>}
   */
  CachedResponse.prototype.text = function () {
    var utf8decoder = new TextDecoder();

    return Promise.resolve(utf8decoder.decode(this.parsedBody));
  }

  function createCacheEntry(url, company, product, timestamp, response) {
    var cacheEntry = {
      url: url,
      version: RequestStore.version,
      company: company,
      product: product, 
      updated: timestamp,
      revalidated: timestamp,
      accessed: timestamp,
      response: {
        headers: {}
      }
    };

    if (response) {
      response.headers.forEach(function (value, key) {
        cacheEntry.response.headers[key] = value; 
      });
      ["redirected", "status", "statusText", "type", "url"].forEach(function (property) { cacheEntry.response[property] = response[property]; });
      cacheEntry.response.parsedBody = response.parsedBody;
    }
    return cacheEntry;
  }

  function isCacheEnabled(url, init) {
    if (init && init.method && init.method !== "GET") {
      return false;
    }

    if (init && ["must-revalidate", "immutable"].indexOf(init.control) == -1) {
      return false;
    }

    if (!url.match("^https?:\/\/")) {
      return false;
    }

    return true;
  }

  function cachedFetch(resource, init) {
    var unityCache = UnityCache.getInstance();
    var url = resolveURL((typeof resource === "string") ? resource : resource.url);
    var cache = { enabled: isCacheEnabled(url, init) };
    if (init) {
      cache.control = init.control;
      cache.company = init.company;
      cache.product = init.product;
    }
    cache.result = createCacheEntry(url, cache.company, cache.product, Date.now());
    cache.revalidated = false;

    function fetchAndStoreInCache(resource, init) {
      return fetchWithProgress(resource, init).then(function (response) {
        if (!cache.enabled || cache.revalidated) {
          return response;
        }

        if (response.status === 304) {
          // Cached response is still valid. Set revalidated flag and return cached response
          cache.result.revalidated = cache.result.accessed;
          cache.revalidated = true;

          unityCache.storeRequest(cache.result).then(function () {
            log("'" + cache.result.url + "' successfully revalidated and served from the indexedDB cache");
          }).catch(function (error) {
            log("'" + cache.result.url + "' successfully revalidated but not stored in the indexedDB cache due to the error: " + error);
          });

          return new CachedResponse(cache.result.response);
        } else if (response.status == 200) {
          // New response -> Store it and cache and return it
          cache.result = createCacheEntry(
            response.url,
            cache.company,
            cache.product,
            cache.accessed,
            response
          );
          cache.revalidated = true;

          unityCache.storeRequest(cache.result).then(function () {
            log("'" + cache.result.url + "' successfully downloaded and stored in the indexedDB cache");
          }).catch(function (error) {
            log("'" + cache.result.url + "' successfully downloaded but not stored in the indexedDB cache due to the error: " + error);
          });
        } else {
          // Request failed
          log("'" + cache.result.url + "' request failed with status: " + response.status + " " + response.statusText);
        }

        return response;
      });
    }

    function sendProgressEvents(response) {
      if (init && init.onProgress) {
        init.onProgress({
          type: "progress",
          total: response.parsedBody.length,
          loaded: response.parsedBody.length,
          lengthComputable: true
        });
        init.onProgress({
          type: "load",
          total: response.parsedBody.length,
          loaded: response.parsedBody.length,
          lengthComputable: true
        });
      }
    }

    // Use fetch directly if request can't be cached
    if (!cache.enabled) {
      return fetchWithProgress(resource, init);
    }

    return unityCache.loadRequest(cache.result.url).then(function (result) {
      // Fetch resource and store it in cache if not present or cache is outdated
      if (!result || result.version !== RequestStore.version) {
        return fetchAndStoreInCache(resource, init);
      }

      cache.result = result;
      cache.result.accessed = Date.now();
      var response = new CachedResponse(cache.result.response);
      
      if (cache.control == "immutable") {
        cache.revalidated = true;
        unityCache.storeRequest(cache.result);
        log("'" + cache.result.url + "' served from the indexedDB cache without revalidation");
        sendProgressEvents(response);

        return response;
      } else if (isCrossOriginURL(cache.result.url) && (response.headers.get("Last-Modified") || response.headers.get("ETag"))) {
        return fetch(cache.result.url, { method: "HEAD" }).then(function (headResult) {
          cache.revalidated = ["Last-Modified", "ETag"].every(function (header) {
            return !response.headers.get(header) || response.headers.get(header) == headResult.headers.get(header);
          });
          if (cache.revalidated) {
            cache.result.revalidated = cache.result.accessed;
            unityCache.storeRequest(cache.result);
            log("'" + cache.result.url + "' successfully revalidated and served from the indexedDB cache");
            sendProgressEvents(response);
            
            return response;
          } else {
            return fetchAndStoreInCache(resource, init);
          }
        });
      } else {
        init = init || {};
        var requestHeaders = init.headers || {};
        init.headers = requestHeaders;
        if (response.headers.get("Last-Modified")) {
          requestHeaders["If-Modified-Since"] = response.headers.get("Last-Modified");
          requestHeaders["Cache-Control"] = "no-cache";
        } else if (response.headers.get("ETag")) {
          requestHeaders["If-None-Match"] = response.headers.get("ETag");
          requestHeaders["Cache-Control"] = "no-cache";
        }

        return fetchAndStoreInCache(resource, init);
      }
    }).catch(function (error) {
      // Fallback to regular fetch if and IndexDB error occurs
      log("Failed to load '" + cache.result.url + "' from indexedDB cache due to the error: " + error);
      return fetchWithProgress(resource, init);
    });
  }

  return cachedFetch;
}();


  function downloadBinary(urlId) {
      progressUpdate(urlId);
      var cacheControl = Module.cacheControl(Module[urlId]);
      var fetchImpl = Module.companyName && Module.productName ? Module.cachedFetch : Module.fetchWithProgress;
      var url = Module[urlId];
      var mode = /file:\/\//.exec(url) ? "same-origin" : undefined;

      var request = fetchImpl(Module[urlId], {
        method: "GET",
        companyName: Module.companyName,
        productName: Module.productName,
        control: cacheControl,
        mode: mode,
        onProgress: function (event) {
          progressUpdate(urlId, event);
        }
      });

      return request.then(function (response) {
        return response.parsedBody;
      }).catch(function (e) {
        var error = 'Failed to download file ' + Module[urlId];
        if (location.protocol == 'file:') {
          showBanner(error + '. Loading web pages via a file:// URL without a web server is not supported by this browser. Please use a local development web server to host Unity content, or use the Unity Build and Run option.', 'error');
        } else {
          console.error(error);
        }
      });
  }

  function downloadFramework() {
      return new Promise(function (resolve, reject) {
        var script = document.createElement("script");
        script.src = Module.frameworkUrl;
        script.onload = function () {
          // Adding the framework.js script to DOM created a global
          // 'unityFramework' variable that should be considered internal.
          // If not, then we have received a malformed file.
          if (typeof unityFramework === 'undefined' || !unityFramework) {
            var compressions = [['br', 'br'], ['gz', 'gzip']];
            for(var i in compressions) {
              var compression = compressions[i];
              if (Module.frameworkUrl.endsWith('.' + compression[0])) {
                var error = 'Unable to parse ' + Module.frameworkUrl + '!';
                if (location.protocol == 'file:') {
                  showBanner(error + ' Loading pre-compressed (brotli or gzip) content via a file:// URL without a web server is not supported by this browser. Please use a local development web server to host compressed Unity content, or use the Unity Build and Run option.', 'error');
                  return;
                }
                error += ' This can happen if build compression was enabled but web server hosting the content was misconfigured to not serve the file with HTTP Response Header "Content-Encoding: ' + compression[1] + '" present. Check browser Console and Devtools Network tab to debug.';
                if (compression[0] == 'br') {
                  if (location.protocol == 'http:') {
                    var migrationHelp = ['localhost', '127.0.0.1'].indexOf(location.hostname) != -1 ? '' : 'Migrate your server to use HTTPS.'
                    if (/Firefox/.test(navigator.userAgent)) error = 'Unable to parse ' + Module.frameworkUrl + '!<br>If using custom web server, verify that web server is sending .br files with HTTP Response Header "Content-Encoding: br". Brotli compression may not be supported in Firefox over HTTP connections. ' + migrationHelp + ' See <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=1670675">https://bugzilla.mozilla.org/show_bug.cgi?id=1670675</a> for more information.';
                    else error = 'Unable to parse ' + Module.frameworkUrl + '!<br>If using custom web server, verify that web server is sending .br files with HTTP Response Header "Content-Encoding: br". Brotli compression may not be supported over HTTP connections. Migrate your server to use HTTPS.';
                  }
                }
                showBanner(error, 'error');
                return;
              }
            };
            showBanner('Unable to parse ' + Module.frameworkUrl + '! The file is corrupt, or compression was misconfigured? (check Content-Encoding HTTP Response Header on web server)', 'error');
          }

          // Capture the variable to local scope and clear it from global
          // scope so that JS garbage collection can take place on
          // application quit.
          var fw = unityFramework;
          unityFramework = null;
          // Also ensure this function will not hold any JS scope
          // references to prevent JS garbage collection.
          script.onload = null;
          resolve(fw);
        }
        script.onerror = function(e) {
          showBanner('Unable to load file ' + Module.frameworkUrl + '! Check that the file exists on the remote server. (also check browser Console and Devtools Network tab to debug)', 'error');
        }
        document.body.appendChild(script);
        Module.deinitializers.push(function() {
          document.body.removeChild(script);
        });
      });
  }

  function loadBuild() {
    downloadFramework().then(function (unityFramework) {
      unityFramework(Module);
    });

    var dataPromise = downloadBinary("dataUrl");
    Module.preRun.push(function () {
      Module.addRunDependency("dataUrl");
      dataPromise.then(function (data) {
        var view = new DataView(data.buffer, data.byteOffset, data.byteLength);
        var pos = 0;
        var prefix = "UnityWebData1.0\0";
        if (!String.fromCharCode.apply(null, data.subarray(pos, pos + prefix.length)) == prefix)
          throw "unknown data format";
        pos += prefix.length;
        var headerSize = view.getUint32(pos, true); pos += 4;
        while (pos < headerSize) {
          var offset = view.getUint32(pos, true); pos += 4;
          var size = view.getUint32(pos, true); pos += 4;
          var pathLength = view.getUint32(pos, true); pos += 4;
          var path = String.fromCharCode.apply(null, data.subarray(pos, pos + pathLength)); pos += pathLength;
          for (var folder = 0, folderNext = path.indexOf("/", folder) + 1 ; folderNext > 0; folder = folderNext, folderNext = path.indexOf("/", folder) + 1)
            Module.FS_createPath(path.substring(0, folder), path.substring(folder, folderNext - 1), true, true);
          Module.FS_createDataFile(path, null, data.subarray(offset, offset + size), true, true, true);
        }
        Module.removeRunDependency("dataUrl");
      });
    });
  }

  return new Promise(function (resolve, reject) {
    if (!Module.SystemInfo.hasWebGL) {
      reject("Your browser does not support WebGL.");
    } else if (!Module.SystemInfo.hasWasm) {
      reject("Your browser does not support WebAssembly.");
    } else {
      if (Module.SystemInfo.hasWebGL == 1)
        Module.print("Warning: Your browser does not support \"WebGL 2\" Graphics API, switching to \"WebGL 1\"");
      Module.startupErrorHandler = reject;
      onProgress(0);
      Module.postRun.push(function () {
        onProgress(1);
        delete Module.startupErrorHandler;
        resolve(unityInstance);
      });
      loadBuild();
    }
  });
}
