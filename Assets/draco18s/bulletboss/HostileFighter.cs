using Assets.draco18s.bulletboss.ui;
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

		public void SetSpawn(HardPoint mount)
		{
			spawnPoint = mount;
			transform.rotation = mount.transform.rotation;
			active = false;
			gameObject.layer = LayerMask.NameToLayer("Default");
			image.color = new Color(1, 1, 1, 0.5f);
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
