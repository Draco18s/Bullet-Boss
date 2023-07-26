using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.draco18s.bulletboss
{
	[Serializable]
	public enum UpgradeType
	{
		Unknown,
		SmallGun,
		BigGun,
		Launcher,
		Bullet,
		Attribute,
		SpecialTrigger_Gun,
		SpecialTrigger_Bullet,
		FighterEntry
	}

	public enum NamedRarity
	{
		Starting,
		Common,
		Uncommon,
		Rare,
		Epic,
		Artifact,
		UltraRare,
		Legendary,
		Unique
	}
}
