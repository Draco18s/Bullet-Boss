using System.Collections.Generic;
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
			transform.position = (mount.transform.position - transform.right*1).ReplaceZ(-1);
			CurHp = MaxHp = mount.GetStat(StatAttribute.FighterHealth, (a, b) => a + b);
			speed *= mount.GetStat(StatAttribute.GunSpeed, (a, b) => a + b);
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
			bar.pattern.effects.MergeWith(i.upgradeTypeData.patternModifiers);
			bar.Init();
			image.color = Color.white;
			active = true;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if (!active) return;

			if (CurHp <= 0)
			{
				GenerateDrops(MaxHp);
				Destroy(gameObject);
				return;
			}
			float dt = Time.fixedDeltaTime;
			transform.Translate(new Vector3(1, 0, 0) * dt, Space.Self);
		}

		private void GenerateDrops(float value)
		{
			List<PlayerBuffScriptable> gems = ResourcesManager.instance.GetAssetsMatching<PlayerBuffScriptable>(s => s.BonusType == PlayerBuffScriptable.BuffType.Score && s.ScoreValue <= value);
			gems.Sort((g,h) => h.ScoreValue.CompareTo(g.ScoreValue));
			while (value > 0 || gems.Count == 0)
			{
				if (gems[0].ScoreValue >= value)
				{
					Drop(gems[0].RelevantPrefab, value / gems[0].ScoreValue);
					value -= gems[0].ScoreValue * (value / gems[0].ScoreValue);
				}
				else
				{
					gems.RemoveAt(0);
				}
			}
		}

		private void Drop(GameObject gem, float num)
		{
			Instantiate(gem, transform.localPosition + (Vector3)Random.insideUnitCircle, Quaternion.identity, transform.parent);
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			CurHp -= damage;
			OnTakeDamage.Invoke();
			return damage;
		}
	}
}
