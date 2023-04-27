using Solana.Unity.Metaplex.Candymachine.Types;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{

    internal class CandyMachineCreator : SolanaSetupWizard<CandyMachinePropertyKey>
    {

        #region Properties

        private readonly WizardQuestion<CandyMachinePropertyKey>[] configSetupQuestions = new WizardQuestion<CandyMachinePropertyKey>[] {
            new (CandyMachinePropertyKey.String ,"How many NFTs will you have in your candy machine?", "0"),
            new (CandyMachinePropertyKey.EndSettings ,"End settings", new EndSettings())
        };

        #endregion

        #region Unity Messages

        private void OnEnable()
        {
            questions = configSetupQuestions;
        }

        #endregion

        #region SolanaSetupWizard

        private protected override void OnQuestionAnswered(WizardQuestion<CandyMachinePropertyKey> question)
        {
            var answer = question.Answer();
            var s = "";
            var settings = new EndSettings();
            switch (question.key) {
                case CandyMachinePropertyKey.String:
                    s = (string)answer;
                    break;
                case CandyMachinePropertyKey.EndSettings:
                    settings = (EndSettings)answer;
                    break;
            }
        }

        private protected override void OnWizardFinished()
        {
            // Create config file
        }

        private protected override void RenderQuestion(WizardQuestion<CandyMachinePropertyKey> question)
        {
            switch (question.key) {
                case CandyMachinePropertyKey.EndSettings:
                    EndSettings(question);
                    break;
                case CandyMachinePropertyKey.String:
                    StringQuestion(question);
                    break;
            }
        }

        #endregion

        #region Questions

        private EndSettings EndSettings(WizardQuestion<CandyMachinePropertyKey> question)
        {
            var settings = (EndSettings)question.answer;
            SolanaEditorUtility.Heading(question.question, TextAnchor.UpperCenter);
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Type", SolanaEditorUtility.propLabelStyle);
                    settings.EndSettingType = (EndSettingType)EditorGUILayout.EnumPopup(settings.EndSettingType);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Value", SolanaEditorUtility.propLabelStyle);
                    settings.Number = (ulong)EditorGUILayout.IntField((int)settings.Number);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            return settings;
        }

        private string StringQuestion(WizardQuestion<CandyMachinePropertyKey> question)
        {
            var answer = (string)question.answer;
            SolanaEditorUtility.Heading(question.question, TextAnchor.UpperCenter);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            answer = EditorGUILayout.TextField(answer, SolanaEditorUtility.answerFieldStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return answer;
        }

        #endregion
    }

    internal enum CandyMachinePropertyKey
    {
        EndSettings,
        String
    }
}
