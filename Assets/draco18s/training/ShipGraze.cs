using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.training
{
	public class ShipGraze : MonoBehaviour, IDamageTaker
	{
		public UnityEvent OnTakeDamage { get; set; } = new UnityEvent();
		private IDamageDealer parent;

		[UsedImplicitly]
		void Start()
		{
			parent = GetComponentInParent<IDamageDealer>();
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			parent.ApplyGraze(1, col);
			//parent.AddScore(0.00025f, col);
			return 0;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			return 0;
		}

		[UsedImplicitly]
		private void OnCollisionStay2D(Collision2D col)
		{
			if (col.gameObject.layer != this.gameObject.layer)
			{
				parent.AddScore(0.5f * Time.fixedDeltaTime, col.gameObject.GetComponent<Collider2D>());
			}
		}
	}
}