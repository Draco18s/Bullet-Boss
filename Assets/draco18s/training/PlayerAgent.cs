using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		public Vector2 Velocity;
		public float multi = 10;
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

		public float predictionRadius = 0.2f;

		public override void OnEpisodeBegin()
		{
			GetComponentInChildren<TrainingDodgeDetection>().SetColliders();
			if (GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining
			    && GameStateManager.instance.state != GameStateManager.GameState.RecordDemo) return;
			transform.localPosition = new Vector3(Random.value * 10 - 5, Random.value * 5 - 3.5f, transform.localPosition.z);
			//transform.localPosition = new Vector3(Random.value * 10 - 5, -5, transform.localPosition.z);
			//entryAnim = StartCoroutine(AnimateEntry(new Vector3(transform.localPosition.x, -3.5f, 0)));
			Collider2D[] stuff = Physics2D.OverlapCircleAll(transform.position, 3, LayerMask.GetMask(new[] { "BossBullets", "BossEnemy", "PlayerCollectable" }));
			foreach (Collider2D col in stuff)
			{
				Destroy(col.gameObject);
			}
			ShipAcademy.instance.ConfigureEnvironment();
		}

		private IEnumerator AnimateEntry(Vector3 final)
		{
			while (transform.localPosition.y < -4f)
			{
				yield return null;
				transform.Translate(Vector3.up * speed * 1.5f * Time.fixedDeltaTime, Space.Self);
			}

			entryAnim = null;
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			sensor.AddObservation((Vector2)transform.localPosition / 10f);
			sensor.AddObservation(Velocity / 10f);
			Vector3 p1 = transform.position + Vector3.left * 1 + Vector3.down * 1.5f;
			Vector3 p2 = transform.position + Vector3.right * 1 + Vector3.up * 5;
			List<Collider2D> bullets = new List<Collider2D>();

			Collider2D[] _bts = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[]{ "BossBullets" }));
			bullets.AddRange(_bts);

			Debug.DrawLine(p1, p2.ReplaceY(p1.y));
			Debug.DrawLine(p1, p2.ReplaceX(p1.x));
			Debug.DrawLine(p1.ReplaceY(p2.y), p2);
			Debug.DrawLine(p1.ReplaceX(p2.x), p2);

			p1 = transform.position + Vector3.left * 2 + Vector3.down * 1;
			p2 = transform.position + Vector3.right * 2 + Vector3.up * 4;

			_bts = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "BossBullets" }));
			bullets.AddRange(_bts);

			Debug.DrawLine(p1, p2.ReplaceY(p1.y));
			Debug.DrawLine(p1, p2.ReplaceX(p1.x));
			Debug.DrawLine(p1.ReplaceY(p2.y), p2);
			Debug.DrawLine(p1.ReplaceX(p2.x), p2);

			p1 = transform.position + Vector3.left * 4 + Vector3.up * 1;
			p2 = transform.position + Vector3.right * 4 + Vector3.up * 3;

			_bts = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "BossBullets" }));
			bullets.AddRange(_bts);

			Debug.DrawLine(p1,                p2.ReplaceY(p1.y));
			Debug.DrawLine(p1,                p2.ReplaceX(p1.x));
			Debug.DrawLine(p1.ReplaceY(p2.y), p2);
			Debug.DrawLine(p1.ReplaceX(p2.x), p2);

			bullets = bullets.Distinct().ToList();
			bullets.Sort((a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition, transform.localPosition);
				float db = Vector2.Distance(b.transform.localPosition, transform.localPosition);
				return da.CompareTo(db);
			});

			p1 = transform.position + Vector3.left * 15 + Vector3.down * 4;
			p2 = transform.position + Vector3.right * 15 + Vector3.up * 6;

			_bts = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "BossBullets" }));
			foreach (Collider2D c in _bts)
			{
				Bullet bul = c.GetComponent<Bullet>();
				if (bul == null)
				{
					continue;
				}
				if (bul.spriteRenderer != null)
					bul.spriteRenderer.color = new Color(1, 1, 1, GameStateManager.instance.state == GameStateManager.GameState.RecordDemo ? 0 : 0.5f);
			}

			List<Bullet> skip = new List<Bullet>();
			int i = 0;
			int j = 0;
			// add all bullets that might intersect
			for (j = 0; j < 24; j++)
			{
				if (i >= bullets.Count)
				{
					break;
				}
				Bullet bul = bullets[i++].GetComponent<Bullet>();
				if (bul == null)
				{
					j--;
					continue;
				}
				Debug.DrawLine(transform.position, bul.transform.position, Color.cyan);
				if(bul.spriteRenderer != null)
					bul.spriteRenderer.color = new Color(1, 1, 1, 0.5f);

				Vector2 bulVel = bul.transform.localPosition - bul.previousPosition1;
				bulVel /= Time.fixedDeltaTime; //one second prediction

				Vector2 s = bul.transform.localPosition - transform.localPosition; // vector between the centers of each sphere
				Vector2 v = bulVel - (Velocity * speed); // relative velocity between spheres

				v *= 2;
				float dist = Vector2.Distance(transform.position, bul.transform.position);
				if (dist <= 1) //add any bullet that is very close
				{
					skip.Add(bul);
					sensor.AddObservation((Vector2)bul.previousPosition1 / 10f);
					sensor.AddObservation((Vector2)bul.transform.localPosition / 10f);
					if (bul.spriteRenderer != null)
						bul.spriteRenderer.color = Color.white;
					continue;
				}


				float r = predictionRadius + dist * 0.25f * speed;
				float c = Vector2.Dot(s,s) - r * r;
				if (c < 0) // if negative, they overlap; kinda too late
				{
					skip.Add(bul);
					sensor.AddObservation((Vector2)bul.previousPosition1 / 10f);
					sensor.AddObservation((Vector2)bul.transform.localPosition / 10f);
					if (bul.spriteRenderer != null)
						bul.spriteRenderer.color = Color.white;
					continue;
				}
				float a = Vector2.Dot(v, v);
				float b = Vector2.Dot(v, s);
				float d = b * b - a * c;
				if (b >= 0.0 || d < 0)// do not move towards each other OR no real roots ... no collision
				{
					j--;
					continue;
				}
				skip.Add(bul);
				sensor.AddObservation((Vector2)bul.previousPosition1 / 10f);
				sensor.AddObservation((Vector2)bul.transform.localPosition / 10f);
				if (bul.spriteRenderer != null)
					bul.spriteRenderer.color = Color.white;
			}
			/*
			for (int i = 0; i < 12; i+=4)
			{
				Vector2 p1 = new Vector2(observation[i + o + 0], observation[i + o + 1]) * 20;
				Vector2 p0 = new Vector2(observation[i + o + 2], observation[i + o + 3]) * 20;
				float max = 1.5f - Spd;
				for (float dt = Spd * i; dt < max; dt += 0.1f)
				{
					Vector2 p3 = p1 + (p1 - p0) * dt;
					if (Vector2.Distance(p3, p) < .20 + Spd*3)
					{
						if (p.x < p3.x)
							dir += -1 * (max - dt);
						if (p.x > p3.x)
							dir += 1 * (max - dt);
					}
				}
			
			}
			*/
			i = 0;
			//add any remaining bullets up to a max of 24
			for (; j < 24; j++)
			{
				if (i >= bullets.Count)
				{
					sensor.AddObservation(Vector2.one * -10 / 10f);
					sensor.AddObservation(Vector2.one * -10 / 10f);
					continue;
				}
				Bullet bul = bullets[i++].GetComponent<Bullet>();
				if (bul == null || skip.Contains(bul))
				{
					j--;
					continue;
				}
				float dist = Vector2.Distance(transform.position, bul.transform.position);
				if (dist < 2.5f)
				{
					sensor.AddObservation((Vector2)bul.previousPosition1 / 10f);
					sensor.AddObservation((Vector2)bul.transform.localPosition / 10f);
					if (bul.spriteRenderer != null)
						bul.spriteRenderer.color = new Color(0.5f, 1, 0.5f, 0.5f);
				}
				else
				{
					sensor.AddObservation(Vector2.one * -10 / 10f);
					sensor.AddObservation(Vector2.one * -10 / 10f);
					Debug.DrawLine(transform.position, bul.transform.position, Color.red);
				}
			}
			
			Collider2D[] enemies = Physics2D.OverlapAreaAll(p1, p2, LayerMask.GetMask(new[] { "BossEnemy" }));

			Array.Sort(enemies, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition, transform.localPosition);
				float db = Vector2.Distance(b.transform.localPosition, transform.localPosition);
				return da.CompareTo(db);
			});
			i = 0;
			for (j = 0; j < 4; j++)
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
				float da = Vector2.Distance(a.transform.localPosition, transform.localPosition);
				float db = Vector2.Distance(b.transform.localPosition, transform.localPosition);
				return da.CompareTo(db);
			});
			i = 0;
			for (j = 0; j < 4; j++)
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

		private Queue<Vector2> prevMoves = new Queue<Vector2>();
		private readonly int cachedLength = 64;

		public override void OnActionReceived(ActionBuffers actionBuffers)
		{
			Vector2 newMove = new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
			if (GameStateManager.instance.state == GameStateManager.GameState.RecordDemo)
			{
				newMove = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			}
			prevMoves.Enqueue(MoveInput);
			if (prevMoves.Count > cachedLength)
			{
				_ = prevMoves.Dequeue();
				//Debug.Log($"({FastFourier.ProcessData(prevMoves.Select(v => v.x).ToList(), 0.5f)[cachedLength / 2].x},{FastFourier.ProcessData(prevMoves.Select(v => v.y).ToList(), 0.5f)[cachedLength / 2].x})");
				if (FastFourier.ProcessData(prevMoves.Select(v => v.x).ToList(), 0.5f)[cachedLength/2].x < -1)
				{
					AddReward(-0.0025f);
				}
				if (FastFourier.ProcessData(prevMoves.Select(v => v.y).ToList(), 0.5f)[cachedLength/2].x < -1)
				{
					AddReward(-0.0025f);
				}
			}
			MoveInput = newMove;
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
			if (GameStateManager.instance.state == GameStateManager.GameState.ActiveTraining || GameStateManager.instance.state == GameStateManager.GameState.RecordDemo)
			{
				AddReward(-1);
			}

			if (GameStateManager.instance.state == GameStateManager.GameState.InGame)
			{
				if (invulnTime > 0 || entryAnim != null) return 0;
				CurHp -= damage;
				invulnTime = 0.5f;
				OnTakeDamage.Invoke(damage);
				if (CurHp <= 0)
				{
					Inventory.instance.AddKill();
					CurHp = MaxHp;
					invulnTime = 2f;
					OnTakeDamage.Invoke(0);
				}

				GetComponentInChildren<SpriteRenderer>().color = new Color(1, 1, 1, 0.4f);
			}
			return damage;
		}

		private float maxGameTime = 120;
		
		[UsedImplicitly]
		void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;
			if (GameStateManager.instance.state == GameStateManager.GameState.InGame) UpdateForGamePlay(dt);
			if (GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining
			    && GameStateManager.instance.state != GameStateManager.GameState.RecordDemo) return;
			maxGameTime -= dt;
			if (maxGameTime <= 0)
			{
				maxGameTime = 120;
				FindFirstObjectByType<TrainingBoss>()?.UpdateValues(GetCumulativeReward());
				EndEpisode();
			}

			float d = Math.Abs(transform.localPosition.x) - 2;
			if (d <= 0) d = -6.5f;
			AddReward(-0.0002f * d/6.5f);
			Velocity = Vector2.Lerp(Velocity, MoveInput, multi);
			transform.Translate(Velocity * dt * speed, Space.Self);
			if (Mathf.Abs(transform.localPosition.x) > 6.5f)
			{
				Velocity.x = 0;
				transform.localPosition = new Vector3(6.5f * Math.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
				if (GameStateManager.instance.state != GameStateManager.GameState.RecordDemo)
				AddReward(-0.005f);
			}
			if (transform.localPosition.y > 1 || transform.localPosition.y < -4f)
			{
				Velocity.y = 0;
				float ny = (transform.localPosition.y > 1 ? 1 : -4f);
				transform.localPosition = new Vector3(transform.localPosition.x, ny, transform.localPosition.z);
				if(GameStateManager.instance.state != GameStateManager.GameState.RecordDemo)
					AddReward(-0.005f);
			}

			AddReward(-Mathf.Pow(MoveInput.magnitude, 0.25f) * 0.0002f);
			cooldown -= dt;
			Fire();
		}

		private void UpdateForGamePlay(float dt)
		{
			if (entryAnim != null) return;
			invulnTime -= dt;
			if (invulnTime <= 0)
				GetComponentInChildren<SpriteRenderer>().color = Color.white;
			Velocity = Vector2.Lerp(Velocity, MoveInput, multi);
			transform.Translate(Velocity * dt * speed, Space.Self);
			if (Mathf.Abs(transform.localPosition.x) > 6.5f)
			{
				transform.localPosition = new Vector3(6.5f * Math.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
			}
			if (transform.localPosition.y > -1 || transform.localPosition.y < -4f)
			{
				float ny = (transform.localPosition.y > -1 ? -1 : -4f);
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
