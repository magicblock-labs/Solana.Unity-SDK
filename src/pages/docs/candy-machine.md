---
title: Candy Machine
description: 
---
This documentation refers to the CandyMachine Editor Tools, and runtime library. Before continuing, you should familiarize yourself with [The Metaplex CandyMachine Program](https://docs.metaplex.com/programs/candy-machine/).

## Editor Tools
The CandyMachine editor tool is designed as an alternative to Metaplex's [Sugar CLI](https://docs.metaplex.com/developer-tools/sugar/). The following functionality is currently supported:

- Creating, importing, and exporting configuration files
- Uploading asset files to Arweave via Bundlr
- [CandyGuard](https://docs.metaplex.com/programs/candy-machine/candy-guards) configuration
- Deployment
- Minting (with & without guards)
- Freeze, Unlock Funds, Thaw
- Withdraw
- Reveal
- Sign

Currently Unsupported:
- pNFTs

### Upload Methods
The upload API is designed such that users can easily add their own upload methods. There are 2 ways of creating a custom upload method:

- Implement the `IMetaplexAssetUploader` Interface to upload assets one-by-one.
- Implement a subclass of `MetaplexParallelAssetUploader`, a convenience class that provides a basic implementation for uploading multiple assets in one action.

### Tutorial

#### 1.Opening the Editor tool
The CandyMachine Editor tool can be found in the action bar under Solana > Metaplex > CandyMachine
![Editor Tool Location](/CandyMachine/1.png)

#### 2.Settings Configuration
Before using the tool, you first need to configure your settings:
- Keypair (defaults to .config/id.json): A valid Solana keypair stored in a JSON file.
- Config Location: The folder in which the tool should look for configuration files.
- RPC URL: A valid Solana RPC URL.
![Editor Tool Settings](/CandyMachine/2.png)

#### 3. Config Creation
To create a new CandyMachine configuration, select "Create New CandyMachine" and follow the setup wizard. For queries about configratuon values, see the [Metaplex Docs](https://docs.metaplex.com/developer-tools/sugar/reference/configuration).
![Editor Tool Settings](/CandyMachine/3.png)

#### 4. Uploading Assets
Once you've created your CandyMachine configuration, your CandyMachine will appear under `Existing CandyMachines`. To begin uploading assets, select `Upload`.
![Editor Tool Settings](/CandyMachine/4.png)

Before selecting your asset folder, you will be prompted to save your Cache file.
![Editor Tool Settings](/CandyMachine/5.png)

After saving your Cache, select your assets folder. Assets should be formatted according to the [Metaplex Documentation](https://docs.metaplex.com/programs/candy-machine/inserting-items).

You can track your upload progress in the console.

#### 5. Deployment & Usage
Once you have successfully uploaded your assets, you can select `Deploy` to deploy your CandyMachine. Deployment progress can be tracked in the console.

Once deployed, you will be able to execute supported CandyMachine commands from the Editor.
![Editor Tool Settings](/CandyMachine/6.png)

## Runtime Libaries
The runtime library provides convenience methods for executing CandyMachine commands during runtime, such as minting, or revealing.

All CandyMachine runtime commands can be found in the `CandyMachineCommands` static class.
