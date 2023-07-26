using Assets.draco18s.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	[Serializable]
	public class UpgradeRuntime
	{
		public UpgradeType type;
		public Sprite image;
		[HideInInspector] public string upgradeID;
		public string upgradeName;
		[TextArea(3, 10)] public string description;
		public NamedRarity rarityTier;
		public int relativeRarityInTier = 100;
		public SerializableDictionary<StatAttribute, float> attributeModifiers;

		public PatternData relevantPattern;
		public GameObject relevantPrefab;
		public PatternEffects patternModifiers;
	}
}
