---
title: Publish your game as an Xnft
description:
---

## Build your project as a xNFT compatible WebGL

- Open [Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html) window.
- Change to WebGL platform if you haven't already:
  - Select WebGL as the platform
  - Click the **Switch Platform** button to apply changes.
    ![Switch Platform](/switch_platform.png)
- When your Unity project is set to WebGL Platform, the SDK automagically imports a new WebGL Template into the /Assets/WebGLTemplate folder, named xNFT:
  ![XNFT Template Imported](/xnft_webgl_template_imported.png)
- Open **Player Settings** window from the Build Settings, and select the xNFT template.
  ![Select xNFT Template](/select_xnft_template.png)

  Congrats! You can now build your webgl project normally and just upload the generated files to your web server as any other WebGL game. You need the url for publishing the xnft in next step.

## Deploy your game as an xNFT

As the time of writing this, you need to own a invite code to access backpack.

You also need to ask for the deploy password in the [official backpack developer's discord](https://discord.gg/y6wYRN73) as it's still in very early access.

After that you can just go to [https://www.xnft.gg/publish](https://www.xnft.gg/publish) to deploy your xnft in mainnet-beta or [https://devnet.xnft.gg/publish](https://devnet.xnft.gg/publish) to deploy the xnft in devnet. Connect you wallet and then follow the instructions:

- Select Storage Option: First you need to select which storage service you want to use to store your xNFT:
  ![Select XNFT Storage](/select_xnft_storage.png)
- Write your web server's url (the one you deployed the webgl project) and hit next to deploy your xnft game using the Iframe Source Url option.
  ![Iframe Source](/select_iframe_source.png)
- Then you just need to fill in the details , review them and mint!

Congrats! You're done publishing your game as an xNFT!
