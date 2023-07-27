using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.draco18s.bulletboss;
using Assets.draco18s.util;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Assets.draco18s.Editor
{
	[CustomEditor(typeof(ResourcesManager))]
	public class ResourcesManagerEd : UnityEditor.Editor
	{
		private SerializedProperty assetsProperty;
		[UsedImplicitly]
		void OnEnable()
		{
			assetsProperty = serializedObject.FindProperty("ScriptableObjects");
		}

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(assetsProperty);
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Recompile"))
			{
				BuildAssetList(assetsProperty);
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void BuildAssetList(SerializedProperty property)
		{
			List<ScriptableObject> allObj = FindAssetsByType<ScriptableObject>();
			property.ClearArray();
			int index = 0;
			foreach (ScriptableObject obj in allObj)
			{
				property.InsertArrayElementAtIndex(index);
				property.GetArrayElementAtIndex(index).objectReferenceValue = obj;
				index++;
			}
		}

		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			List<T> assets = new List<T>();

			string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).ToString().Replace("UnityEngine.", "")}");

			foreach (string t in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(t);
				if(assetPath.ToLowerInvariant().Contains("packages")) continue;
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}
			return assets;
		}
	}
}
