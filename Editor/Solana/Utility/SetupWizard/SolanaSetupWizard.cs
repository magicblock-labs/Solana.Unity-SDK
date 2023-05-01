using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    public abstract class SolanaSetupWizard<SetupObject> : EditorWindow where SetupObject : ScriptableObject
    {

        #region Types

        /// <summary>
        /// Used to store questions along with their designated fields.
        /// </summary>
        private struct SetupQuestion
        {
            #region Properties

            internal string question;
            internal List<string> properties;

            #endregion

            #region Constructors

            public SetupQuestion(string question)
            {
                this.question = question;
                this.properties = new List<string>();
            }

            #endregion
        }

        #endregion

        #region Properties

        /// <summary>
        /// The index of the question being displayed.
        /// </summary>
        private int questionIndex = 0;

        /// <summary>
        /// The target object being created by this window.
        /// </summary>
        private protected SerializedObject target;

        /// <summary>
        /// The questions to display during setup.
        /// </summary>
        private readonly List<SetupQuestion> questions = new();

        #endregion

        #region Unity Messages

        protected void OnEnable()
        {
            var targetInstance = CreateInstance<SetupObject>();
            target = new(targetInstance);
            GetQuestions();
        }

        protected void OnGUI()
        {
            RenderCurrentQuestion();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(questionIndex == 0);
                {
                    if (GUILayout.Button("Back")) 
                    {
                        questionIndex--;
                        questionIndex = Mathf.Max(0, questionIndex);
                        GUI.FocusControl(null);
                        target.ApplyModifiedProperties();
                    }
                }
                EditorGUI.EndDisabledGroup();
                if (questionIndex < questions.Count - 1) 
                {
                    if (GUILayout.Button("Next")) 
                    {
                        questionIndex++;
                        GUI.FocusControl(null);
                        target.ApplyModifiedProperties();
                    }
                }
                else 
                {
                    if (GUILayout.Button("Finish")) 
                    {
                        target.ApplyModifiedProperties();
                        OnWizardFinished();
                    }
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Protected

        /// <summary>
        /// Displays the fields for the current question.
        /// </summary>
        private protected virtual void RenderCurrentQuestion()
        {
            var question = questions[questionIndex];
            SolanaEditorUtility.Heading(question.question, TextAnchor.UpperCenter);
            foreach (var prop in question.properties) 
            {
                var serializedProperty = target.FindProperty(prop);
                EditorGUILayout.PropertyField(serializedProperty);
            }
        }

        /// <summary>
        /// Called when the user clicks the "Finish" button at the end of the setup wizard.
        /// </summary>
        private protected abstract void OnWizardFinished();

        #endregion

        #region Private

        /// <summary>
        /// Reads all of the <see cref="SetupQuestionAttribute"/>s in the current target and their respective
        /// fields.
        /// </summary>
        private void GetQuestions()
        {
            TypeInfo typeInfo = typeof(SetupObject).GetTypeInfo();
            SetupQuestion? currentQuestion = null;
            var fields = typeInfo.DeclaredFields;
            foreach (var prop in fields) 
            {
                var newQuestion = prop.GetCustomAttribute<SetupQuestionAttribute>();
                if (newQuestion != null) 
                {
                    currentQuestion = new(newQuestion.question);
                    questions.Add(currentQuestion.Value);
                }
                if (currentQuestion.HasValue) 
                {
                    currentQuestion.Value.properties.Add(prop.Name);
                }
            }
        }

        #endregion
    }
}
