using System.Xml;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.bulletboss.ui
{
	public class UpgradeCard : MonoBehaviour
	{
		public InventoryItem item { get; protected set; }
		public Image image;
		public Image glint;
		public TextMeshProUGUI title;
		public TextMeshProUGUI description;
		public Gradient rainbow;
		
		public void SetUpgrade(InventoryItem upgrade)
		{
			item = upgrade;
			if (item == null)
			{
				this.gameObject.SetActive(false);
				return;
			}
			gameObject.SetActive(true);
			image.sprite = item.upgradeTypeData.data.image;
			title.text = item.upgradeTypeData.data.upgradeName;
			description.text = item.upgradeTypeData.data.description;
			Color c = GetColor(item.upgradeTypeData.data.rarityTier);
			c.a = 135 / 255f;
			glint.color = c;
		}

		[UsedImplicitly]
		void Update()
		{
			if (item == null) return;
			if (item.upgradeTypeData.data.rarityTier != NamedRarity.Legendary) return;
			Color c = rainbow.Evaluate(Time.time % 1);
			c.a = 135 / 255f;
			glint.color = c;
		}

		private static readonly Color purple = new Color(54 / 255f, 16 / 255f, 255 / 255f, 1);
		private static readonly Color orange = new Color(255 / 255f, 88 / 255f, 16 / 255f, 1);
		private static readonly Color yellow = new Color(255 / 255f, 192 / 255f, 8 / 255f, 1);
		private static readonly Color seagrn = new Color(8 / 255f, 255 / 255f, 147 / 255f, 1);
		private static Color GetColor(NamedRarity rarity)
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