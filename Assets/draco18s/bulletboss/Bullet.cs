using System;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class Bullet : MonoBehaviour, IHasPattern
	{
		public PatternData pattern;
		protected Dictionary<PatternDataKey, float> currentValues = PopulateDict();
		public Dictionary<PatternDataKey, float> modifiers = PopulateDict();

		public PatternData Pattern => pattern;
		
		public float CurrentTime => timeAlive % (pattern.Lifetime * lifetime);
		[SerializeField]
		
		public float timeAlive;

		protected float damage = 1;
		protected float lifetime;

		public IDamageDealer playerOwner;
		public Vector3 previousPosition1;
		public Vector3 previousPosition2;
		public Vector3 previousPosition3;

		public SpriteRenderer spriteRenderer { get; protected set; }

		private static Dictionary<PatternDataKey, float> PopulateDict()
		{
			Dictionary<PatternDataKey, float> d = new Dictionary<PatternDataKey, float>();
			for (PatternDataKey i = 0; i < PatternDataKey.Length; i++)
			{
				d.Add(i, 1);
			}
			return d;
		}

		[UsedImplicitly]
		void Start()
		{
			timeAlive = 0;
			if (pattern.Lifetime == 0)
			{
				pattern.Lifetime = 10;
			}
			Init();
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.color = new Color(1, 1, 1, 0.5f);
		}

		public void SetDamage(float dmg)
		{
			if (dmg < 1) dmg = 1;
			damage = dmg;
		}

		public void SetLifetime(float time)
		{
			lifetime = time;
		}

		public void SetStat(PatternDataKey key, float val)
		{
			pattern.dataValues[key] = val;
		}

		public void Init()
		{
			foreach (PatternDataKey d in GetAllowedValues())
			{
				if(pattern.timeline.data.ContainsKey(d)) continue;
				AnimationCurve c = new AnimationCurve()
				{
					preWrapMode = WrapMode.ClampForever,
					postWrapMode = WrapMode.ClampForever
				};
				if (d == PatternDataKey.Speed)
				{
					c.AddKey(0, 3);
					c.AddKey(10, 3);
				}

				if (d == PatternDataKey.Size)
				{
					c.AddKey(0, 1);
					c.AddKey(10, 1);
				}
				pattern.timeline.data.Add(d, c);
			}
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			previousPosition3 = previousPosition2;
			previousPosition2 = previousPosition1;
			previousPosition1 = transform.localPosition;
			float dt = Time.fixedDeltaTime;
			timeAlive += dt;
			if (timeAlive < 0) return;
			if (timeAlive < 0.15f)
			{
				GetComponent<SpriteRenderer>().enabled = timeAlive > 0;
				if(transform.localPosition.y < -5.5f) Destroy(gameObject);
			}
			if (timeAlive >= pattern.Lifetime || Mathf.Abs(transform.localPosition.x) > 7.5f || transform.localPosition.y < -7.5f || transform.localPosition.y > 4.75f)
			{
				Destroy(gameObject);
				return;
			}

			foreach (PatternDataKey d in GetAllowedValues())
			{
				currentValues[d] = pattern.dataValues[d] * pattern.timeline.Evaluate(d, timeAlive / pattern.Lifetime * 10);
			}
			transform.Translate(new Vector3(currentValues[PatternDataKey.Speed] * dt, 0, dt), Space.Self);
			transform.localScale = Vector3.one * currentValues[PatternDataKey.Size] / 3;

			if (pattern.effects.HomingShots)
			{
				float bestAngle = 180;
				foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 20, 1 << gameObject.layer-2))
				{
					Transform playerTransform = c.transform;

					Vector3 relativePos = playerTransform.position - transform.position;
					Vector3 forward = transform.right;
					var angle = Vector3.SignedAngle(relativePos.ReplaceZ(0), forward, transform.forward);

					if (Math.Abs(angle) < Math.Abs(bestAngle))
					{
						bestAngle = angle;
					}
				}
				transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt * Mathf.Sign(-bestAngle) / 8f, Space.Self);
			}
			else
			{
				transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt * (pattern.effects.MirrorSpreadShots ? -1 : 1), Space.Self);
			}
		}

		[UsedImplicitly]
		private void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.layer == gameObject.layer || col.gameObject.layer+2 == gameObject.layer || col.gameObject.layer == 0) return;
			if (transform.localScale.x > 0.02f)
			{
				IDamageTaker taker = col.gameObject.GetComponent<IDamageTaker>();
				if (taker == null) return;
				float d = taker.ApplyDamage(damage, GetComponent<Collider2D>());
				playerOwner?.DamagedEnemy(d, col);
				if(d > 0) Destroy(gameObject);
			}
		}

		public Timeline GetTimeline()
		{
			return pattern.timeline;
		}

		public PatternData GetPatternData()
		{
			return pattern;
		}

		public IHasPattern GetSubsystem()
		{
			return GetComponentInChildren<GunBarrel>();
		}

		public void SetPattern(PatternData patternIn)
		{
			pattern.CopyFrom(patternIn);
		}

		public void SetTime(int v)
		{

		}

		public List<PatternDataKey> GetAllowedValues()
		{
			return new List<PatternDataKey>() {
				PatternDataKey.Rotation,
				PatternDataKey.Size,
				PatternDataKey.Speed,
			};
		}

		public Vector3 GetAllowedRange(PatternDataKey dataKey)
		{
			switch (dataKey)
			{
				case PatternDataKey.Speed:
					return new Vector3(0, 10, 4);
				case PatternDataKey.Size:
					return new Vector3(0, 2.5f, 4);
				case PatternDataKey.Rotation:
					return new Vector3(-2, 2, 10);
			}
			return Vector3.zero;
		}
	}
}