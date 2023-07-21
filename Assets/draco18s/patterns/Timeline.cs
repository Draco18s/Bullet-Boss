using Assets.draco18s.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Assets.draco18s
{
	[Serializable]
	public class Timeline
	{
		public float duration;
		public SerializableDictionary<PatternDataKey, AnimationCurve> data = new SerializableDictionary<PatternDataKey, AnimationCurve>();
		
		public float Evaluate(PatternDataKey d, float t)
		{
			return data[d].Evaluate(t);
		}
#pragma warning disable CS0618 // Type or member is obsolete
		internal void CopyFrom(Timeline original)
		{
			data.Clear();
			for (PatternDataKey i = 0; i < PatternDataKey.Length; i++)
			{
				if (!original.data.TryGetValue(i, out AnimationCurve v)) continue;
				Keyframe[] keys = new Keyframe[v.keys.Length];
				for (int k = 0; k < v.keys.Length; k++)
				{
					keys[k] = new Keyframe(v.keys[k].time, v.keys[k].value, v.keys[k].inTangent, v.keys[k].outTangent, v.keys[k].inWeight, v.keys[k].outWeight);
					keys[k].tangentMode = v.keys[k].tangentMode;
				}
				AnimationCurve v2 = new AnimationCurve(keys);
				data.Add(i, v2);
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}
}