using System;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    public abstract class SolanaSetupWizard<KeyType> : EditorWindow where KeyType : Enum
    {

        #region Types

        private protected class WizardQuestion<Key> where Key: Enum
        {
            #region Properties

            internal Key key;
            internal object answer;
            internal readonly string question;

            #endregion

            #region Constructors

            internal WizardQuestion(Key key, string question, object answer)
            {
                this.key = key;
                this.question = question;
                this.answer = answer;
            }

            #endregion

            #region Internal

            internal protected object Answer()
            {
                return answer;
            }

            #endregion
        }

        #endregion

        #region Properties

        private int questionIndex = 0;
        private protected WizardQuestion<KeyType>[] questions;

        #endregion

        #region Unity Messages

        protected void OnGUI()
        {
            RenderQuestion(questions[questionIndex]);
            EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(questionIndex == 0);
                    if (GUILayout.Button("Back")) {
                        questionIndex--;
                        questionIndex = Mathf.Max(0, questionIndex);
                    }
                EditorGUI.EndDisabledGroup();
                if (questionIndex < questions.Length - 1) {
                    if (GUILayout.Button("Next")) {
                        OnQuestionAnswered(questions[questionIndex]);
                        questionIndex++;
                    }
                } else {
                    if (GUILayout.Button("Finish")) {
                        OnQuestionAnswered(questions[questionIndex]);
                        OnWizardFinished();
                    }
                }
                GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Protected

        private protected abstract void RenderQuestion(WizardQuestion<KeyType> question);
        private protected abstract void OnWizardFinished();
        private protected abstract void OnQuestionAnswered(WizardQuestion<KeyType> prevQuestion);

        #endregion
    }
}
