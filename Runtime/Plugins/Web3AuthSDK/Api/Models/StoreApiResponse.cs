using System;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class StoreApiResponse {
    [Preserve]
    public string message { get; set; }
    [Preserve]
    public bool success { get; set; }
}
