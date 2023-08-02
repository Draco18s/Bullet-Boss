using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.training
{
	public class TrainingDodgeDetection : MonoBehaviour
	{
		public PlayerAgent agent;
		private int bulletLayer;
		private List<Collider2D> trackedObjects;

		[UsedImplicitly]
		void Start()
		{
			bulletLayer = LayerMask.NameToLayer("BossBullets");
			trackedObjects = new List<Collider2D>();
		}

		[UsedImplicitly]
		private void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.layer != bulletLayer) return;
			trackedObjects.Add(col);
		}

		[UsedImplicitly]
		private void OnTriggerExit2D(Collider2D col)
		{
			if (col.gameObject.layer != bulletLayer) return;
			trackedObjects.Remove(col);
			StartCoroutine(CheckFor(col));
		}

		private IEnumerator CheckFor(Collider2D col)
		{
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			if (col != null)
			{
				agent.AddReward(0.05f);
			}
		}

		/*public UnityEvent OnTakeDamage { get; }
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
			agent.AddReward(0.05f);
			return 0;
		}*/
	}
}