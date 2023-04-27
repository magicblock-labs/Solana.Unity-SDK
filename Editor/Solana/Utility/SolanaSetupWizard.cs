using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    public abstract class SolanaSetupWizard : EditorWindow
    {

        #region Types

        private protected class WizardQuestion
        {
            #region Properties

            private readonly string question;
            private string answer;

            #endregion

            #region Constructors

            internal WizardQuestion(string question, string answer)
            {
                this.question = question;
                this.answer = answer;
            }

            #endregion

            #region Internal

            internal void Render()
            {
                SolanaEditorUtility.Heading(question, TextAnchor.UpperCenter);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                answer = EditorGUILayout.TextField(answer, SolanaEditorUtility.answerFieldStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            #endregion
        }

        #endregion

        #region Properties

        private int currentQuestion = 0;
        private protected abstract WizardQuestion[] Questions { get; }

        #endregion

        #region Unity Messages

        protected void OnGUI()
        {
            Questions[currentQuestion].Render();
            EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(currentQuestion == 0);
                    if (GUILayout.Button("Back")) {
                        currentQuestion--;
                        currentQuestion = Mathf.Max(0, currentQuestion);
                    }
                EditorGUI.EndDisabledGroup();
                if (currentQuestion < Questions.Length - 1) {
                    if (GUILayout.Button("Next")) {
                        currentQuestion++;
                    }
                } else {
                    if (GUILayout.Button("Finish")) {
                        OnWizardFinished();
                    }
                }
                GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Protected

        private protected abstract void OnWizardFinished();

        #endregion
    }
}
