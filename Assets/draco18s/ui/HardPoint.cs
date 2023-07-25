using Assets.draco18s.training;
using Assets.draco18s.util;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace Assets.draco18s.ui
{
	public class HardPoint : MonoBehaviour, IInventoryDropTarget
	{
		[SerializeField]
		private UpgradeScriptable.UpgradeType m_slotType;

		public UpgradeScriptable.UpgradeType slotType => m_slotType;

		[SerializeField]
		public InventoryItem attachedBarrel;
		[SerializeField]
		public InventoryItem attachedShell;

		public GunBarrel gun;

		private GameObject spawnedObject;

		public readonly List<InventoryItem> attachedUpgrades = new List<InventoryItem>();
		public SerializableDictionary<StatAttribute, float> hardpointModifiers;

		public bool Attach(InventoryItem item)
		{
			if (item == null)
			{
				Clear(attachedBarrel);
				return true;
			}
			if (item.upgradeTypeData.type != slotType) return false;
			if (attachedBarrel != null)
			{
				Destroy(spawnedObject);
				if(attachedBarrel.upgradeTypeData.rarityTier != UpgradeScriptable.NamedRarity.Starting)
					Inventory.instance.Add(attachedBarrel);
			}
			spawnedObject = Instantiate(item.upgradeTypeData.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
			gun = spawnedObject.GetComponent<GunBarrel>();
			gun.SetMounting(this);
			attachedBarrel = item;
			Inventory.instance.Remove(attachedBarrel);
			attachedBarrel.transform.SetParent(transform);
			attachedBarrel.gameObject.SetActive(false);
			return true;
		}

		public void Clear(InventoryItem item)
		{
			if (item == attachedBarrel)
			{
				Destroy(spawnedObject);
				spawnedObject = null;
				gun = null;
				attachedBarrel = null;
			}
			else
			{
				throw new Exception("What");
			}
		}

		[UsedImplicitly]
		private void OnMouseUpAsButton()
		{
			PatternEditor.instance.ChangeTarget(spawnedObject);
			UpgradeSlotGroup.instance.Detail(this);
		}

		public float GetStat(StatAttribute stat, Func<float, float, float> combine)
		{
			float r = 1;
			if (hardpointModifiers.ContainsKey(stat))
			{
				hardpointModifiers.TryGetValue(stat, out float modifier);
				r = combine(r, modifier);
			}

			if(attachedShell != null && attachedShell.upgradeTypeData.attributeModifiers.TryGetValue(stat, out var attributeModifier))
				r = combine(r, attributeModifier);

			foreach (InventoryItem item in attachedUpgrades)
			{
				if(!item.upgradeTypeData.attributeModifiers.ContainsKey(stat)) continue;
				r = combine(r, item.upgradeTypeData.attributeModifiers[stat]);
			}
			return r;
		}

		public void AttachFromSlotUpdate(UpgradeScriptable.UpgradeType slotType,InventoryItem item)
		{
			if (slotType == UpgradeScriptable.UpgradeType.Bullet)
			{
				attachedShell = item;
				gun?.SetShell(attachedShell);
			}
			else
			{
				if (item == null)
				{
					Clear(attachedBarrel);
					return;
				}

				if (attachedBarrel != null)
				{
					Destroy(spawnedObject);
					Inventory.instance.Add(attachedBarrel);
				}

				spawnedObject = Instantiate(item.upgradeTypeData.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
				gun = spawnedObject.GetComponent<GunBarrel>();
				gun.SetMounting(this);
				gun.SetShell(attachedShell);
				attachedBarrel = item;
			}
		}

		public PatternEffects GetPatternModifiers()
		{
			PatternEffects ret = new PatternEffects();
			foreach (InventoryItem item in attachedUpgrades)
			{
				ret = ret.Merge(item.upgradeTypeData.patternModifiers);
			}

			return ret;
		}
	}
}
