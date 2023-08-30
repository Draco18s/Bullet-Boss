using Assets.draco18s;
using Assets.draco18s.training;
using Assets.draco18s.util;
using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class PlayerCollectable : MonoBehaviour
	{
		public Sprite image;
		public Vector2 previousPosition1;
		public Vector2 previousPosition2;
		public Vector2 previousPosition3;
		public float speed = 3 / 5f;
		public float value = 1;

		[UsedImplicitly]
		void Start()
		{
			//GetComponent<SpriteRenderer>().sprite = image;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			previousPosition1 = previousPosition2;
			previousPosition2 = previousPosition3;
			previousPosition3 = transform.localPosition;
			float dt = Time.fixedDeltaTime;

			transform.Translate(new Vector3(0, -1 * dt * speed, dt), Space.Self);

			if (transform.localPosition.y < -5)
			{
				Destroy(gameObject);
			}
		}

		[UsedImplicitly]
		void OnTriggerEnter2D(Collider2D other)
		{
			if (other.gameObject.layer == LayerMask.NameToLayer("AIPlayer"))
			{
				PlayerAgent ag = other.GetComponentInParent<PlayerAgent>();
				ag.AddReward(0.15f);
				ag.AddScore(value); //this is "game score" not AI training
				Destroy(gameObject);
			}
		}

		public static void GenerateDrops(float value, Vector3 pos)
		{
			float val = value;
			List<PlayerBuffScriptable> gems = ResourcesManager.instance.GetAssetsMatching<PlayerBuffScriptable>(s => s.BonusType == PlayerBuffScriptable.BuffType.Score && s.ScoreValue <= val);
			gems.Sort((g, h) => h.ScoreValue.CompareTo(g.ScoreValue));

			while (value > 0 && gems.Count > 0)
			{
				if (gems[0].ScoreValue <= value)
				{
					Drop(gems[0].RelevantPrefab, value / gems[0].ScoreValue, pos);
					value -= Mathf.Max(gems[0].ScoreValue * (value / gems[0].ScoreValue), 1);
				}
				else if (gems[0].ScoreValue > 1)
				{
					gems.RemoveAt(0);
				}
			}
		}

		private static void Drop(GameObject gem, float num, Vector3 pos)
		{
			for (; num-- > 0;)
			{
				if (Random.value > ShipAcademy.instance.GemDropRate) continue;
				if (Random.value > ShipAcademy.instance.GemDropRate) continue;
				Instantiate(gem, pos + (Vector3)Random.insideUnitCircle * 0.5f, Quaternion.identity, GameTransform.instance.transform);
			}
		}
	}
}