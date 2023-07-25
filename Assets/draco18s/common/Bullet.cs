using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.training
{
	public class Bullet : MonoBehaviour, IHasPattern
	{
		public PatternData pattern;
		protected Dictionary<PatternDataKey, float> currentValues = PopulateDict();
		public Dictionary<PatternDataKey, float> modifiers = PopulateDict();

		public PatternData Pattern => pattern;
		
		public float CurrentTime => timeAlive % (pattern.Lifetime* lifetime);
		[SerializeField]
		
		public float timeAlive;

		protected float damage;
		protected float lifetime;

		public IDamageDealer playerOwner;
		public Vector2 previousPosition1;
		public Vector2 previousPosition2;
		public Vector2 previousPosition3;
		public bool hitSomething = false;

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
		}

		public void SetDamage(float dmg)
		{
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
			previousPosition1 = previousPosition2;
			previousPosition2 = previousPosition3;
			previousPosition3 = transform.localPosition;
			float dt = Time.deltaTime;
			timeAlive += Time.deltaTime;
			if (timeAlive < 0) return;
			if (timeAlive < 0.15f)
			{
				GetComponent<SpriteRenderer>().enabled = timeAlive > 0;
			}
			if (timeAlive >= pattern.Lifetime || Mathf.Abs(transform.localPosition.x) > 10f || Mathf.Abs(transform.localPosition.y) > 10)
			{
				timeAlive = -1000;
				if (!hitSomething && playerOwner != null)
				{
					//playerOwner.AddScore(-10f, GetComponent<Collider2D>());
				}
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
				foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 10, LayerMask.GetMask(new[] { "AIPlayer" })))
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
				transform.Rotate(Vector3.forward, Math.Max(Math.Abs(currentValues[PatternDataKey.Rotation]), 0.25f) * pattern.dataValues[PatternDataKey.Rotation] * dt * Mathf.Sign(-bestAngle) * 0.2f, Space.Self);
			}
			else
			{
				transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt * (pattern.effects.MirrorSpreadShots ? -1 : 1), Space.Self);
			}
		}

		[UsedImplicitly]
		private void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.layer == this.gameObject.layer) return;
			float d = col.gameObject.GetComponent<IDamageTaker>().ApplyDamage(damage, GetComponent<Collider2D>());
			playerOwner?.AddScore(d, col);
			hitSomething = true;
		}

		public Timeline GetTimeline()
		{
			return pattern.timeline;
		}

		public PatternData GetSubsystem()
		{
			return pattern.childPattern;
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
				case PatternDataKey.Size:
					return new Vector3(0, 10, 4);
				case PatternDataKey.Rotation:
					return new Vector3(-2, 2, 10);
			}
			return Vector3.zero;
		}
	}
}