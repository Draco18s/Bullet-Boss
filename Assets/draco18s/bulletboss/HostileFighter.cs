using System.Linq;
using Assets.draco18s.bulletboss.ui;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.bulletboss
{
	public class HostileFighter : MonoBehaviour, IDamageTaker
	{
		public SpriteRenderer image;
		private HardPoint spawnPoint;
		private bool active;

		[UsedImplicitly]
		public UnityEvent OnTakeDamage { get; }

		public float CurHp;
		public float MaxHp;
		private float speed = 1;

		public void SetSpawn(HardPoint mount)
		{
			spawnPoint = mount;
			transform.rotation = mount.transform.rotation;
			active = false;
			gameObject.layer = LayerMask.NameToLayer("Default");
			image.color = new Color(1, 1, 1, 0.5f);
		}

		public void Spawn(HardPoint mount)
		{
			spawnPoint = mount;
			transform.rotation = mount.transform.rotation;
			transform.localPosition = mount.transform.localPosition - mount.transform.right*3;
			CurHp = MaxHp = mount.GetStat(StatAttribute.FighterHealth, (a, b) => a + b);
			speed *= mount.GetStat(StatAttribute.GunSpeed, (a, b) => a + b);
			(PatternData d, InventoryItem i) = mount.GetFighterGunPattern();
			UpgradeScriptable gun = ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => s.data.type == UpgradeType.SmallGun && s.data.rarityTier == NamedRarity.Starting).First();
			UpgradeScriptable shell = ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => s.data.type == UpgradeType.Bullet && s.data.rarityTier == NamedRarity.Starting).First();
			GameObject go = Instantiate(gun.data.relevantPrefab, transform);
			GunBarrel bar = go.GetComponent<GunBarrel>();
			bar.SetPattern(d);
			bar.SetShell(shell.data, d.childPattern ?? shell.data.relevantPattern);
			bar.pattern.effects.Merge(i.upgradeTypeData.patternModifiers);
			active = true;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if (!active) return;

			if (CurHp <= 0)
			{
				Destroy(gameObject);
				return;
			}
			float dt = Time.fixedDeltaTime;
			transform.Translate(new Vector3(1, 0, 0) * dt, Space.Self);
		}
		public float ApplyDamage(float damage, Collider2D col)
		{
			CurHp -= damage;
			OnTakeDamage.Invoke();
			return damage;
		}
	}
}
