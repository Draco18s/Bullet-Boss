using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.ui;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(CustomScrollbar), true)]
[CanEditMultipleObjects]
public class CustomScrollbarEd : ScrollbarEditor
{
	SerializedProperty CustomNumberOfSteps;

	protected override void OnEnable()
	{
		base.OnEnable();
		CustomNumberOfSteps = serializedObject.FindProperty("CustomNumberOfSteps");
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		EditorGUILayout.PropertyField(CustomNumberOfSteps);
		serializedObject.ApplyModifiedProperties();
	}
}
