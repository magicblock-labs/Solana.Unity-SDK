#if UNITY_WEBGL && UNITY_EDITOR

using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.Build.Reporting;
using UnityEditor;
using System.IO;

/// Inspired by Author: Jonas Hahn, Source: https://github.com/Woody4618/Solana.Unity-SDK/blob/main/Runtime/codebase/WebGLTemplatePostProcessor.cs

/// <summary>
/// Since the template is in the packages and untiy wants it to be in the Assets folder we copy it over if it does not
/// yet exists.
/// </summary>

// When UnityEditor.Callbacks.DidReloadScriptsDidReloadScripts we import the WebGL template
// This is needed because the WebGL template is in the package and unity wants it to be in the Assets folder
// So we copy it over if it does not yet exists

public class WebGLTemplatesExporter {
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        Debug.Log("Starting to pre-process webgl template...");

        var destinationRootFolder = Path.GetFullPath("Assets/WebGLTemplates/");
        var sourceRootFolder = Path.GetFullPath("Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates/");

        if (!Directory.Exists("Assets/WebGLTemplates"))
        {
            Directory.CreateDirectory("Assets/WebGLTemplates");   
        }

        // Iterate trhough all the temaplate folders in Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates/ and copy them over to Assets/WebGLTemplates
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
            else
            {
                Debug.Log($"Template {templateName} already exists in {destinationFolder}");
            }

        }
    }
    
}

#endif