using Assets.draco18s.util;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

		public Image timerGraphic;

		public GunBarrel gun;

		private GameObject spawnedObject;

		public readonly List<InventoryItem> attachedUpgrades = new List<InventoryItem>();
		public SerializableDictionary<StatAttribute, float> hardpointModifiers;

		public int SpawnCount;
		public float SpawnTime;
		public float SpawnTimer;
		public float cooldown;
		public float cooldownTimer;

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
				if(attachedBarrel.upgradeTypeData.rarityTier != NamedRarity.Starting)
					Inventory.instance.Add(attachedBarrel);
			}
			spawnedObject = Instantiate(item.upgradeTypeData.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
			gun = spawnedObject.GetComponent<GunBarrel>();
			if (gun != null)
			{
				gun.SetMounting(this);
				attachedBarrel = item;
				Inventory.instance.Remove(attachedBarrel);
				attachedBarrel.transform.SetParent(transform);
				gun.transform.localRotation = transform.localRotation;
				attachedBarrel.gameObject.SetActive(false);
			}
			HostileFighter fighter = spawnedObject.GetComponent<HostileFighter>();
			if (fighter != null)
			{
				fighter.SetSpawn(this);
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
			if (GameStateManager.instance.state == GameStateManager.GameState.Manage)
			{
				PatternEditor.instance.ChangeTarget(spawnedObject);
				UpgradeSlotGroup.instance.Detail(this);
				return;
			}

			if (GameStateManager.instance.state == GameStateManager.GameState.InGame && slotType == UpgradeType.FighterEntry)
			{
				if (SpawnCount <= 0 || cooldownTimer <= 0) return;
				SpawnCount--;
				GameObject go = Instantiate(spawnedObject, GameTransform.instance.transform);
				HostileFighter fighter = go.GetComponent<HostileFighter>();
				fighter.Spawn(this);
				cooldownTimer = 0f;
			}
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if (spawnedObject == null || slotType != UpgradeType.FighterEntry) return;
			if (GameStateManager.instance.state != GameStateManager.GameState.InGame) return;
			float dt = Time.fixedDeltaTime;
			SpawnTimer += dt;
			cooldownTimer += dt;
			if (SpawnTimer >= SpawnTime)
			{
				SpawnTimer -= SpawnTime;
				SpawnCount++;
			}

			if (timerGraphic == null) return;
			if (SpawnCount > 0)
			{
				timerGraphic.fillAmount = cooldownTimer / 1.5f;
				timerGraphic.color = Color.yellow;
			}
			else
			{
				timerGraphic.fillAmount = SpawnTimer / SpawnTime;
				timerGraphic.color = Color.red;
			}

			if (timerGraphic.fillAmount > 0.999f)
			{
				timerGraphic.color = Color.green;
			}
		}

		public float GetStat(StatAttribute stat, Func<float> baseValue, Func<float, float, float> combine)
		{
			float r = baseValue();
			if (hardpointModifiers.ContainsKey(stat))
			{
				hardpointModifiers.TryGetValue(stat, out float modifier);
				r = combine(r, modifier);
			}

			if (this.slotType == UpgradeType.FighterEntry && attachedBarrel.upgradeTypeData.attributeModifiers.TryGetValue(stat, out float mod))
			{
				r = combine(r, mod);
			}

			if(attachedShell != null && attachedShell.upgradeTypeData.attributeModifiers.TryGetValue(stat, out var attributeModifier))
			{
				r = combine(r, attributeModifier);
			}

			foreach (InventoryItem item in attachedUpgrades)
			{
				if(!item.upgradeTypeData.attributeModifiers.ContainsKey(stat)) continue;
				r = combine(r, item.upgradeTypeData.attributeModifiers[stat]);
			}
			return r;
		}

		public (PatternData, InventoryItem) GetFighterGunPattern()
		{
			InventoryItem fg = attachedBarrel;
			if (fg.upgradeTypeData.relevantPattern.ReloadType != GunType.None)
				return (fg.upgradeTypeData.relevantPattern,fg);
			else
				return (ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => s.data.rarityTier == NamedRarity.Starting && s.data.type == UpgradeType.SmallGun).First().data.relevantPattern,fg);
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

				spawnedObject = Instantiate(item.upgradeTypeData.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
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
				ret = ret.CombineIntoNew(item.upgradeTypeData.patternModifiers);
			}

			return ret;
		}
	}
}
