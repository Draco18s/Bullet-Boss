using System;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using Assets.draco18s.util;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "BulletBoss/Upgrade", order = 1)]
public class UpgradeScriptable : ScriptableObject
{
	[Serializable]
	public enum UpgradeType
	{
		Unknown,SmallGun,BigGun,Launcher,Bullet,Attribute,SpecialTrigger_Gun,SpecialTrigger_Bullet
	}

	public enum NamedRarity
	{
		Starting,Common,Uncommon,Rare,Epic,Artifact,UltraRare,Legendary,Unique
	}

	public UpgradeType type;
	public Sprite image;
	[HideInInspector]
	public string upgradeID;
	public string upgradeName;
	[TextArea(3, 10)]
	public string description;
	public NamedRarity rarityTier;
	public int relativeRarityInTier = 100;
	public SerializableDictionary<StatAttribute, float> attributeModifiers;

	public PatternData relevantPattern;
	public GameObject relevantPrefab;
	public PatternEffects patternModifiers;

	void OnValidate()
	{
		upgradeID = this.name;
	}
}
