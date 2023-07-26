using Assets.draco18s.bulletboss;
using Assets.draco18s.bulletboss.ui;

namespace Assets.draco18s
{
	public interface IInventoryDropTarget
	{
		UpgradeType slotType { get; }
		bool Attach(InventoryItem item);
		void Clear(InventoryItem item);
	}
}
