using Assets.draco18s.training;
using Assets.draco18s.util;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace Assets.draco18s.bulletboss.ui
{
	public class HardPoint : MonoBehaviour, IInventoryDropTarget
	{
		[SerializeField]
		private UpgradeType m_slotType;

		public UpgradeType slotType => m_slotType;

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
			if (slotType == UpgradeType.FighterEntry)
			{
				if (item == null)
				{
					Clear(attachedBarrel);
					return true;
				}
				attachedBarrel = item;
				spawnedObject = Instantiate(item.upgradeTypeData.data.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
				HostileFighter fighter = spawnedObject.GetComponent<HostileFighter>();

				if (fighter == null) return false;

				fighter.SetSpawn(this);
				item.transform.SetParent(transform);
				item.gameObject.SetActive(false);

				return true;
			}
			if (item == null)
			{
				Clear(attachedBarrel);
				return true;
			}
			if (item.upgradeTypeData.data.type != slotType) return false;
			if (attachedBarrel != null)
			{
				Destroy(spawnedObject);
				if(attachedBarrel.upgradeTypeData.data.rarityTier != NamedRarity.Starting)
					Inventory.instance.Add(attachedBarrel);
			}
			spawnedObject = Instantiate(item.upgradeTypeData.data.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
			gun = spawnedObject.GetComponent<GunBarrel>();
			if (gun != null)
			{
				gun.SetMounting(this);
				attachedBarrel = item;
				Inventory.instance.Remove(attachedBarrel);
				attachedBarrel.transform.SetParent(transform);
				attachedBarrel.gameObject.SetActive(false);
			}
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

			if(attachedShell != null && attachedShell.upgradeTypeData.data.attributeModifiers.TryGetValue(stat, out var attributeModifier))
				r = combine(r, attributeModifier);

			foreach (InventoryItem item in attachedUpgrades)
			{
				if(!item.upgradeTypeData.data.attributeModifiers.ContainsKey(stat)) continue;
				r = combine(r, item.upgradeTypeData.data.attributeModifiers[stat]);
			}
			return r;
		}

		public void AttachFromSlotUpdate(UpgradeType slotType,InventoryItem item)
		{
			if (slotType == UpgradeType.Bullet)
			{
				attachedShell = item;
				gun?.SetShell(attachedShell);
			}
			else if (slotType == UpgradeType.FighterEntry)
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

				spawnedObject = Instantiate(item.upgradeTypeData.data.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
				HostileFighter fighter = spawnedObject.GetComponent<HostileFighter>();
				fighter.SetSpawn(this);
				attachedBarrel = item;
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

				spawnedObject = Instantiate(item.upgradeTypeData.data.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
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
				ret = ret.Merge(item.upgradeTypeData.data.patternModifiers);
			}

			return ret;
		}
	}
}
