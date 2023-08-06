using System;
using System.Collections;
using Assets.draco18s.bulletboss;
using Assets.draco18s.bulletboss.ui;
using Assets.draco18s.util;
using JetBrains.Annotations;
using Mono.Cecil;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Integrations.Match3;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets.draco18s.training
{
	public class PlayerAgent : Agent, IDamageDealer
	{
		public UnityEvent<float> OnTakeDamage { get; } = new UnityEvent<float>();
		public Vector2 MoveInput;
		private float speed = 2;
		private float CurHp = 5;
		private float MaxHp = 5;
		private float invulnTime = 0;
		private float Pickups = 0;
		private float CurCharge = 0;
		private float MaxCharge = 100;

		private float cooldown = 0;
		private float attackRate = 0.2f;
		public Transform muzzle;
		public GameObject bulletClone;

		private Coroutine entryAnim;

		public override void OnEpisodeBegin()
		{
			GetComponentInChildren<TrainingDodgeDetection>().SetColliders();
			if (GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining) return;
			transform.localPosition = new Vector3(Random.value * 10 - 5, -5, transform.localPosition.z);
			entryAnim = StartCoroutine(AnimateEntry(new Vector3(transform.localPosition.x, -3.5f, 0)));
			Collider2D[] stuff = Physics2D.OverlapCircleAll(transform.position, 3, LayerMask.GetMask(new[] { "BossBullets", "BossEnemy", "PlayerCollectable" }));
			foreach (Collider2D col in stuff)
			{
				Destroy(col.gameObject);
			}
			ShipAcademy.instance.ConfigureEnvironment();
		}

		private IEnumerator AnimateEntry(Vector3 final)
		{
			while (transform.localPosition.y < -3.5f)
			{
				yield return null;
				transform.Translate(Vector3.up * speed * 1.5f * Time.fixedDeltaTime, Space.Self);
			}

			entryAnim = null;
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			sensor.AddObservation((Vector2)transform.localPosition / 10f);
			sensor.AddObservation(MoveInput / 10f);
			Vector3 p1 = transform.position + Vector3.left * 2 + Vector3.down * 1;
			Vector3 p2 = transform.position + Vector3.right * 2 + Vector3.up * 4;
			Collider2D[] bullets = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[]{ "BossBullets" }));

			Array.Sort(bullets, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				float db = Vector2.Distance(b.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				return da.CompareTo(db);
			});
			int i = 0;
			for (int j = 0; j < 24; j++)
			{
				if (i >= bullets.Length)
				{
					sensor.AddObservation(Vector2.up * -10 / 10f);
					continue;
				}
				Bullet b = bullets[i++].GetComponent<Bullet>();
				if (b == null)
				{
					j--;
					continue;
				}

				Vector3 p = b.transform.localPosition.ReplaceZ(0);
				Vector3 q = transform.localPosition.ReplaceZ(0);

				Vector3 r = new Vector3(p.x, p.y, 0) - b.previousPosition1.ReplaceZ(0);
				Vector3 s = q - new Vector3(q.x - speed * MoveInput.x * Time.fixedDeltaTime, -3.5f, 0);

				float rs = Cross(r, s);
				if (Mathf.Approximately(rs, 0))
				{
					sensor.AddObservation(Vector2.up * -10 / 10f);
					continue;
				}
				float t = Cross((q - p), s) / Cross(r, s);
				float u = Cross((q - p), r) / Cross(s, r);

				/*if (t < 0 && u < 0)
				{
					sensor.AddObservation(Vector3.up * -10);
					continue;
				}*/
				//sensor.AddObservation((Vector2)b.previousPosition1.ReplaceZ(0) / 10f);
				sensor.AddObservation((Vector2)b.transform.localPosition / 10f);
			}

			Collider2D[] enemies = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "BossEnemy" }));

			Array.Sort(enemies, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				float db = Vector2.Distance(b.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				return da.CompareTo(db);
			});
			i = 0;
			for (int j = 0; j < 4; j++)
			{
				if (i >= enemies.Length)
				{
					sensor.AddObservation(Vector2.up * -10 / 10f);
					continue;
				}
				sensor.AddObservation((Vector2)enemies[i].transform.localPosition / 10f);
				i++;
			}

			Collider2D[] collecables = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "PlayerCollectable" }));

			Array.Sort(collecables, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				float db = Vector2.Distance(b.transform.localPosition.ReplaceZ(0), transform.localPosition.ReplaceZ(0));
				return da.CompareTo(db);
			});
			i = 0;
			for (int j = 0; j < 2; j++)
			{
				if (i >= collecables.Length)
				{
					sensor.AddObservation(Vector2.up * -10 / 10f);
					continue;
				}
				sensor.AddObservation((Vector2)collecables[i].transform.localPosition / 10f);
				i++;
			}
		}

		float Cross(Vector3 point1, Vector3 point2)
		{
			//we don't care about z
			return point1.x * point2.y - point1.y * point2.x;
		}

		public override void OnActionReceived(ActionBuffers actionBuffers)
		{
			MoveInput = new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
		}
		public float GetCurrentHealth()
		{
			return GameStateManager.instance.state == GameStateManager.GameState.ActiveTraining ? 1 : CurHp;
		}

		public float GetMaxHealth()
		{
			return GameStateManager.instance.state == GameStateManager.GameState.ActiveTraining ? 1 : MaxHp;
		}

		public float ApplyDamage(float damage, Collider2D bulletCol)
		{
			if (GameStateManager.instance.state == GameStateManager.GameState.ActiveTraining)
			{
				SetReward(-1);
				EndEpisode();
			}

			if (GameStateManager.instance.state == GameStateManager.GameState.InGame)
			{
				if (invulnTime > 0 || entryAnim != null) return 0;
				CurHp -= damage;
				invulnTime = 2;
				OnTakeDamage.Invoke(damage);
				if (CurHp <= 0)
				{
					Inventory.instance.AddKill();
					CurHp = MaxHp;
					invulnTime = 0.5f;
					EndEpisode();
					OnEpisodeBegin();
					OnTakeDamage.Invoke(0);
				}
			}
			return damage;
		}
		
		[UsedImplicitly]
		void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;
			if (GameStateManager.instance.state == GameStateManager.GameState.InGame) UpdateForGamePlay(dt);
			if (GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining) return;
			float d = Vector2.Distance(transform.localPosition, new Vector2(0, -3.5f));
			AddReward(0.001f + 0.001f * (6.5f - d)/6.5f);
			transform.Translate(MoveInput * dt * speed, Space.Self);
			if (Mathf.Abs(transform.localPosition.x) > 6.5f)
			{
				transform.localPosition = new Vector3(6.5f * Math.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
				AddReward(-0.005f);
			}
			if (transform.localPosition.y > -1 || transform.localPosition.y < -3.5f)
			{
				float ny = (transform.localPosition.y > -1 ? -1 : -3.5f);
				transform.localPosition = new Vector3(transform.localPosition.x, ny, transform.localPosition.z);
				AddReward(-0.005f);
			}
			cooldown -= dt;
			Fire();
		}

		private void UpdateForGamePlay(float dt)
		{
			if (entryAnim != null) return;
			invulnTime -= dt;
			transform.Translate(MoveInput * dt * speed, Space.Self);
			if (Mathf.Abs(transform.localPosition.x) > 6.5f)
			{
				transform.localPosition = new Vector3(6.5f * Math.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
			}
			if (transform.localPosition.y > -1 || transform.localPosition.y < -3.5f)
			{
				float ny = (transform.localPosition.y > -1 ? -1 : -3.5f);
				transform.localPosition = new Vector3(transform.localPosition.x, ny, transform.localPosition.z);
			}
			cooldown -= dt;
			Fire();
		}

		[UsedImplicitly]
		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			bulletClone = Instantiate(GameTransform.instance.basicBulletPrefab);
			bulletClone.SetActive(false);
			bulletClone.layer = gameObject.layer + 2;
			Bullet b = bulletClone.GetComponent<Bullet>();
			b.Init();
			b.pattern.Lifetime = 2;
			b.pattern.timeline.data[PatternDataKey.Size].keys = new[] { new Keyframe(0, 0.6f) };
			b.pattern.timeline.data[PatternDataKey.Speed].keys = new[] { new Keyframe(0, 12f) };
		}

		private void Fire()
		{
			if (cooldown > 0) return;
			StartCoroutine(Fire2());
		}

		private IEnumerator Fire2()
		{
			cooldown = attackRate;

			yield return new WaitForSeconds(0.15f);
			
			GameObject g = Instantiate(bulletClone, muzzle.position, muzzle.rotation, GameTransform.instance.transform);
			g.SetActive(true);
			g.layer = gameObject.layer + 2;
			g.transform.localScale = Vector3.one * 0.3f;
			//g.GetComponent<SpriteRenderer>().sprite = bulletTexture;
			g.GetComponent<SpriteRenderer>().enabled = true;
			Bullet b = g.GetComponent<Bullet>();
			b.pattern.dataValues[PatternDataKey.Speed] = 0.5f;
			b.pattern.Lifetime = 2.6f;
			b.SetDamage(1);
			b.pattern.timeline.CopyFrom(bulletClone.GetComponent<Bullet>().pattern.timeline);
			b.playerOwner = this;
		}

		public float ApplyGraze(float damage, Collider2D bulletCol)
		{
			if (GameStateManager.instance.state == GameStateManager.GameState.InGame)
			{
				CurCharge = Mathf.Clamp(CurCharge + Time.fixedDeltaTime, 0, MaxCharge);
			}
			return damage;
		}

		public void DamagedEnemy(float damage, Collider2D enemyCol)
		{
			AddReward(0.001f);
		}

		public void AddScore(float points)
		{
			Pickups += points;
		}
	}
}
