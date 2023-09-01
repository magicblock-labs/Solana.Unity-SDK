using System;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    [AttributeUsage(AttributeTargets.Field)]
    public class SetupQuestionAttribute : Attribute
    {

        public string question;

        public SetupQuestionAttribute(string question)
        {
            this.question = question;
        }
    }
}
