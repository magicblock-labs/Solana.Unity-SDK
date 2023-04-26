using UnityEditor;
using UnityEngine;

public static class SolanaEditorUtility
{

    #region Styling Constants

    public static readonly RectOffset standardMargin = new(5, 5, 2, 2);
    public static readonly RectOffset standardPadding = new(10, 10, 2, 2);
    public static readonly RectOffset scrollViewPadding = new(15, 15, 2, 2);

    #endregion

    #region GUIStyles

    public static readonly GUIStyle propLabelStyle = new(GUI.skin.label) 
    {
        stretchWidth = false,
        alignment = TextAnchor.MiddleLeft,
        fontStyle = FontStyle.Bold,
        padding = standardPadding,
        margin = standardMargin
    };

    public static readonly GUIStyle selectButtonStyle = new(GUI.skin.button) 
    {
        stretchWidth = false,
        padding = standardPadding,
        margin = standardMargin
    };

    public static readonly GUIStyle fileSelectPathStyle = new(GUI.skin.box) 
    {
        stretchWidth = true,
        wordWrap = false,
        alignment = TextAnchor.MiddleLeft,
        padding = standardPadding,
        margin = standardMargin,
        clipping = TextClipping.Clip
    };

    public static readonly GUIStyle textFieldStyle = new(GUI.skin.textField) 
    {
        stretchWidth = true,
        alignment = TextAnchor.MiddleLeft,
        padding = standardPadding,
        margin = standardMargin
    };

    public static readonly GUIStyle scrollViewStyle = new(GUI.skin.scrollView)
    {
        padding = scrollViewPadding,
        stretchHeight = false,
    };

    #endregion

    #region Controls

    public static string RPCField(string currentRPC) 
    {
        // Layout:
        string rpc = null;
        EditorGUILayout.BeginHorizontal();
            GUILayout.Label("RPC URL", propLabelStyle);
            EditorGUILayout.TextField(currentRPC, textFieldStyle);
        EditorGUILayout.EndHorizontal();
        return rpc ?? currentRPC;
    }

    public static string FileSelectField(
        string labelText,
        string currentPath,
        string explorerTitle = null,
        string extension = null
    ) 
    {
        // Layout:
        string newPath = null;
        EditorGUILayout.BeginHorizontal();
            StaticTextProperty(labelText, currentPath);
            if (GUILayout.Button("Select", selectButtonStyle)) 
            {
                if (extension == null)
                {
                    newPath = EditorUtility.OpenFolderPanel(explorerTitle, "", "");
                } 
                else 
                {
                    newPath = EditorUtility.OpenFilePanel(explorerTitle, "", extension);
                }
            }
        EditorGUILayout.EndHorizontal();
        return newPath ?? currentPath;
    }

    public static void StaticTextProperty(string label, string value) 
    {
        EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, propLabelStyle);
            GUILayout.Box(value, fileSelectPathStyle);
        EditorGUILayout.EndHorizontal();
    }

    #endregion
}
