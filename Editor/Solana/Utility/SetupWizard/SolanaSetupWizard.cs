using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    /// <summary>
    /// An EditorWindow which can be used to create <see cref="ConfigurableObject"/>s.
    /// </summary>
    /// <typeparam name="SetupObject">The type of object being created.</typeparam>
    internal abstract class SolanaSetupWizard<SetupObject> : EditorWindow where SetupObject : ConfigurableObject
    {

        #region Properties

        protected abstract string SavePath { get; }

        #endregion

        #region Fields

        private int questionIndex = 0;

        /// <summary>
        /// The target object being created by this window.
        /// </summary>
        private protected SerializedObject target;

        /// <summary>
        /// The questions to display during setup.
        /// </summary>
        private FieldInfo[] questions;

        private Vector2 scrollPosition;

        #endregion

        #region Unity Messages

        protected void OnEnable()
        {
            var targetInstance = CreateInstance<SetupObject>();
            target = new(targetInstance);
            TypeInfo typeInfo = typeof(SetupObject).GetTypeInfo();
            questions = typeInfo.DeclaredFields.ToArray();
        }

        protected void OnGUI()
        {
            RenderCurrentQuestion();
            NavigationControls();
        }

        #endregion

        #region Protected

        /// <summary>
        /// Displays the fields for the current question.
        /// </summary>
        private protected virtual void RenderCurrentQuestion()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                var questionName = questions[questionIndex];
                var question = questionName.GetCustomAttribute<SetupQuestionAttribute>();
                SolanaEditorUtility.Heading(question.question, TextAnchor.UpperCenter);
                EditorGUILayout.BeginVertical(MetaplexEditorUtility.answerFieldStyle);
                {
                    var serializedProperty = target.FindProperty(questionName.Name);
                    EditorGUILayout.PropertyField(serializedProperty);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Called when the user clicks the "Finish" button at the end of the setup wizard.
        /// </summary>
        private protected virtual void OnWizardFinished()
        {
            var filePath = EditorUtility.SaveFilePanel("Save Config File", SavePath, "config", "asset");
            filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            AssetDatabase.CreateAsset(target.targetObject, filePath);
            AssetDatabase.SaveAssets();
            Close();
        }

        #endregion

        #region Private

        private void NavigationControls()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(questionIndex == 0);
                {
                    if (GUILayout.Button("Back")) {
                        questionIndex--;
                        questionIndex = Mathf.Max(0, questionIndex);
                        GUI.FocusControl(null);
                        target.ApplyModifiedProperties();
                    }
                }
                EditorGUI.EndDisabledGroup();
                if (questionIndex < questions.Length - 1) {
                    if (GUILayout.Button("Next")) {
                        questionIndex++;
                        GUI.FocusControl(null);
                        target.ApplyModifiedProperties();
                    }
                }
                else {
                    if (GUILayout.Button("Finish")) {
                        GUI.FocusControl(null);
                        target.ApplyModifiedProperties();
                        target.ApplyModifiedProperties();
                        var targetObject = (SetupObject)target.targetObject;
                        if (targetObject.IsValidConfiguration) {
                            OnWizardFinished();
                        } else {
                            Debug.LogError("Configuration is invalid.");
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}
