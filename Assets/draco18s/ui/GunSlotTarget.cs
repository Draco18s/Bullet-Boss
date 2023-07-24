using System;
using Assets.draco18s.util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.ui
{
	public class GunSlotTarget : MonoBehaviour, IInventoryDropTarget
	{
		public delegate void OnInventorySlotChanged(GunSlotTarget slot, InventoryItem previousItem, InventoryItem updatedItem);

		public OnInventorySlotChanged OnUpdate;

		[SerializeField]
		private UpgradeScriptable.UpgradeType m_slotType;

		public bool HideSlotWhenEmpty = false;

		public UpgradeScriptable.UpgradeType slotType => m_slotType;

		[SerializeField] public InventoryItem attachedItem;

		public bool Attach(InventoryItem item)
		{
			if (item.upgradeTypeData.type != slotType) return false;
			if (attachedItem != null)
			{
				Inventory.instance.Add(attachedItem);
			}
			InventoryItem pItem = attachedItem;
			attachedItem = item;
			Inventory.instance.Remove(attachedItem);
			attachedItem.transform.SetParent(transform);
			attachedItem.transform.localPosition = Vector3.zero;
			attachedItem.transform.SetSiblingIndex(1);
			OnUpdate(this, pItem, attachedItem);
			return true;
		}

		public void Clear(InventoryItem item)
		{
			if (item == attachedItem)
			{
				OnUpdate(this, attachedItem, null);
				attachedItem = null;
			}
			else
			{
				throw new Exception("What");
			}
		}

		public void SetNoUpdate(InventoryItem item)
		{
			if(attachedItem != null) attachedItem.gameObject.SetActive(false);
			attachedItem = item;
			if (attachedItem == null) return;
			attachedItem.transform.SetParent(transform);
			attachedItem.transform.localPosition = Vector3.zero;
			attachedItem.transform.SetSiblingIndex(1);
			attachedItem.gameObject.SetActive(true);
		}
	}
}