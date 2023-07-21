using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Assets.draco18s.util;

using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using UnityEngine.Profiling;

namespace Assets.draco18s.training
{
	public class AgentShip : Agent, IDamageDealer
	{
		[Flags]
		enum SpriteState
		{
			None = 0,
			MoveL = 1 << 0,
			MoveR = 1 << 1,
			Attack = 1 << 2,
			Bomb = 1 << 3
		}

		public UnityEvent OnTakeDamage { get; set; } = new UnityEvent();

		public Sprite[] TrueIdle;
		public Sprite[] Idle;
		public Sprite[] Attack;
		public Sprite[] Move;
		public GameObject bulletPrefab;
		private GameObject bulletClone;
		public Transform muzzle;
		public Sprite bulletTexture;
		public float attackRate = 0.2f;
		public float speed = 2f;

		public int MaxBombs = 4;
		public int NumBombs = 4;

		public int MaxHP = 30;
		public int CurHP = 30;

		public int NumBullets = 1000;

		private Vector2 previousPos;
		private SpriteState state;
		private SpriteState pState;
		private float cooldown = 0;
		private float spriteTimer = 0;
		private float invulnTime = 0;
		private float totalTime = 0;
		private float distanceMoved = 0;
		private float goalDistance = 0;
		private SpriteRenderer sRenderer;

		public Vector2 MoveInput = Vector2.zero;
		public bool FirePressed = false;
		public bool LeftPressed = false;
		public bool RigtPressed = false;
		public bool BombPressed = false;
		List<Collider2D> BombedBullets = new List<Collider2D>();
		public BufferSensorComponent bulletSensor;
		private bool hideBombedBullets = false;
		private int bulletsDodged = 0;

		private float surviveReward;
		private float totalDamageDealt;
		//private float cachedRewards;
		//private float cachedPenalties;
		List<Collider2D> TakenBullets = new List<Collider2D>();

		private EnvironmentParameters m_ResetParams;
		private StatsRecorder m_Recorder;

		void Start()
		{
			bulletClone = Instantiate(bulletPrefab);
			bulletClone.SetActive(false);
			sRenderer = GetComponentInChildren<SpriteRenderer>();
			sRenderer.color = Random.ColorHSV(0, 1, 1, 1, 1, 1, 1, 1);
			Bullet b = bulletClone.GetComponent<Bullet>();
			b.Init();
			b.pattern.Lifetime = 2;
			b.pattern.timeline.data[PatternDataKey.Size].keys = new[] { new Keyframe(0, 0.6f) };
			b.pattern.timeline.data[PatternDataKey.Speed].keys = new[] { new Keyframe(0, 12f) };
			b.pattern.timeline.data[PatternDataKey.Damage].keys = new[] { new Keyframe(0, 2f) };
			hideBombedBullets = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;
			BufferSensorComponent buf = GetComponent<BufferSensorComponent>();
			m_ResetParams = Academy.Instance.EnvironmentParameters;
			m_Recorder = Academy.Instance.StatsRecorder;
		}

		public override void OnEpisodeBegin()
		{
			float x = Random.value * 6 - 3;
			goalDistance = x;
			x += Math.Sign(x) * 1.85f;
			if (hideBombedBullets)
			{
				CurHP = MaxHP;
				NumBombs = MaxBombs;
			}
			else
			{
				MaxHP = Mathf.FloorToInt(Random.value * 8) + 1;
				MaxBombs = Mathf.FloorToInt(Random.value * 4) + 1;
				CurHP = Mathf.FloorToInt(Random.value * MaxHP) + 1;
				NumBombs = Mathf.FloorToInt(Random.value * MaxBombs) + 1;
			}

			NumBullets = 1000;
			speed = Random.value * 0.5f + 1.75f;
			previousPos = transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
			BombPressed = FirePressed = LeftPressed = RigtPressed = false;
			state = pState = SpriteState.None;
			distanceMoved = totalTime = /*cachedPenalties = cachedRewards =*/ surviveReward = totalDamageDealt = cooldown = spriteTimer = 0;
			invulnTime = 3;
			bulletsDodged = 0;
			BombedBullets.Clear();
			TakenBullets.Clear();
			sRenderer.color = Random.ColorHSV(0, 1, 1, 1, 1, 1, 1, 1);
			OnTakeDamage.Invoke();
		}

		public override void OnActionReceived(ActionBuffers action)
		{
			int moveInput = action.DiscreteActions[0];
			int fireInput = action.DiscreteActions[1];
			int bombInput = action.DiscreteActions[2];

			if (action.ContinuousActions.Length > 0)
			{
				if (fireInput == 1)
				{
					NumBullets--;
					FirePressed = true;
					LeftPressed = false;
					RigtPressed = false;

					MoveInput = Vector2.zero;
				}
				else
				{
					FirePressed = false;
					MoveInput = new Vector2(action.ContinuousActions[0], 0);
				}
			}
			else
			{
				/*if (moveInput == 0)
				{
					FirePressed = false;
					LeftPressed = true;
					RigtPressed = false;
				}
				else if (moveInput == 1)
				{
					LeftPressed = false;
					RigtPressed = false;
					if (fireInput == 1)
					{
						FirePressed = true;
					}
					else
					{
						FirePressed = false;
					}
				}
				else if (moveInput == 2)
				{
					FirePressed = false;
					LeftPressed = false;
					RigtPressed = true;
				
				}*/
			}

			int sx = Math.Sign(transform.localPosition.x);
			int dx = Math.Sign(MoveInput.x);
			if(sx != dx && MathF.Abs(transform.localPosition.x) > 2)
				AddReward(0.1f);
			if (bombInput == 1)
			{
				BombPressed = true;
				if (invulnTime > 0.1f)
				{
					AddReward(-50);
				}
			}
			else
			{
				BombPressed = false;
			}

			/*if (cachedRewards > 0)
			{
				AddReward(cachedRewards);
				cachedRewards = 0;
			}

			if (cachedPenalties > 0)
			{
				AddReward(-cachedPenalties);
				cachedPenalties = 0;
			}*/

			surviveReward += 0.1f;
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			if (BombedBullets.Contains(col)) return 0;
			if (TakenBullets.Contains(col)) return 0;
			TakenBullets.Add(col);
			if (invulnTime > 0) return 0;
			AddReward(-100);
			if (NumBombs > 0)
				AddReward(-200);
			if (hideBombedBullets)
			{
				CurHP -= Mathf.CeilToInt(damage);
				col.GetComponent<SpriteRenderer>().enabled = false;
			}
			else
				CurHP = 0;
			OnTakeDamage.Invoke();
			invulnTime = 0.25f;
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

		public void AddScore(float amt, Collider2D col)
		{
			if (BombedBullets.Contains(col)) return;
			if (TakenBullets.Contains(col)) return;
			//if (amt > 0)
			//	cachedRewards += amt;
			//else
			//	cachedPenalties -= amt;
			if (col.GetComponent<IDamageTaker>() != null && col.gameObject != gameObject)
				totalDamageDealt += amt;
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			// Target and Agent positions
			//sensor.AddObservation(FirePressed ? 1 : 0);
			//sensor.AddObservation(LeftPressed ? 1 : 0);
			//sensor.AddObservation(RigtPressed ? 1 : 0);
			//sensor.AddObservation(BombPressed ? 1 : 0);
			sensor.AddObservation(previousPos / 10);
			sensor.AddObservation(new Vector2(transform.localPosition.x, transform.localPosition.y) / 10);
			sensor.AddObservation((float)CurHP / MaxHP);
			sensor.AddObservation((float)Mathf.Max(NumBombs, 0) / MaxBombs);
			//sensor.AddObservation(speed / 50f);
			sensor.AddObservation(Mathf.Max(invulnTime, 0) / 5f);

			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position + Vector3.up, 5, LayerMask.GetMask(new string[] { "BossEnemy" }));
			Array.Sort(colls, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition, transform.localPosition);
				float db = Vector2.Distance(b.transform.localPosition, transform.localPosition);
				return da.CompareTo(db);
			});
			int inRange = 0;
			for (int j = 0; j < 16 && j < colls.Length; j++)
			{
				//ignore bullets we've bombed
				if (BombedBullets.Contains(colls[j]) || TakenBullets.Contains(colls[j]))
				{
					j++;
					continue;
				}

				//ignore bullets below us
				if (colls[j].transform.localPosition.y < transform.localPosition.y - 1.5f)
				{
					j++;
					continue;
				}

				Bullet b = colls[j].GetComponent<Bullet>();
				if(b == null) continue;
				
				Vector3 p = b.transform.localPosition.ReplaceZ(0);
				Vector3 q = transform.localPosition.ReplaceZ(0);

				Vector3 r = new Vector2(p.x, p.y) - b.previousPosition1;
				Vector3 s = q - new Vector3(q.x - speed * MoveInput.x * Time.fixedDeltaTime, -3.5f, 0);

				float rs = Cross(r, s);
				if (Mathf.Approximately(rs, 0))
				{
					//parallel or (super unlikely) colinear
					//skip
					continue;
				}
				float t = Cross((q - p), s) / Cross(r, s);
				float u = Cross((q - p), r) / Cross(s, r);

				if(t < 0 && u < 0) continue;

				//bulletSensor.AppendObservation(new float[] { b.previousPosition1.x / 10, b.previousPosition1.y / 10, 0 });
				bulletSensor.AppendObservation(new float[] { b.transform.localPosition.x / 10, b.transform.localPosition.y / 10, Mathf.Min(t, u) });
				//bulletSensor.AppendObservation(new float[] { Mathf.Min(t, u), Mathf.Max(t, u) });

				if (Vector2.Distance(b.transform.localPosition, transform.localPosition) <= 2.5f)
					inRange++;
			}

			sensor.AddObservation(inRange / 24f);
		}

		float Cross(Vector2 point1, Vector2 point2)
		{
			return point1.x * point2.y - point1.y * point2.x;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if ((Time.frameCount % 100) == 0)
			{
				m_Recorder.Add("DamageDealt", totalDamageDealt);
				m_Recorder.Add("DistanceMoved", distanceMoved);
				m_Recorder.Add("TimeAlive", totalTime);
				m_Recorder.Add("BulletsDodged", bulletsDodged);
			}
			//sRenderer.enabled = AgentTrainer.instance.simStarted;
			//if (!AgentTrainer.instance.simStarted) return;
			if (CurHP <= 0)
			{
				/*if (cachedRewards > 0)
				{
					AddReward(cachedRewards);
					cachedRewards = 0;
				}

				if (cachedPenalties > 0)
				{
					AddReward(-cachedPenalties);
					cachedPenalties = 0;
				}*/
				if (Math.Abs(transform.localPosition.x) < 0.5f)
				{
					AddReward(100);
				}
				else if (Math.Abs(transform.localPosition.x) < goalDistance + 1.85)
				{
					AddReward(50);
				}
				AddReward(Math.Abs(transform.localPosition.x) * 0.1f);
				//AddReward((goalDistance - distanceMoved) * -0.02f);
				AddReward(totalTime * -0.05f);
				if(distanceMoved < 0)
					AddReward(-50);
				m_Recorder.Add("EndDistance", 6.5f - Math.Abs(transform.localPosition.x));
				Debug.Log($"Dealt {totalDamageDealt} damage. {NumBullets} bullets {NumBombs} bombs. Time Alive {totalTime}. Moved {distanceMoved}");
				EndEpisode();
				//sRenderer.color = Color.gray;
				return;
			}

			float dt = Time.fixedDeltaTime;
			invulnTime -= dt;
			totalTime += dt;
			if (m_ResetParams.GetWithDefault("BossNumber", 0) < 4)
			{
				if (totalTime > 60 || NumBullets <= 0 || Math.Abs(transform.localPosition.x) < 0.5f)
				{
					if (Math.Abs(transform.localPosition.x) < 0.5f)
					{
						AddReward(100);
					}
					else if (Math.Abs(transform.localPosition.x) < goalDistance+1.85)
					{
						AddReward(50);
					}
					AddReward(Math.Abs(transform.localPosition.x) * 0.1f);
					//AddReward((goalDistance - distanceMoved) * -0.02f);
					AddReward(totalTime * -0.05f);
					if (distanceMoved < 1)
						AddReward(-10);
					if (distanceMoved < 0.25f)
						AddReward(-20);
					if (distanceMoved < 0.05f)
						AddReward(-30);

					m_Recorder.Add("EndDistance", 7f - Math.Abs(transform.localPosition.x));
					Debug.Log($"Dealt {totalDamageDealt} damage. {NumBullets} bullets {NumBombs} bombs. Time Alive {totalTime}. Moved {distanceMoved}");
					EndEpisode();
				}
			}
			//RequestDecision();

			BombedBullets.RemoveAll(b => b == null);
			TakenBullets.RemoveAll(b => b == null);

			//lastMovement += dt;
			cooldown -= dt;
			spriteTimer += dt * 8;
			pState = state;
			if (FirePressed)
			{
				spriteTimer += dt * 16;
				Fire();
			}
			else
				state &= ~SpriteState.Attack;

			if (LeftPressed /*&& !FirePressed*/ && !RigtPressed)
				state |= SpriteState.MoveL;
			else
				state &= ~SpriteState.MoveL;
			if (RigtPressed /*&& !FirePressed*/ && !LeftPressed)
				state |= SpriteState.MoveR;
			else
				state &= ~SpriteState.MoveR;
			if (BombPressed)
				state |= SpriteState.Bomb;
			else
				state &= ~SpriteState.Bomb;

			//if (LeftPressed /*&& !FirePressed*/ && !RigtPressed)
			//	ProcessMove(-1 * dt);
			//if (RigtPressed /*&& !FirePressed*/ && !LeftPressed)
			//	ProcessMove(1 * dt);

			if (LeftPressed && !RigtPressed)
				MoveInput = new Vector2(-1, 0);
			if (RigtPressed && !LeftPressed)
				MoveInput = new Vector2(1, 0);

			//MoveInput = new Vector2(Mathf.Clamp(MoveInput.x * 10, -1, 1), 0);
			ProcessMove(MoveInput * dt);

			if (BombPressed && !pState.HasFlag(SpriteState.Bomb))
			{
				ExplodeBomb();
			}

			SpriteState check = SpriteState.MoveR | SpriteState.MoveL;
			if ((check & state) != (pState & check) && !FirePressed)
			{
				spriteTimer = 0;
			}

			ShowSprite();
		}

		private new void AddReward(float f)
		{
			base.AddReward(f/100);
		}

		private void ExplodeBomb()
		{
			if (NumBombs <= 0)
			{
				NumBombs--;
				//this.cachedPenalties += 10f;
				AddReward(-10);
				return;
			}

			NumBombs--;
			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.localPosition, 2.5f, LayerMask.GetMask(new string[] { "BossEnemy" })); //, 0, LayerMask.GetMask(new string[] { "BossEnemy" }));
			BombedBullets.AddRange(colls);
			float f = colls.Length > 0 ? colls.Length * 0.25f : -50;
			AddReward(f);
			if (!this.hideBombedBullets) return;
			foreach (Collider2D c in colls)
			{
				c.GetComponent<SpriteRenderer>().enabled = false;
			}
		}

		private void ProcessMove(Vector2 v)
		{
			previousPos = transform.localPosition;
			transform.Translate(new Vector3(v.x, v.y, 0) * speed);
			if (Mathf.Abs(transform.localPosition.x) > 5.5f)
			{
				transform.localPosition = new Vector3(5.5f * Mathf.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
			}

			distanceMoved += v.magnitude * speed;
		}

		private void ShowSprite()
		{
			int spriteIndex = Mathf.FloorToInt(spriteTimer);
			if (state.HasFlag(SpriteState.Attack))
			{
				if (spriteIndex < Attack.Length)
					sRenderer.sprite = Attack[spriteIndex];
				else
					sRenderer.sprite = TrueIdle[spriteIndex % TrueIdle.Length];
				sRenderer.flipY = false;
			}
			else if (state.HasFlag(SpriteState.MoveL))
			{
				sRenderer.sprite = Move[Math.Min(spriteIndex, Move.Length - 1)];
				sRenderer.flipY = false;
			}
			else if (state.HasFlag(SpriteState.MoveR))
			{
				sRenderer.sprite = Move[Math.Min(spriteIndex, Move.Length - 1)];
				sRenderer.flipY = true;
			}
			else
				sRenderer.sprite = Idle[spriteIndex % Idle.Length];
		}

		private void Fire()
		{
			if (cooldown > 0) return;
			StartCoroutine(Fire2());
		}

		private IEnumerator Fire2()
		{
			cooldown = attackRate;
			state |= SpriteState.Attack;
			spriteTimer = 0;

			yield return new WaitForSeconds(0.15f);
			
			NumBullets--;
			if (m_ResetParams.GetWithDefault("BossNumber", 0) > 1)
			{
				if (Mathf.Abs(transform.localPosition.x) > 2.1f)
				{
					AddReward(-1);
				}
				else
				{
					AddReward(1);
					totalDamageDealt += 1;
				}
			}

			GameObject g = Instantiate(bulletClone, muzzle.position, muzzle.rotation, RandomBossSelector.instance.GameLayer);
			g.SetActive(true);
			g.layer = gameObject.layer;
			g.transform.localScale = Vector3.one * 0.3f;
			g.GetComponent<SpriteRenderer>().sprite = bulletTexture;
			g.GetComponent<SpriteRenderer>().enabled = true;
			Bullet b = g.GetComponent<Bullet>();
			b.pattern.Lifetime = .6f;
			b.pattern.timeline.CopyFrom(bulletClone.GetComponent<Bullet>().pattern.timeline);
			b.playerOwner = this;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			if (BombedBullets.Contains(col)) return 0;
			if (TakenBullets.Contains(col)) return 0;
			bulletsDodged++;
			return 0;
		}
	}
}
