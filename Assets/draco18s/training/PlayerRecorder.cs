using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.draco18s;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using static SimTrainer;
using Random = UnityEngine.Random;

namespace Assets.draco18s.training
{
	public class PlayerRecorder : MonoBehaviour, IDamageDealer
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

		public int score = 0;
		public int scoreOffset = 0;
		public float damageScore = 0;
		public float decisionPeriod = .75f;
		public float decisionOutcomeCheckDelay = 1f;

		SimTrainer simController;
		SimTrainer.PlayerObservationDecision prevDecisionFrame;
		SimTrainer.PlayerObservationDecision decisionFrame;
		

		void Start()
		{
			bulletClone = Instantiate(bulletPrefab);
			bulletClone.SetActive(false);
			sRenderer = GetComponentInChildren<SpriteRenderer>();
			Bullet b = bulletClone.GetComponent<Bullet>();
			b.Init();
			b.pattern.Lifetime = 2;
			b.pattern.timeline.data[PatternDataKey.Size].keys = new[] { new Keyframe(0, 0.6f) };
			b.pattern.timeline.data[PatternDataKey.Speed].keys = new[] { new Keyframe(0, 12f) };
			b.pattern.timeline.data[PatternDataKey.Damage].keys = new[] { new Keyframe(0, 2f) };

			if (simController == null)
				simController = SimTrainer.instance;
			
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

			if (simController == null)
				Start();

			if (IsDead())
			{
				sRenderer.color = Color.gray;
				return;
			}

			BombedBullets.RemoveAll(b => b == null);

			float[] samples = AcquireObservations();
			
			float dt = Time.deltaTime;
			
			decisionFrame = new SimTrainer.PlayerObservationDecision();
			decisionFrame.decision = PlayerDecision(decisionFrame.observation);
			ImplementDecision(decisionFrame.decision);

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
			//Debug.Log($"{pState} -> {state}");
			if (state != pState && CurHP > 0)
			{
				decisionFrame.observation = samples;
				StartCoroutine(CheckDecisionOutcome(decisionFrame));
				prevDecisionFrame = decisionFrame;
			}


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
				observationDecision.weight += 0 - (CurHP < origHP ? 10 * (bombs > 0 ? 2 : 1) : 0) + (FirePressed ? damageScore : 0) + (BombPressed ? bombScore : 0);
				damageScore = 0;
				bombScore = 0;
				//if (Mathf.Approximately(observationDecision.weight, 0)) yield break;
				if (observationDecision.observation[o] == 0 && observationDecision.observation[o + 1] == 0 && Mathf.Approximately(observationDecision.weight, 0)) yield break;
				//if (doSubmit)
				{
					//simController.AddTrainingSet(observationDecision);
				}
			}
		}

		private void ImplementDecision(float[] decision)
		{
			FirePressed = decision[0] > 0.5f;
			LeftPressed = decision[1] < -0.333f;
			RigtPressed = decision[1] >  0.333f;
			BombPressed = decision[2] > 0.5f;
		}

		float[] PlayerDecision(float[] observation)
		{
			/* decision:
	         decision[0]: fire
	         decision[1]: move left/move right
	         decision[2]: bomb
	        */
			float[] decision = new float[SimTrainer.no_decisions];
			decision[0] = Input.GetMouseButton(0) ? 1 : 0;
			decision[1] = Input.GetMouseButton(0) ? 0 : Input.GetAxis("Horizontal");
			decision[2] = Input.GetMouseButton(1) ? 1 : 0;
			//Debug.Log(string.Join(',', decision));
			return decision;
		}

		List<Collider2D> BombedBullets = new List<Collider2D>();

		private void ExplodeBomb()
		{
			if (NumBombs <= 0)
			{
				this.bombScore -= 5f;
				return;
			}
			NumBombs--;
			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.localPosition, 2.5f, LayerMask.GetMask(new string[] { "BossEnemy" })); //, 0, LayerMask.GetMask(new string[] { "BossEnemy" }));
			BombedBullets.AddRange(colls);
			bombScore += colls.Length > 0 ? colls.Length * 0.1f : -50;
			foreach (Collider2D c in colls)
			{
				c.GetComponent<SpriteRenderer>().enabled = false;
			}
		}

		private float bombScore = 0;
		int o = 32;

		float[] AcquireObservations()
		{
			float[] sensorInputs = new float[SimTrainer.no_observations];
			sensorInputs[0] = (FirePressed ? 1 : 0);
			sensorInputs[1] = (FirePressed ? 1 : (LeftPressed ? 0 : (RigtPressed ? 2 : 1))) / 3f;
			sensorInputs[2] = BombPressed ? 1 : 0;
			sensorInputs[4] = NumBombs / 10f;
			sensorInputs[5] = (float)CurHP / MaxHP;
			sensorInputs[6] = transform.localPosition.x * 0.05f;
			sensorInputs[7] = transform.localPosition.y * 0.05f;

			foreach(Bullet q in GameObject.FindObjectsOfType<Bullet>())
			{
				if(q.gameObject.layer == this.gameObject.layer) continue;
				Color c = q.GetComponent<SpriteRenderer>().color;
				c.a = 0;
				q.GetComponent<SpriteRenderer>().color = c;
			}

			Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position + Vector3.up, 5, LayerMask.GetMask(new string[] { "BossEnemy" }));
			//Debug.Log($"Mask: {LayerMask.GetMask(new string[] { "BossEnemy" })}, {colls.Length}");
			DrawDebugCircle(transform.position, 5f);
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

				Color c = b.GetComponent<SpriteRenderer>().color;
				c.a = 1;
				b.GetComponent<SpriteRenderer>().color = c;
				j++;
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
			if (Mathf.Abs(transform.localPosition.x) > 6)
			{
				transform.localPosition = new Vector3(6 * Mathf.Sign(transform.localPosition.x), transform.localPosition.y, transform.localPosition.z);
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
					sRenderer.sprite = Idle[spriteIndex % Idle.Length];
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
			g.GetComponent<SpriteRenderer>().enabled = false;
			Bullet b = g.GetComponent<Bullet>();
			b.pattern.timeline.CopyFrom(bulletClone.GetComponent<Bullet>().pattern.timeline);
			b.playerOwner = this;
		}

		public float ApplyDamage(float damage, Collider2D col)
		{
			if (BombedBullets.Contains(col)) return 0;
			if (!sRenderer.enabled) return 0;
			CurHP -= Mathf.CeilToInt(damage);
			col.GetComponent<SpriteRenderer>().enabled = false;
			return damage;
		}

		public bool IsDead()
		{
			return CurHP <= 0;
		}

		public void Reset()
		{
			if (sRenderer == null) return;
			CurHP = MaxHP;
			float x = Random.value * 10 - 5;
			transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
			BombPressed = FirePressed = LeftPressed = RigtPressed = false;
			state = pState = SpriteState.None;
			cooldown = spriteTimer = 0;
			NumBombs = MaxBombs;
			BombedBullets.Clear();
			sRenderer.color = new Color(0, 1, 0.85f, 1);
		}

		public void AddScore(float amt, Collider2D col)
		{
			damageScore += amt;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			return 0;
		}
	}
}
