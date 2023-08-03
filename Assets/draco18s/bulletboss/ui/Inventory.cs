using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.bulletboss.ui
{
	[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
	public class Inventory : MonoBehaviour
	{
		public static Inventory instance;
		public GameObject itemPrefab;
		private readonly List<InventoryItem> inventory = new List<InventoryItem>();
		private Canvas canvas;
		[SerializeField] private Transform container;
		private int killCounter = 0;

		[UsedImplicitly]
		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			instance = this;
			canvas = GetComponent<Canvas>();
			foreach (UpgradeScriptable v in ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => true))
			{
				GameObject go = Instantiate(itemPrefab, container);
				InventoryItem item = go.GetComponent<InventoryItem>();
				item.upgradeTypeData.Populate(v.data);
				inventory.Add(item);
				item.transform.SetParent(container);
				item.gameObject.SetActive(true);
			}
		}

		[UsedImplicitly]
		void Update()
		{
			if(Input.GetButtonDown("OpenInventory"))
			{
				canvas.enabled = !canvas.enabled;
				if (!canvas.enabled)
					UpgradeSlotGroup.instance.Hide();
			}
		}

		public void Remove(InventoryItem item)
		{
			if (item == null) return;
			inventory.Remove(item);
			if (item.upgradeTypeData.rarityTier == NamedRarity.Starting)
			{
				InventoryItem newCopy = Instantiate(item, container);
				newCopy.transform.SetSiblingIndex((int)item.upgradeTypeData.type -1);
			}
		}

		public void Add(InventoryItem item)
		{
			if (item.upgradeTypeData.rarityTier == NamedRarity.Starting)
			{
				Destroy(item.gameObject);
				return;
			}
			inventory.Add(item);
			item.transform.SetParent(container);
			item.gameObject.SetActive(true);
		}

		public void AddKill()
		{
			killCounter++;
		}

		private static readonly Color purple = new Color(54 / 255f, 16 / 255f, 255 / 255f, 1);
		private static readonly Color orange = new Color(255 / 255f, 88 / 255f, 16 / 255f, 1);
		private static readonly Color yellow = new Color(255 / 255f, 192 / 255f, 8 / 255f, 1);
		private static readonly Color seagrn = new Color(8 / 255f, 255 / 255f, 147 / 255f, 1);
		public static Color GetColor(NamedRarity rarity)
		{
			switch (rarity)
			{
				case NamedRarity.Starting:
				case NamedRarity.Common:
				case NamedRarity.Legendary:
					return Color.white;
				case NamedRarity.Uncommon:
					return Color.green;
				case NamedRarity.Rare:
					return Color.blue;
				case NamedRarity.Epic:
					return purple;
				case NamedRarity.Artifact:
					return orange;
				case NamedRarity.UltraRare:
					return seagrn;
				case NamedRarity.Unique:
					return yellow;
			}
			return Color.gray;
		}
	}
}
