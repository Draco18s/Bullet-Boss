using Assets.draco18s.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.draco18s.bulletboss.ui;
using Unity.VisualScripting;
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
		public GameObject secondaryPrefab;
		public PatternEffects patternModifiers;

		public void Populate(UpgradeRuntime original)
		{
			type = original.type;
			image = original.image;
			upgradeID = original.upgradeID;
			upgradeName = original.upgradeName;
			description = original.description;
			rarityTier = original.rarityTier;
			relativeRarityInTier = original.relativeRarityInTier;

			foreach (var kvp in original.attributeModifiers)
			{
				attributeModifiers.Add(kvp.Key, kvp.Value);
			}

			patternModifiers = original.patternModifiers.Copy();
			relevantPattern.CopyFrom(original.relevantPattern);
			relevantPrefab = original.relevantPrefab;
			secondaryPrefab = original.secondaryPrefab;
		}

		public string GenerateTooltip()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<color=\"").Append(Inventory.GetColor(rarityTier).ToHexString()).Append("\">");
			sb.AppendLine(upgradeName);
			sb.Append("</color>");
			sb.AppendLine(description);
			return sb.ToString();
		}
	}
}
