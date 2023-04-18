#if UNITY_WEBGL && UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;



// <summary>
// This script exports Prefabs from the package to the Resources folder of the Unity project
// so they can be accessed via Resources.Load method.
// </summary>

namespace Solana.Unity.SDK.Exporters
{
    public class PrefabsExporter
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var destinationRootFolder = Path.GetFullPath("Assets/Resources/SolanaUnitySDK/");
            var sourceRootFolder = Path.GetFullPath("Packages/com.solana.unity_sdk/Runtime/codebase/Prefabs/");

            if (!Directory.Exists(destinationRootFolder))
            {
                Directory.CreateDirectory(destinationRootFolder);
            }

            // Iterate through all the prefabs in Packages/com.solana.unity_sdk/Runtime/codebase/Prefabs
            // and copy them over to Assets/Resources/SolanaUnitySDK
            foreach (var prefabPath in Directory.GetFiles(sourceRootFolder, "*.prefab"))
            {
                var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                var destinationPath = Path.Combine(destinationRootFolder, prefabName + ".prefab");

                if (!File.Exists(destinationPath))
                {
                    Debug.Log($"Copying prefab from {prefabPath} to {destinationPath}");
                    FileUtil.CopyFileOrDirectory(prefabPath, destinationPath);
                    AssetDatabase.Refresh();
                }

            }
        }
    }
}

#endif

