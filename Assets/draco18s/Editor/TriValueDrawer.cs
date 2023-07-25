using Assets.draco18s.util;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

namespace Assets.draco18s.Editor
{
	[CustomPropertyDrawer(typeof(TriValue))]
	public class TriValueDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			/*int vv = property.FindPropertyRelative("v").intValue;
			int v = 1;
			if (vv < 0) v = 2;
			if (vv == 0) v = 0;
			v = EditorGUI.Popup(position, label, v, new[] { new GUIContent("Default"), new GUIContent("True"), new GUIContent("False") });
			switch (v)
			{
				case 1:
					property.FindPropertyRelative("v").intValue = 1;
					break;
				case 2:
					property.FindPropertyRelative("v").intValue = -1;
					break;
				default:
					property.FindPropertyRelative("v").intValue = 0;
					break;
			}*/
			TriValue t = property.GetUnderlyingValue() as TriValue;
			int v = t == TriValue.True ? 1 : (t == TriValue.False ? 2 : 0);
			int w = EditorGUI.Popup(position, label, v, new[] { new GUIContent("Default"), new GUIContent("True"), new GUIContent("False") });
			switch (w)
			{
				case 1:
					property.SetUnderlyingValue(TriValue.True.Clone());
					break;
				case 2:
					property.SetUnderlyingValue(TriValue.False.Clone());
					break;
				default:
					property.SetUnderlyingValue(TriValue.Default.Clone());
					break;
			}

			EditorUtility.SetDirty(property.serializedObject.targetObject);
			property.serializedObject.ApplyModifiedProperties();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}
