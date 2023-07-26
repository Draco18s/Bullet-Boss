using Assets.draco18s.training;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.bulletboss.ui
{
	public class UpgradeSlotGroup : MonoBehaviour
	{
		public static UpgradeSlotGroup instance;
		public GunSlotTarget[] slots;
		private HardPoint activeHardPoint;

		[UsedImplicitly]
		void Start()
		{
			instance = this;
			foreach (GunSlotTarget slot in slots)
			{
				slot.OnUpdate += UpdateSlotContents;
			}
			Hide();
		}

		private void UpdateSlotContents(GunSlotTarget slot, InventoryItem previousItem, InventoryItem updateditem)
		{
			if (activeHardPoint == null) return;
			
			if (previousItem == activeHardPoint.attachedBarrel)
			{
				activeHardPoint.AttachFromSlotUpdate(activeHardPoint.slotType, updateditem);
			}
			else if (previousItem == activeHardPoint.attachedShell)
			{
				activeHardPoint.AttachFromSlotUpdate(UpgradeType.Bullet, updateditem);
			}
			else if (previousItem == null)
			{
				activeHardPoint.attachedUpgrades.Add(updateditem);
			}
			else if (updateditem == null)
			{
				activeHardPoint.attachedUpgrades.Remove(previousItem);
			}
		}

		public void Detail(HardPoint hardPoint)
		{
			activeHardPoint = hardPoint;
			if (activeHardPoint == null)
			{
				Hide();
				return;
			}

			Show();
			foreach (GunSlotTarget slot in slots)
			{
				slot.SetNoUpdate(null);
			}
			transform.position = Camera.main.WorldToScreenPoint(hardPoint.transform.position);
			GunBarrel gun = hardPoint.gun;
			GetFirstRelevantSlot(hardPoint.slotType).SetNoUpdate(hardPoint.attachedBarrel);
			GetFirstRelevantSlot(UpgradeType.Bullet).SetNoUpdate(hardPoint.attachedShell);
			foreach (InventoryItem item in hardPoint.attachedUpgrades)
			{
				GunSlotTarget slot = GetFirstRelevantSlot(item.upgradeTypeData.data.type);
				slot?.SetNoUpdate(item);
			}
			foreach (GunSlotTarget slot in slots)
			{
				if (slot.HideSlotWhenEmpty) 
				{
					slot.gameObject.SetActive(slot.attachedItem != null);
				}
			}
			GetFirstRelevantSlot(hardPoint.slotType, false).gameObject.SetActive(true);

			//TODO later
			if (hardPoint.slotType == UpgradeType.FighterEntry)
			{
				GetFirstRelevantSlot(UpgradeType.Bullet, false).gameObject.SetActive(false);
				return;
			}
			GetFirstRelevantSlot(UpgradeType.Bullet, false).gameObject.SetActive(true);
		}

		private GunSlotTarget GetFirstRelevantSlot(UpgradeType slotType, bool empty=true)
		{
			foreach (GunSlotTarget slot in slots)
			{
				if (slot.slotType == slotType && (!empty || slot.attachedItem == null)) return slot;
			}
			return null;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public void Show()
		{
			gameObject.SetActive(true);
		}
	}
}
