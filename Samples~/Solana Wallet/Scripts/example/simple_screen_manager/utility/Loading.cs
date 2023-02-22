using UnityEngine;

// ReSharper disable once CheckNamespace

public class Loading : MonoBehaviour
{
    private static GameObject _loadingSpinner;
    
    public static void StartLoading()
    {
        _loadingSpinner ??= GameObject.Find("Loading");
        if(_loadingSpinner != null)
            _loadingSpinner.transform.GetChild(0)?.gameObject.SetActive(true);
    }
    
    public static void StopLoading()
    {
        _loadingSpinner ??= GameObject.Find("Loading");
        if(_loadingSpinner != null)
            _loadingSpinner.transform.GetChild(0)?.gameObject.SetActive(false);
    }
}
