using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameController : MonoBehaviour
{
    private const string RepoUrl = "https://github.com/garbles-labs/Solana.Unity-SDK";
    
    public void OpenSDKRepo()
    {
        Application.OpenURL(RepoUrl);
    }
    
}
