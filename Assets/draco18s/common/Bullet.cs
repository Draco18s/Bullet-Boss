using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.training
{
	public class Bullet : MonoBehaviour, IHasPattern
	{
		public PatternData pattern;
		protected Dictionary<PatternDataKey, float> currentValues = PopulateDict();

		public PatternData Pattern => pattern;
		
		public float CurrentTime => timeAlive % pattern.Lifetime;
		[SerializeField]
		
		public float timeAlive;

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

		public void Init()
		{
			foreach (PatternDataKey d in GetAllowedValues())
			{
				if(pattern.timeline.data.ContainsKey(d)) continue;
				pattern.timeline.data.Add(d, new AnimationCurve()
				{
					preWrapMode = WrapMode.ClampForever,
					postWrapMode = WrapMode.ClampForever
				});
			}
			if(!pattern.timeline.data.ContainsKey(PatternDataKey.Damage))
				pattern.timeline.data.Add(PatternDataKey.Damage, AnimationCurve.Constant(0, 10, 1));
			pattern.timeline.data[PatternDataKey.Speed].AddKey(0, 1);
			pattern.timeline.data[PatternDataKey.Size].AddKey(0, 1);
			pattern.timeline.data[PatternDataKey.Speed].AddKey(10, 1);
			pattern.timeline.data[PatternDataKey.Size].AddKey(10, 1);

			pattern.dataValues[PatternDataKey.Size] = 1;
			pattern.dataValues[PatternDataKey.Damage] = 1;
			pattern.dataValues[PatternDataKey.Speed] = 3;
			pattern.dataValues[PatternDataKey.Rotation] = 72;
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
			if (timeAlive >= pattern.Lifetime)
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
			transform.localScale = Vector3.one * currentValues[PatternDataKey.Size];
			transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt, Space.Self);
		}

		[UsedImplicitly]
		private void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.layer != this.gameObject.layer)
			{
				float d = col.gameObject.GetComponent<IDamageTaker>().ApplyDamage(currentValues[PatternDataKey.Damage], GetComponent<Collider2D>());
				if (playerOwner != null)
				{
					playerOwner.AddScore(d, col);
				}
				hitSomething = true;
			}
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