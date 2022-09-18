#if UNITY_WEBGL && UNITY_EDITOR

using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;
using System.IO;

/// <summary>
/// Since the template is in the packages and untiy wants it to be in the Assets folder we copy it over if it does not
/// yet exists.
/// </summary>
public class WebGLTemplatePostProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("Starting to pre-process webgl template...");
        PlayerSettings.WebGL.threadsSupport = false;

        var destinationFolder = Path.GetFullPath("Assets/WebGLTemplates/SolanaWebGlTemplate");

        if (!Directory.Exists("Assets/WebGLTemplates"))
        {
            Directory.CreateDirectory("Assets/WebGLTemplates");   
        }

        if (!Directory.Exists("Assets/WebGLTemplates/SolanaWebGlTemplate"))
        {
            var sourceFolder = Path.GetFullPath("Packages/com.solana.unity_sdk/Runtime/codebase/WebGLTemplates/SolanaWebGlTemplate");

            Debug.Log($"Copying template from {sourceFolder}...");

            FileUtil.ReplaceDirectory(sourceFolder, destinationFolder);

            AssetDatabase.Refresh();

            Debug.Log($"Setting webgl template, old was = {PlayerSettings.WebGL.template}");

            PlayerSettings.WebGL.template = "PROJECT:SolanaWebGlTemplate";

            Debug.Log($"Set webgl template to {PlayerSettings.WebGL.template}");

            Debug.Log("Done pre-processing webl template...");
        }
        else
        {
            Debug.Log("Skip copy webgl template because it already exists");
        }
    }
}

#endif