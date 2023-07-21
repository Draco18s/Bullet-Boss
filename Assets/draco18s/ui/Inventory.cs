using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.ui
{
	[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
	public class Inventory : MonoBehaviour
	{
		public static Inventory instance;
		public GameObject itemPrefab;
		private readonly List<InventoryItem> inventory = new List<InventoryItem>();
		private Canvas canvas;
		[SerializeField] private Transform container;

		[UsedImplicitly]
		void Start()
		{
			instance = this;
			canvas = GetComponent<Canvas>();
		}

		[UsedImplicitly]
		void Update()
		{
			if(Input.GetButtonDown("OpenInventory"))
			{
				canvas.enabled = !canvas.enabled;
			}
		}

		public void Remove(InventoryItem item)
		{
			if (item == null) return;
			inventory.Remove(item);
			if (item.upgradeTypeData.rarityTier == UpgradeScriptable.NamedRarity.Starting)
			{
				InventoryItem newCopy = Instantiate(item, container);
				newCopy.transform.SetSiblingIndex((int)item.upgradeTypeData.type -1);
			}
		}

		public void Add(InventoryItem item)
		{
			if (item.upgradeTypeData.rarityTier == UpgradeScriptable.NamedRarity.Starting)
			{
				Destroy(item.gameObject);
				return;
			}
			inventory.Add(item);
			item.transform.SetParent(container);
			item.gameObject.SetActive(true);
		}
	}
}
