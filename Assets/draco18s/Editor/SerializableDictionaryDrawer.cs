using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Assets.draco18s.util
{
	[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
	public class SerializableDictionaryDrawer : PropertyDrawer
	{
		private ReorderableList list;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty keysProp = property.FindPropertyRelative("keys");
			SerializedProperty valuesProp = property.FindPropertyRelative("values");
			if (list == null || list.serializedProperty.serializedObject != keysProp.serializedObject)
			{
				list = SetupList(keysProp, valuesProp, label);
			}

			list.DoList(position);
			property.serializedObject.ApplyModifiedProperties();
		}

		private static ReorderableList SetupList(SerializedProperty keysProp, SerializedProperty valuesProp, GUIContent guiContent)
		{
			ReorderableList list = new ReorderableList(keysProp.serializedObject, keysProp, false, true, true, true)
			{
				drawHeaderCallback = rect =>
				{
					EditorGUI.LabelField(rect, guiContent);
				},
				drawElementCallback = ((rect, index, active, focused) =>
				{
					rect.y += EditorGUIUtility.standardVerticalSpacing;
					rect.height = EditorGUIUtility.singleLineHeight;
					rect.width /= 2;
					rect.width -= 2;
					rect.x += 2;
					EditorGUI.PropertyField(rect, keysProp.GetArrayElementAtIndex(index), GUIContent.none);
					rect.x += rect.width + 2;
					EditorGUI.PropertyField(rect, valuesProp.GetArrayElementAtIndex(index), GUIContent.none);
					rect.width += 2;
					rect.width *= 2;
				}),
				onAddCallback = _ =>
				{
					keysProp.arraySize++;
					valuesProp.arraySize = keysProp.arraySize;

				},
				onRemoveCallback = l =>
				{
					for (int index = l.selectedIndices.Count; index-- > 0;)
					{
						Debug.Log(index);
						int i = l.selectedIndices[index];
						keysProp.DeleteArrayElementAtIndex(i);
						valuesProp.DeleteArrayElementAtIndex(i);
					}
				}
			};
			return list;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty keysProp = property.FindPropertyRelative("keys");
			return (list?.GetHeight() ?? (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 3) * (keysProp.arraySize + 2) + EditorGUIUtility.standardVerticalSpacing);
		}
	}
}
