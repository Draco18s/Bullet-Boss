using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.bulletboss.util
{
	public class TestPlayer : MonoBehaviour, IDamageTaker
	{
		public UnityEvent<float> OnTakeDamage { get; set; }

		private void Start()
		{
			gameObject.layer = LayerMask.NameToLayer("AIPlayer");
		}

		public float GetCurrentHealth()
		{
			return 1;
		}

		public float GetMaxHealth()
		{
			return 1;
		}

		public float ApplyDamage(float damage, Collider2D bulletCol)
		{
			return damage;
		}
	}
}