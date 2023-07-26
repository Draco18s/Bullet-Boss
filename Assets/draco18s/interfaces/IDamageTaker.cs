using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s
{
	public interface IDamageTaker
	{
		UnityEvent OnTakeDamage { get; }
		float ApplyDamage(float damage, Collider2D col);
	}
}
