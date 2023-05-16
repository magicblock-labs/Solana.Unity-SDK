---
title: Host your game on Github pages
description: Host your game for free using Github pages
---

Solana.Unity SDK is fully compatible with WebGL. In this tutorial you will compile the Solana.Unity-SDK [demo scene](https://garbles-labs.github.io/Solana.Unity-SDK/) and publish it using [Github pages](https://pages.github.com/).

With GitHub pages, GitHub allows you to host a webpage from your repository.

## Compile the game to WebGL

1. Download and Install [Unity](https://unity3d.com/get-unity/download)
2. Install the Solana.Unity-SDK following the [instructions](https://github.com/garbles-labs/Solana.Unity-SDK#installation) and import the example
4. Compile the scene to WebGL (be sure to [disable compression](https://www.youtube.com/watch?v=2jjESP58jsA), as GH pages does not support serving compressed files)
   ![WebGL](/webgl.png)

{% callout title="Skip the compilation step" %}
If you want to skip the compilation step, you can fork the SDK repository, which contained a pre-compiled WebGL build in the [gp-pages](https://github.com/garbles-labs/Solana.Unity-SDK/tree/gh-pages) branch 
{% /callout %}

## Host the demo on Github pages

- Create a new repository
- Navigate to the build folder containing the index.html

```shell
git init
git add .
git commit -m "WebGL game"
git remote add origin <remote_repo_url>
git push origin <branch>
```

- You repository should now looks similar to the SDK [gp-pages](https://github.com/garbles-labs/Solana.Unity-SDK/tree/gh-pages) branch.
- Enable gh-pages deployment from the repository settings
![gh-pages](/gh-pages-deply.png)

Github will provide a url for the live deployment: [garbles-labs.github.io/Solana.Unity-SDK](https://garbles-labs.github.io/Solana.Unity-SDK/)

{% callout title="Custom domain" %}
Learn how to setup a [custom domain](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site) on Github pages
{% /callout %}

{% callout title="Publish your game as xNFT" %}
Follow [this guide](/docs/xnft#deploy-your-game-as-an-x-nft) to publish your game as an xNFT in less than 2 minutes.
{% /callout %}


---

Happy game development 🎈 and don't forget to ⭐ the repo

