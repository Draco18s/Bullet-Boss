using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s
{
	public interface IDamageTaker
	{
		UnityEvent<float> OnTakeDamage { get; }
		public float GetCurrentHealth();
		public float GetMaxHealth();
		float ApplyDamage(float damage, Collider2D bulletCol);
	}
}
