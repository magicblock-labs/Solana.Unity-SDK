mergeInto(LibraryManager.library, {

    InitWeb3Auth: async function (clientID) {
        // Add Web3Auth from CDN
        if(window.Core == undefined){
            console.log("Adding JS")
            var script = document.createElement("script");
            script.src = "https://cdn.jsdelivr.net/npm/@web3auth/core";
            document.head.appendChild(script);
            script = document.createElement("script");
            script.src = "https://cdn.jsdelivr.net/npm/@web3auth/openlogin-adapter";
            document.head.appendChild(script);
        }
        console.log("A");
        console.log(window.Core);
        console.log(UTF8ToString(clientID));
        
        if(this.web3AuthInstance == undefined && window.Core != undefined && window.Core.Web3AuthCore != undefined){
            this.initialized = false;
            this.web3AuthInstance = new window.Core.Web3AuthCore({
              chainConfig: {
                chainNamespace: "solana",
                rpcTarget: "https://rpc.ankr.com/solana",
                chainId: "0x1",
              },
              clientId: UTF8ToString(clientID),
            });
            
            console.log("B");
            
            const openloginAdapter = new window.OpenloginAdapter.OpenloginAdapter({
              adapterSettings: {
                network: "testnet",
                uxMode: "popup",
              },
            });
            
            console.log("C");
            
            this.web3AuthInstance.configureAdapter(openloginAdapter);
            
            console.log("D");
    
            await this.web3AuthInstance.init();
            
            this.initialized = true;
            
            if(this.web3AuthInstance != undefined && this.initialized == true && this.doLogin == true){
              this.doLogin = false;
              console.log("Open login after init");
              const web3authProvider = await this.web3AuthInstance.connectTo(
                //window.WALLET_ADAPTERS.OPENLOGIN,
                "openlogin",
                {
                  loginProvider: 'google',
                },
              );
              console.log("provider after login", web3authProvider);
            }
            
            console.log("E");
            
            console.log("web3AuthInstance", this.web3AuthInstance, this.web3AuthInstance.provider);
            
            if (this.web3AuthInstance.provider != undefined){
                const privateKey = await this.web3AuthInstance.provider.request({
                  method: "solanaPrivateKey",
                });
                console.log(privateKey as string);
            }
        }
    },
    
    LoginWeb3Auth: async function () {
        console.log("Start login");
        try {
          console.log(this.web3AuthInstance);
          console.log(this.initialized);
          this.doLogin = true;
          if(this.web3AuthInstance != undefined && this.initialized == true){
              this.doLogin = false;
              console.log("Open login");
              const web3authProvider = await this.web3AuthInstance.connectTo(
                //window.WALLET_ADAPTERS.OPENLOGIN,
                "openlogin",
                {
                  loginProvider: 'google',
                },
              );
              console.log("provider after login", web3authProvider);
          }
        } catch (error) {
          console.error(error.message);
        }
    },

});
