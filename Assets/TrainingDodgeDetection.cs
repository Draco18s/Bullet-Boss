using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.bulletboss;
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

		public void SetColliders()
		{
			GetComponent<BoxCollider2D>().enabled = GameStateManager.instance.state == GameStateManager.GameState.ActiveTraining;
			GetComponent<CircleCollider2D>().enabled = GameStateManager.instance.state == GameStateManager.GameState.InGame;
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
			if (col.gameObject.layer != bulletLayer || GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining) return;
			trackedObjects.Remove(col);
			StartCoroutine(CheckFor(col));
		}

		[UsedImplicitly]
		private void OnTriggerStay2D(Collider2D col)
		{
			if (col.gameObject.layer != bulletLayer || GameStateManager.instance.state != GameStateManager.GameState.InGame) return;
			agent.ApplyGraze(1, col);
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
	}
}