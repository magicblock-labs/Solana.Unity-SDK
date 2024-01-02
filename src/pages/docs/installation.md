---
title: Installation
description: Solana.Unity-SDK - Installation
---

## Video Tutorial

Here's a quick tutorial on how to setup the Unity SDK.

{% video src="https://www.youtube.com/embed/D0uk6oDVezM" /%}



https://www.youtube.com/watch?v=D0uk6oDVezM

## Unity

Go [here](https://unity.com/download) to install Unity.

## Import the SDK package

* Open [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) window.
* Click the add **+** button in the status bar.
* The options for adding packages appear.
* Select Add package from git URL from the add menu. A text box and an Add button appear.
* ![Package manager](/package_manager.png)
* Enter the `https://github.com/magicblock-labs/Solana.Unity-SDK.git` Git URL in the text box and click Add.
* Once the package is installed, in the Package Manager inspector you will have Samples. Click on Import
* You may also install a specific package version by using the URL with the specified version.
    * `https://github.com/magicblock-labs/Solana.Unity-SDK.git#X.Y.X`
    * Please note that the version `X.Y.Z` stated here is to be replaced with the version you would like to get.
    * You can find all the available releases [here](https://github.com/magicblock-labs/Solana.Unity-SDK/releases).
    * The latest available release version is [![Last Release](https://img.shields.io/github/v/release/magicblock-labs/Solana.Unity-SDK)](https://github.com/magicblock-labs/Solana.Unity-SDK/releases/latest)
* Import the Sample Scene
* ![Import sample](/import_sample.png)
* You will find a sample scene with a configured wallet in `Samples/Solana SDK/0.0.2/Simple Wallet/Solana Wallet/scenes/wallet_scene.unity`

{% callout title="You should know!" %}
This tutorial is made in Unity 2021.3.5f1
{% /callout %}
