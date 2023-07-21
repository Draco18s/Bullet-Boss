using System;
using Assets.draco18s.util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.ui
{
	public class GunSlotTarget : MonoBehaviour, IInventoryDropTarget
	{
		[SerializeField]
		private UpgradeScriptable.UpgradeType m_slotType;

		public UpgradeScriptable.UpgradeType slotType => m_slotType;

		[SerializeField] public InventoryItem attachedItem;

		public bool Attach(InventoryItem item)
		{
			if (item.upgradeTypeData.type != slotType) return false;
			if (attachedItem != null)
			{
				Inventory.instance.Add(attachedItem);
			}
			attachedItem = item;
			Inventory.instance.Remove(attachedItem);
			attachedItem.transform.SetParent(transform);
			attachedItem.transform.localPosition = Vector3.zero;
			attachedItem.transform.SetSiblingIndex(1);
			return true;
		}

		public void Clear(InventoryItem item)
		{
			if (item == attachedItem)
			{
				attachedItem = null;
			}
			else
			{
				throw new Exception("What");
			}
		}
	}
}