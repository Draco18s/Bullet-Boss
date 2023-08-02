using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.training
{
	public class ShipGraze : MonoBehaviour, IDamageTaker
	{
		// NOT BEING USED
		// GOTO TrainingDodgeDetection!!
		public UnityEvent OnTakeDamage { get; set; } = new UnityEvent();

		private IDamageDealer parent;
		public float GetCurrentHealth()
		{
			return 0;
		}

		public float GetMaxHealth()
		{
			return 0;
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			return 0;
		}

		[UsedImplicitly]
		void Start()
		{
			parent = GetComponentInParent<IDamageDealer>();
		}

		/*[UsedImplicitly]
		private void OnCollisionStay2D(Collision2D col)
		{
			if (col.gameObject.layer != this.gameObject.layer)
			{
				
			}
		}*/
	}
}