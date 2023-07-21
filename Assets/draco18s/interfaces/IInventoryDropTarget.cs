using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.draco18s.ui;

namespace Assets.draco18s
{
	public interface IInventoryDropTarget
	{
		UpgradeScriptable.UpgradeType slotType { get; }
		bool Attach(InventoryItem item);
		void Clear(InventoryItem item);
	}
}
