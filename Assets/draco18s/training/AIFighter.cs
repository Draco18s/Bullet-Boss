using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Assets.draco18s;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using static SimTrainer;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Assets.draco18s.training
{
	public class AIFighter : MonoBehaviour, IDamageDealer
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
		private int NumBombs = 4;

		public int MaxHP = 30;
		public int CurHP = 30;

		private SpriteState state;
		private SpriteState pState;
		private float cooldown = 0;
		private float spriteTimer = 0;
		private SpriteRenderer sRenderer;

		public bool FirePressed = false;
		public bool LeftPressed = false;
		public bool RigtPressed = false;
		public bool BombPressed = false;

		public bool Training = false;

		public float[] lastDecision;
		public float lastActionScore;
		public float damageScore = 0;
		public float decisionPeriod = .75f;
		public float decisionOutcomeCheckDelay = 0.1f;

		SimTrainer simController;
		PlayerObservationDecision decisionFrame;

		//private float lastMovement = 0;
		float lastMovement_t;
		float modifiedDecisionPeriod;

		Noedify_Solver evalSolver;

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

			if (simController == null)
				simController = SimTrainer.instance;
			evalSolver = Noedify.CreateSolver();

			if (Training)
			{
				lastMovement_t = -decisionPeriod;
				modifiedDecisionPeriod = decisionPeriod;
			}
		}

		public float GetCurrentHealth()
		{
			return CurHP;
		}

		public float GetMaxHealth()
		{
			return MaxHP;
		}

		// Update is called once per frame
		[UsedImplicitly]
		void Update()
		{
			sRenderer.enabled = SimTrainer.instance.simStarted;
			if (!SimTrainer.instance.simStarted) return;

			if (IsDead())
			{
				sRenderer.color = Color.gray;
				return;
			}

			BombedBullets.RemoveAll(b => b == null);

			float[] samples = AcquireObservations();

			if ((Time.time - lastMovement_t) > modifiedDecisionPeriod && CurHP > 0)
			{
				decisionFrame = new PlayerObservationDecision();
				decisionFrame.observation = samples;
				decisionFrame.decision = new float[no_decisions];
				float[] newRandomDecision = new float[no_decisions];
				float[] newAIDecision = new float[no_decisions];

				newRandomDecision = RandomDecision(decisionFrame.observation);
				if (simController.randomness < 1)
					newAIDecision = AIDecision(decisionFrame.observation);

				for (int i = 0; i < no_decisions; i++)
					decisionFrame.decision[i] = (simController.randomness * newRandomDecision[i] + (1 - simController.randomness) * newAIDecision[i]);
				lastDecision = new float[] { newAIDecision[0], newAIDecision[1], newAIDecision[2] };
				ImplementDecision(decisionFrame.decision);
				// after some delay, check if the decision was successful
				// if so, add the observation/decision to the training set
				StartCoroutine(CheckDecisionOutcome(decisionFrame));
				lastMovement_t = Time.time;
			}

			float dt = Time.deltaTime;
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

			if (LeftPressed && !FirePressed && !RigtPressed)
				state |= SpriteState.MoveL;
			else
				state &= ~SpriteState.MoveL;
			if (RigtPressed && !FirePressed && !LeftPressed)
				state |= SpriteState.MoveR;
			else
				state &= ~SpriteState.MoveR;
			if (BombPressed)
				state |= SpriteState.Bomb;
			else
				state &= ~SpriteState.Bomb;

			if (LeftPressed && !FirePressed && !RigtPressed)
				ProcessMove(-1 * dt);
			if (RigtPressed && !FirePressed && !LeftPressed)
				ProcessMove(1 * dt);
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

		private IEnumerator CheckDecisionOutcome(PlayerObservationDecision observationDecision)
		{
			int startingRun = simController.currentRun;
			float origHP = CurHP;
			int bombs = NumBombs;
			yield return new WaitForSeconds(decisionOutcomeCheckDelay);
			if (!IsDead() && startingRun == simController.currentRun) // if still alive
			{
				damageScore = Mathf.Abs(transform.localPosition.x) > 2.5 ? -1 : 1;
				float dodgeScore = observationDecision.observation[8] * 2f * (Mathf.Abs(observationDecision.decision[1]) > 0.3f ? 1 : 0);
				observationDecision.weight += bonusScore + (CurHP < origHP ? -10 * (bombs > 0 ? 2 : 1) : dodgeScore) + (FirePressed ? damageScore : 0) + (BombPressed ? bombScore : 0);
				damageScore = 0;
				bombScore = 0;
				bonusScore = 0;
				//if (Mathf.Approximately(observationDecision.weight, 0)) yield break;
				if (observationDecision.observation[o] == 0 && observationDecision.observation[o + 1] == 0 && Mathf.Approximately(observationDecision.weight, 0)) yield break;
				//if (doSubmit)
				{
					simController.AddTrainingSet(observationDecision);
					lastActionScore = observationDecision.weight;
				}
			}
		}

		private void ImplementDecision(float[] decision)
		{
			FirePressed = decision[0] >  0.5f;
			LeftPressed = decision[1] < -0.333f;
			RigtPressed = decision[1] >  0.333f;
			BombPressed = decision[2] >  0.5f;
		}

		float[] RandomDecision(float[] observation)
		{
			/* decision:
	         decision[0]: fire
	         decision[1]: move left/move right
	         decision[2]: bomb
	        */
			float[] decision = new float[no_decisions];

			Vector2 p = new Vector2(observation[6], observation[7]) * 20;
			decision[0] = Mathf.Abs(p.x) < 1 ? 1 : 0;
			decision[2] = observation[8] * 20 > 4 ? 1 : 0;
			float Spd = observation[2] / 60;
			float dir = 0;
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
			if (Mathf.Abs(dir) > 0.5f)
			{
				decision[0] = 0;
				decision[1] = Mathf.Clamp(dir, -1, 1);
			}
			else
			{
				decision[0] = Random.value > 0.5f ? 1 : 0;
				if (p.x < -1)
					decision[1] = 1;
				if(p.x > 1)
					decision[1] = -1;
			}
			Vector2 p2 = new Vector2(observation[o + 16], observation[o + 17]) * 20;
			decision[2] *= (Vector2.Distance(p, p2) < 1) ? 1 : 0;

			return decision;
		}

		float[] AIDecision(float[] observation)
		{
			evalSolver.Evaluate(simController.net, Noedify_Utils.AddTwoSingularDims(observation), Noedify_Solver.SolverMethod.MainThread);
			return evalSolver.prediction;
		}

		List<Collider2D> BombedBullets = new List<Collider2D>();
		List<Collider2D> TakenBullets = new List<Collider2D>();

		private void ExplodeBomb()
		{
			if (NumBombs <= 0)
			{
				this.bombScore -= 1f;
				return;
			}
			NumBombs--;
			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.localPosition, 2.5f, LayerMask.GetMask(new string[] { "BossEnemy" })); //, 0, LayerMask.GetMask(new string[] { "BossEnemy" }));
			BombedBullets.AddRange(colls);
			bombScore += colls.Length > 0 ? colls.Length * 0.1f : -50;
		}
		
		private float bombScore = 0;
		int o = 32;

		float[] AcquireObservations()
		{
			float[] sensorInputs = new float[SimTrainer.no_observations];
			sensorInputs[0] = (FirePressed ? 1 : 0);
			sensorInputs[1] = (FirePressed ? 0 : (LeftPressed ? -1 : (RigtPressed ? 1 : 0)));
			sensorInputs[2] = BombPressed ? 1 : 0;
			sensorInputs[2] = speed;
			sensorInputs[4] = NumBombs / 10f;
			sensorInputs[5] = (float)CurHP / MaxHP;
			sensorInputs[6] = transform.localPosition.x * 0.05f;
			sensorInputs[7] = transform.localPosition.y * 0.05f;

			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position + Vector3.up, 5, LayerMask.GetMask(new string[] { "BossEnemy" }));
			//Debug.Log($"Mask: {LayerMask.GetMask(new string[] { "BossEnemy" })}, {colls.Length}");
			//DrawDebugCircle(transform.position, 4f);
			//.OverlapBoxAll(new Vector2(-6.5f, -3.5f), new Vector2(6.5f, 4), 0, LayerMask.GetMask(new string[]{"BossEnemy"}));
			Array.Sort(colls, (a, b) =>
			{
				float da = Vector2.Distance(a.transform.localPosition, transform.localPosition);
				float db = Vector2.Distance(b.transform.localPosition, transform.localPosition);
				return da.CompareTo(db);
			});
			//[9..32] currently empty
			int j = 0;
			int s = 0;
			for (int i = 0; j < 24 && j < colls.Length; i += 4)
			{
				//ignore bullets we've bombed
				if (BombedBullets.Contains(colls[j]))
				{
					j++;
					continue;
				}

				//ignore bullets below us
				if (colls[j].transform.localPosition.y < transform.localPosition.y - 0.5f)
				{
					j++;
					continue;
				}

				Bullet b = colls[j].GetComponent<Bullet>();

				sensorInputs[i + o + 0] = b.transform.localPosition.x * 0.05f;
				sensorInputs[i + o + 1] = b.transform.localPosition.y * 0.05f;
				sensorInputs[i + o + 2] = b.previousPosition1.x * 0.05f;
				sensorInputs[i + o + 3] = b.previousPosition1.y * 0.05f;
				j++;
				s++;
			}
			sensorInputs[8] = s * 0.05f;

			return sensorInputs;
		}

		private void DrawDebugCircle(Vector3 center, float rad)
		{
			int segments = 32;
			float inc = Mathf.PI * 2 / segments;
			for (float f = 0; f < Mathf.PI * 2; f += inc)
			{
				float x1 = Mathf.Sin(f);
				float x2 = Mathf.Sin(f + inc);
				float y1 = Mathf.Cos(f);
				float y2 = Mathf.Cos(f + inc);
				Debug.DrawLine(new Vector3(x1, y1, 0) * rad + center, new Vector3(x2, y2, 0) * rad + center);
			}
		}

		private void ProcessMove(float v)
		{

			transform.Translate(new Vector3(v, 0, 0) * speed);
			if (Mathf.Abs(transform.localPosition.x) > 5.5f)
			{
				transform.localPosition = new Vector3(5.5f * Mathf.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
			}
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

		public float ApplyDamage(float damage, Collider2D col)
		{
			TakenBullets.Add(col);
			if (BombedBullets.Contains(col)) return 0;
			CurHP -= Mathf.CeilToInt(damage);
			return damage;
		}

		public bool IsDead()
		{
			return CurHP <= 0;
		}

		public void Reset()
		{
			CurHP = MaxHP;
			float x = Random.value * 10 - 5;
			transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
			BombPressed = FirePressed = LeftPressed = RigtPressed = false;
			state = pState = SpriteState.None;
			bonusScore = cooldown = spriteTimer = 0;
			NumBombs = MaxBombs;
			BombedBullets.Clear();
			sRenderer.color = Random.ColorHSV(0, 1, 1, 1, 1, 1, 1, 1);
			speed = 1.75f + (Random.value / 2);
		}

		public float bonusScore = 0;

		public void AddScore(float amt, Collider2D col)
		{
			if (TakenBullets.Contains(col)) return;
			bonusScore += amt;
			//damageScore += amt;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			return 0;
		}
	}
}
