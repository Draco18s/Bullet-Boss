using Assets.draco18s.util;
using Assets.draco18s;
using UnityEditor;
using UnityEngine;
using Assets.draco18s.ui;
using Unity.VisualScripting;

[CustomPropertyDrawer(typeof(PatternData))]
public class PatternEditor : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		position.height = EditorGUIUtility.singleLineHeight;

		property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
		if (property.GetUnderlyingValue() == null || !property.isExpanded)
		{
			return;
		}
		position.x += 14;
		position.width -= 14;
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("isEditable"), new GUIContent("Editable"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("image"), new GUIContent("Sprite"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("ReloadType"), new GUIContent("Reload Type"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("Lifetime"), new GUIContent("Lifetime"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("StartAngle"), new GUIContent("Start Angle"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("TimeOffset"), new GUIContent("Time Offset"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		SerializedProperty dataProp = property.FindPropertyRelative("dataValues");
		EditorGUI.PropertyField(position, dataProp, new GUIContent("Data Values"), true);
		position.y += EditorGUI.GetPropertyHeight(dataProp) + EditorGUIUtility.standardVerticalSpacing;
		
		SerializedProperty timeProp = property.FindPropertyRelative("timeline");
		EditorGUI.PropertyField(position, timeProp, new GUIContent("Timeline"), true);
		position.y += EditorGUI.GetPropertyHeight(timeProp) + EditorGUIUtility.standardVerticalSpacing;

		SerializedProperty childProp = property.FindPropertyRelative("childPattern");
		if (childProp == null || childProp.GetUnderlyingValue() == null)
		{
			GUI.Label(position, "Child Pattern");
			position.x += EditorGUIUtility.labelWidth;
			position.width -= EditorGUIUtility.labelWidth;

			if (GUI.Button(position, "Add"))
			{
				childProp.SetUnderlyingValue(new PatternData());
			}

			position.x -= EditorGUIUtility.labelWidth;
			position.width += EditorGUIUtility.labelWidth;
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
		else
		{
			EditorGUI.PropertyField(position, childProp, new GUIContent("Child Pattern"));
			position.x += EditorGUIUtility.labelWidth;
			position.width -= EditorGUIUtility.labelWidth;

			if (GUI.Button(position,"Remove"))
			{
				childProp.SetUnderlyingValue(null);
			}

			position.x -= EditorGUIUtility.labelWidth;
			position.width += EditorGUIUtility.labelWidth;
			position.y += EditorGUI.GetPropertyHeight(childProp) + EditorGUIUtility.standardVerticalSpacing;
		}

		property.serializedObject.ApplyModifiedProperties();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		if (property.GetUnderlyingValue() == null || !property.isExpanded)
		{
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
		SerializedProperty dataProp = property.FindPropertyRelative("dataValues");
		SerializedProperty timeProp = property.FindPropertyRelative("timeline");
		SerializedProperty childProp = property.FindPropertyRelative("childPattern");
		return EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing * 9 + EditorGUI.GetPropertyHeight(dataProp) + EditorGUI.GetPropertyHeight(timeProp) + EditorGUI.GetPropertyHeight(childProp);
	}
}
