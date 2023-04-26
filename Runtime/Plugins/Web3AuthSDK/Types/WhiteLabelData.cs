using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

[Serializable]
[Preserve]
public class WhiteLabelData { 
    public string name { get; set; }
    public string logoLight { get; set; }
    public string logoDark { get; set; }
    public string defaultLanguage { get; set; } = "en";
    public bool dark { get; set; } = false;
    public Dictionary<string, string> theme { get; set; }

    [Preserve]
    public WhiteLabelData()
    {
    }
}