using System;
using UnityEditor;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
	/// <summary>
	/// Attribute used to show or hide the Field depending on a boolean property.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class ShowWhenAttribute : PropertyAttribute
	{

		public readonly string conditionFieldName;

		public ShowWhenAttribute(string conditionFieldName)
		{
			this.conditionFieldName = conditionFieldName;
		}
    }

    [CustomPropertyDrawer(typeof(ShowWhenAttribute))]
    public class ShowWhenDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowWhenAttribute attribute = (ShowWhenAttribute)this.attribute;
            int place = property.propertyPath.LastIndexOf(property.name);
            var propertyPath = place == -1 ? property.propertyPath : property.propertyPath.Remove(
                place, 
                property.name.Length
            ).Insert(
                place, 
                attribute.conditionFieldName
            );
            SerializedProperty conditionField = property.serializedObject.FindProperty(propertyPath);

            if (conditionField == null) 
            {
                ShowError(position, label, "Error getting the condition Field. Check the name.");
                return;
            }
            bool showField = conditionField.boolValue;
            if (showField)
                EditorGUI.PropertyField(position, property, label, true);              
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowWhenAttribute attribute = (ShowWhenAttribute)this.attribute;
            int place = property.propertyPath.LastIndexOf(property.name);
            var propertyPath = place == -1 ? property.propertyPath : property.propertyPath.Remove(
                place,
                property.name.Length
            ).Insert(
                place,
                attribute.conditionFieldName
            );
            SerializedProperty conditionField = property.serializedObject.FindProperty(propertyPath);
            bool showField = conditionField.boolValue;
            if (showField)
                return EditorGUI.GetPropertyHeight(property);
            else
                return -EditorGUIUtility.standardVerticalSpacing;
        }

        private void ShowError(Rect position, GUIContent label, string errorText)
        {
            EditorGUI.LabelField(position, label, new GUIContent(errorText));
        }

    }
}
