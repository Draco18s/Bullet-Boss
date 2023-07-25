using System;
using Assets.draco18s.util;
using Assets.draco18s;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.draco18s
{
	[Serializable]
	public class PatternData
	{
		public GunType ReloadType;
		public float Lifetime = 10;
		public float StartAngle;
		public float TimeOffset;
		public bool isEditable;
		public PatternEffects effects;
		public Sprite image;

		public SerializableDictionary<PatternDataKey, float> dataValues = PopulateDict();
		public Timeline timeline = new Timeline();
		[SerializeReference]
		public PatternData childPattern;

		private static SerializableDictionary<PatternDataKey, float> PopulateDict()
		{
			SerializableDictionary<PatternDataKey, float> d = new SerializableDictionary<PatternDataKey, float>();
			for (PatternDataKey i = 0; i < PatternDataKey.Length; i++)
			{
				d.Add(i, 0);
			}

			return d;
		}

		public void CopyFrom(PatternData other, bool includeChildPatterns=true)
		{
			ReloadType = other.ReloadType;
			Lifetime = other.Lifetime;
			StartAngle = other.StartAngle;
			TimeOffset = other.TimeOffset;
			dataValues.CopyFrom(other.dataValues);
			timeline.CopyFrom(other.timeline);
			image = other.image;
			isEditable = other.isEditable;
			effects = other.effects;
			if (includeChildPatterns)
				childPattern = other.childPattern;
		}

		public void ApplyEffects(PatternEffects changes)
		{
			effects = effects.Merge(changes);
		}
	}
}