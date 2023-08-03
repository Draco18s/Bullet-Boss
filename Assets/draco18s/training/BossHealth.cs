using Assets.draco18s.bulletboss;
using Assets.draco18s.util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.training
{
	internal class BossHealth : MonoBehaviour, IDamageTaker
	{
		public UnityEvent<float> OnTakeDamage { get; set; } = new UnityEvent<float>();
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
			Debug.Log(damage);
			OnTakeDamage.Invoke(damage);
			PlayerCollectable.GenerateDrops(1, col.transform.position);
			return damage;
		}
	}
}
