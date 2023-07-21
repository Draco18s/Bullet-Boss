using System;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "BulletBoss/Upgrade", order = 1)]
public class UpgradeScriptable : ScriptableObject
{
	[Serializable]
	public enum UpgradeType
	{
		Unknown,GunBarrel,Bullet,Attribute,SpecialTrigger_Gun,SpecialTrigger_Bullet
	}

	public enum NamedRarity
	{
		Starting,Common,Uncommon,Rare,Epic,Artifact,UltraRare,Legendary,Unique
	}

	public UpgradeType type;
	public Sprite image;
	public string upgradeID;
	public string upgradeName;
	public string description;
	public NamedRarity rarityTier;
	public int relativeRarityInTier = 100;

	public PatternData relevantPattern;
	public GameObject relevantPrefab;
}
