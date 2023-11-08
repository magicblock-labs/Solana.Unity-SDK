using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_WEBGL && UNITY_EDITOR

/// Inspired by Author: Jonas Hahn, Source: https://github.com/Woody4618/Solana.Unity-SDK/blob/main/Runtime/codebase/WebGLTemplatePostProcessor.cs

/// <summary>
/// Since the template is in the packages and Unity wants it to be in the Assets folder we copy it over if it does not
/// yet exists.
/// </summary>

// When UnityEditor.Callbacks.DidReloadScriptsDidReloadScripts we import the WebGL template
// This is needed because the WebGL template is in the package and unity wants it to be in the Assets folder
// So we copy it over if it does not yet exists


// ReSharper disable once CheckNamespace

public static class WebGLTemplatesExporter {
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        var destinationRootFolder = Path.GetFullPath("Assets/WebGLTemplates/");
        var sourceRootFolder = Path.GetFullPath("Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates/");

        if (!Directory.Exists("Assets/WebGLTemplates"))
        {
            Directory.CreateDirectory("Assets/WebGLTemplates");   
        }

        // Iterate trough all the template folders in Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates/ and copy them over to Assets/WebGLTemplates
        foreach (var templateFolder in Directory.GetDirectories("Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates"))
        {
            var templateName = Path.GetFileName(templateFolder);
            var sourceFolder = Path.Combine(sourceRootFolder, templateName);
            var destinationFolder = Path.Combine(destinationRootFolder, templateName);
            
            if(!Directory.Exists(destinationFolder))
            {
                Debug.Log($"Copying template from {sourceFolder} to {destinationFolder}");
                FileUtil.CopyFileOrDirectory(sourceFolder, destinationFolder);
                AssetDatabase.Refresh();
                Debug.Log($"Setting webgl template, old was = {PlayerSettings.WebGL.template}");
            }

        }

    }

}

#endif

#if UNITY_EDITOR
public static class SetDefaultSplashScreen
{
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // Set Default Splash Screen
        if (PlayerSettings.SplashScreen.logos.Length > 0 && PlayerSettings.SplashScreen.logos[0].logo == null)
            PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[] { };
        if (PlayerSettings.SplashScreen.logos != null && PlayerSettings.SplashScreen.logos.Length != 0) return;
        Texture2D logoTexture = Resources.Load<Texture2D>("magicblock-logo");
        Texture2D backgroundTexture = Resources.Load<Texture2D>("background");
        if (logoTexture != null)
        {
            var logo = new PlayerSettings.SplashScreenLogo();
            Sprite logoSprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), Vector2.zero);
            logo.logo = logoSprite;
            logo.duration = 2;//asd
            
            var logos = new List<PlayerSettings.SplashScreenLogo>(PlayerSettings.SplashScreen.logos) { logo };
            PlayerSettings.SplashScreen.logos = logos.ToArray();
                
            if (backgroundTexture != null && PlayerSettings.SplashScreen.background == null)
            {
                Sprite backgroundSprite = Sprite.Create(backgroundTexture, new Rect(0, 0, backgroundTexture.width, backgroundTexture.height),
                    Vector2.zero);
                PlayerSettings.SplashScreen.background = backgroundSprite;
            }
        }
    }

}

#endif
