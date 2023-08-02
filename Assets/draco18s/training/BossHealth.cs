using Assets.draco18s.bulletboss;
using Assets.draco18s.util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.training
{
	internal class BossHealth : MonoBehaviour, IDamageTaker
	{
		public UnityEvent OnTakeDamage { get; set; } = new UnityEvent();
		public float GetCurrentHealth()
		{
			return -1;
		}

		public float GetMaxHealth()
		{
			return -1;
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			GenerateDrops(1, col.transform.position);
			return damage;
		}

		private void GenerateDrops(float value, Vector3 pos)
		{
			float val = value;
			List<PlayerBuffScriptable> gems = ResourcesManager.instance.GetAssetsMatching<PlayerBuffScriptable>(s => s.BonusType == PlayerBuffScriptable.BuffType.Score && s.ScoreValue <= val);
			gems.Sort((g, h) => h.ScoreValue.CompareTo(g.ScoreValue));

			while (value > 0 && gems.Count > 0)
			{
				if (gems[0].ScoreValue <= value)
				{
					Drop(gems[0].RelevantPrefab, value / gems[0].ScoreValue, pos);
					value -= Mathf.Max(gems[0].ScoreValue * (value / gems[0].ScoreValue), 1);
				}
				else if (gems[0].ScoreValue > 1)
				{
					gems.RemoveAt(0);
				}
			}
		}

		private void Drop(GameObject gem, float num, Vector3 pos)
		{
			for (; num-- > 0;)
			{
				if (Random.value > ShipAcademy.instance.GemDropRate) continue;
				if (Random.value > ShipAcademy.instance.GemDropRate) continue;
				Instantiate(gem, pos + (Vector3)Random.insideUnitCircle * 0.5f, Quaternion.identity, transform.parent);
			}
		}
	}
}
