using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.bulletboss
{
	public class PlayerFighter : MonoBehaviour, IDamageDealer
	{
		public UnityEvent OnTakeDamage { get; }
		public float CurHP;
		public float MaxHP;

		[UsedImplicitly]
		void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;

		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			CurHP -= damage;
			return damage;
		}

		public float GetCurrentHealth()
		{
			return CurHP;
		}

		public float GetMaxHealth()
		{
			return MaxHP;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			return 0;
		}

		public void DamagedEnemy(float damage, Collider2D enemyCol)
		{
			
		}
	}
}
