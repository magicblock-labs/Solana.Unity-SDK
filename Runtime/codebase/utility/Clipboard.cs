using System.Runtime.InteropServices;
using UnityEngine;

namespace codebase.utility
{
    public static class Clipboard
    {
        public static void Copy(string message)
        {
            GUIUtility.systemCopyBuffer = message;
            var te = new TextEditor
            {
                text = message
            };
            te.SelectAll();
            te.Copy();
            #if UNITY_WEBGL && ! UNITY_EDITOR
            ExternCopyToPastebin(message);
            #endif
        }
        
        #if UNITY_WEBGL && ! UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ExternCopyToPastebin(string message);
        #endif
    }
}