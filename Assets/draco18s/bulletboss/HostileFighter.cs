using System;
using System.Collections.Generic;
using System.Linq;
using Assets.draco18s.bulletboss.ui;
using Assets.draco18s.training;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Assets.draco18s.bulletboss
{
	public class HostileFighter : MonoBehaviour, IDamageTaker
	{
		public SpriteRenderer image;
		private HardPoint spawnPoint;
		private bool active;

		[UsedImplicitly]
		public UnityEvent<float> OnTakeDamage { get; }

		public float CurHp;
		public float MaxHp;
		private float speed = 0.5f;
		private float expValue = 1;

		public void SetSpawn(HardPoint mount)
		{
			spawnPoint = mount;
			transform.rotation = mount.transform.rotation;
			active = false;
			gameObject.layer = LayerMask.NameToLayer("Default");
			image.color = new Color(1, 1, 1, 0.5f);
			foreach (GunBarrel c in GetComponentsInChildren<GunBarrel>())
			{
				c.active = false;
			}
		}

		public void Spawn(HardPoint mount)
		{
			spawnPoint = mount;
			transform.rotation = mount.transform.rotation;
			transform.position = (mount.transform.position - transform.right*1).ReplaceZ(-1);
			gameObject.layer = LayerMask.NameToLayer("BossEnemy");
			CurHp = MaxHp = mount.GetStat(StatAttribute.FighterHealth, () => 0, (a, b) => a + b);
			speed *= mount.GetStat(StatAttribute.GunSpeed, () => 1, (a, b) => a * b);
			(PatternData d, InventoryItem i) = mount.GetFighterGunPattern();
			//UpgradeScriptable gun = ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => s.data.type == UpgradeType.SmallGun && s.data.rarityTier == NamedRarity.Starting).First();
			//UpgradeScriptable shell = ResourcesManager.instance.GetAssetsMatching<UpgradeScriptable>(s => s.data.type == UpgradeType.Bullet && s.data.rarityTier == NamedRarity.Starting).First();
			//GameObject go = Instantiate(gun.data.relevantPrefab, transform);
			
			GameObject go = transform.GetChild(0).gameObject;
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one * 0.2f;
			GunBarrel bar = go.GetComponent<GunBarrel>();
			bar.SetPattern(d);
			bar.SetShell(i.upgradeTypeData.secondaryPrefab, i.upgradeTypeData.relevantPattern);
			//Debug.Log($"{bar.pattern.effects.AimScreenDown} || {bar.pattern.effects.AimAtPlayer}");
			bar.pattern.effects = i.upgradeTypeData.patternModifiers.Copy();
			//Debug.Log($"{bar.pattern.effects.AimScreenDown} || {bar.pattern.effects.AimAtPlayer}");
			bar.Init();
			image.color = Color.white;
			active = true;
			expValue = 1 + MaxHp + Math.Max((int)i.upgradeTypeData.rarityTier - 2, 0) * 10;
			foreach (GunBarrel c in GetComponentsInChildren<GunBarrel>())
			{
				c.active = true;
			}
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if (!active) return;

			if (CurHp <= 0)
			{
				PlayerCollectable.GenerateDrops(expValue, transform.position);
				Destroy(gameObject);
				return;
			}
			float dt = Time.fixedDeltaTime;
			transform.Translate(new Vector3(1, 0, 0) * dt * speed, Space.Self);
			if (Mathf.Abs(transform.localPosition.x) > 8.5f || transform.localPosition.y < -5.5f || transform.localPosition.y > 7)
			{
				Destroy(gameObject);
			}
		}

		public float GetCurrentHealth()
		{
			return CurHp;
		}

		public float GetMaxHealth()
		{
			return MaxHp;
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			CurHp -= damage;
			OnTakeDamage?.Invoke(damage);
			return damage;
		}
	}
}
