using Assets.draco18s.training;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.ui
{
	public class HardPointTarget : MonoBehaviour, IInventoryDropTarget
	{
		[SerializeField]
		private UpgradeScriptable.UpgradeType m_slotType;

		public UpgradeScriptable.UpgradeType slotType => m_slotType;

		[SerializeField]
		public InventoryItem attachedItem;

		public GunBarrel gun => spawnedObject.GetComponent<GunBarrel>();

		private GameObject spawnedObject;

		public bool Attach(InventoryItem item)
		{
			if (item.upgradeTypeData.type != slotType) return false;
			if (attachedItem != null)
			{
				Destroy(spawnedObject);
				Inventory.instance.Add(attachedItem);
			}
			spawnedObject = Instantiate(item.upgradeTypeData.relevantPrefab, transform.position.ReplaceZ(-1), Quaternion.identity, transform.parent);
			attachedItem = item;
			Inventory.instance.Remove(attachedItem);
			attachedItem.transform.SetParent(transform);
			attachedItem.gameObject.SetActive(false);
			return true;
		}

		public void Clear(InventoryItem inventoryItem)
		{
			
		}

		[UsedImplicitly]
		private void OnMouseUpAsButton()
		{
			PatternEditor.instance.ChangeTarget(spawnedObject);
			UpgradeSlotGroup.instance.Detail(this);
		}
	}
}
