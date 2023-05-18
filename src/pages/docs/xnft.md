---
title: Publish your game as an Xnft
description:
---

## Compile the game for xNFT
You can now just compile your game to WebGL without any extra steps. Just make sure you have the latest version of the SDK and you're good to go.

Deploy your game as a normal WebGL game as you will need the url for publishing the xNFT in next step.

{% callout title="One build to rule them all" %}
Your WebGL game will work both in the browser and inside Backpack, no need to build and host a separate version for each platform.
{% /callout %}

{% callout title="Host your game on Github pages" %}
Follow [this guide](/docs/gh-pages) to compile your game to WebGL  and host the build on Github pages
{% /callout %}

### (Optional) Use the xNFT WebGL Template
A customized WebGL Template is also provided for easier deployment of your game as an xNFT. It's not required, but it's recommended as it makes the game fully responsive to the screen size, and it will just look good when running in both the browser and as a xNFT app.

To use the xNFT WebGL Template, follow these steps:

- Open [Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html) window and change to WebGL platform if you haven't already:
  - Select WebGL as the platform
  - Click the **Switch Platform** button to apply changes.
    ![Switch Platform](/xnft/switch_platform.png)

When your Unity project is set to WebGL Platform, the SDK automagically imports a new WebGL Template into the /Assets/WebGLTemplate folder, named xNFT:

  ![XNFT Template Imported](/xnft/xnft_webgl_template_imported.png)

- Open **Player Settings** window from the Build Settings, and select the xNFT template.

  ![Select xNFT Template](/xnft/select_xnft_template.png)

Now you can build your game and host it as a normal WebGL game.

## Publish your game as an xNFT
If you haven't already, go get your Backpack user. You can download Backpack [here](https://www.backpack.app/downloads).
After you have your game hosted in a webserver and you have Backpack account, you can just go to [https://www.xnft.gg/publish](https://www.xnft.gg/publish) to deploy your xnft in mainnet-beta or [https://devnet.xnft.gg/publish](https://devnet.xnft.gg/publish) to deploy the xnft in devnet. 

Connect your Backpack wallet and then follow the instructions:
- copy this basic [xnft.json](https://github.com/coral-xyz/xnft-quickstart/blob/master/xnft.json) configuration file.
- edit the "entrypoints" "default" "web" to point to your game's url
- edit xnft.json (add a "tag": "game" line) and add your app's icon and screenshots in an Assets folder.
- Zip togehter the xnft.json file and the Assets folder.  
- Go to [xnft.gg/publish](https://www.xnft.gg/publish) 
- Drop the zipped Manifest and mint. 

  ![Drop manifest](/xnft/drop_manifest.png)

  Congrats! You're done publishing your game as an xNFT!

## Video Demo
Here's a quick demo on how you can build your game and test it in the browser and as an xNFT app inside Backpack.

[![Xnft Demo](/xnft/XnftTutorial.gif)](/xnft/XnftTutorial.mp4)




