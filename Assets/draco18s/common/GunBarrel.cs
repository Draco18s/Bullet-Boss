using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.draco18s.training
{
	public class GunBarrel : MonoBehaviour, IHasPattern
	{
		public PatternData pattern;
		public Transform[] muzzles;
		public Sprite bulletTexture;
		public GameObject bulletPrefab;
		protected GameObject bulletClone;
		protected Dictionary<PatternDataKey, float> currentValues = PopulateDict();

		public PatternData Pattern => pattern;
		
		public float CurrentTime => timeAlive % pattern.Lifetime;
		
		public GunType ReloadType
		{
			get => pattern.ReloadType;
			set
			{
				if (value == GunType.SingleShot || value == GunType.Shotgun)
				{
					minFireDelay = actualCooldown * (value == GunType.SingleShot ? 0.3f : 1);
				}
				else if (value == GunType.Spread)
				{
					minFireDelay = actualCooldown;
				}
				else if(value == GunType.Overheat)
				{
					minFireDelay = 0.25f;
				}
				else
				{
					minFireDelay = 0.6f;
				}
				pattern.ReloadType = value;
			}
		}

		[SerializeField]
		public float MaxCapacity { get; set; } = 1;
		[SerializeField]
		public float CooldownTime { get; set; } = 0;
		public float timeAlive;
		[SerializeField]
		protected float minFireDelay = 0.25f;
		protected float fireDelay = 0f;

		protected int actualClip = 10;
		protected float actualCooldown => 2.6f - (CooldownTime * 2.5f);

		protected int heatOrClip;
		protected float reloadDelay;
		protected float bonusCooldown;

		private static Dictionary<PatternDataKey, float> PopulateDict()
		{
			Dictionary<PatternDataKey, float> d = new Dictionary<PatternDataKey, float>();
			for (PatternDataKey i = 0; i < PatternDataKey.Length; i++)
			{
				d.Add(i, 0);
			}
			return d;
		}

		void OnValidate()
		{
			pattern ??= new PatternData();
			pattern.childPattern ??= new PatternData();
		}

		[UsedImplicitly]
		void Start()
		{
			timeAlive = 0;
			if (pattern.Lifetime == 0)
			{
				pattern.Lifetime = 10;
			}
			pattern.StartAngle = transform.localEulerAngles.z;
			Init();
		}
		public void Init()
		{
			timeAlive = 0;
			if (pattern.childPattern != null)
			{
				bulletClone = Instantiate(bulletPrefab);
				bulletClone.SetActive(false);
				bulletClone.GetComponent<Bullet>().SetPattern(pattern.childPattern);
			}

			if (pattern.timeline.data.Count > 0) return;
			foreach (PatternDataKey d in GetAllowedValues())
			{
				pattern.timeline.data.Add(d, new AnimationCurve()
				{
					preWrapMode = WrapMode.Loop,
					postWrapMode = WrapMode.Loop
				});
			}
			pattern.dataValues[PatternDataKey.FireShot] = 1;
			pattern.dataValues[PatternDataKey.Rotation] = 36f;
			ReloadType = ReloadType;
		}

		[UsedImplicitly]
		void Update()
		{
			float dt = Time.deltaTime;
			timeAlive += Time.deltaTime;
			foreach (PatternDataKey d in GetAllowedValues())
			{
				currentValues[d] = pattern.dataValues[d] * pattern.timeline.Evaluate(d, timeAlive * (pattern.Lifetime / 10));
			}
			transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt, Space.Self);

			if (ReloadType == GunType.None) return;
			if (ReloadType == GunType.SingleShot || ReloadType == GunType.Shotgun)
			{
				minFireDelay = actualCooldown * (ReloadType == GunType.SingleShot ? 0.3f : 1);
				actualClip = 8;
			}
			else if (ReloadType == GunType.Spread)
			{
				minFireDelay = actualCooldown;
				actualClip = 5;
			}
			else if (ReloadType == GunType.Overheat)
			{
				minFireDelay = 0.25f;
				actualClip = 25;
			}
			else
			{
				minFireDelay = 0.6f;
				actualClip = 12;
			}

			fireDelay -= dt;
			
			if (ReloadType == GunType.ClipSize)
			{
				if (heatOrClip <= 0)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown;
					reloadDelay -= dt;
					if (reloadDelay <= 0)
						heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
					return;
				}
			}
			else if (ReloadType == GunType.Overheat)
			{
				bonusCooldown -= dt;
				if (bonusCooldown <= -2f)
				{
					bonusCooldown += 2f;
					if (heatOrClip > 0)
						heatOrClip = Mathf.Min(heatOrClip + 1, Mathf.RoundToInt(MaxCapacity * actualClip));
					else
						reloadDelay -= 0.25f;
				}
				if (heatOrClip <= 0)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown * 2;
					reloadDelay -= dt;
					if (reloadDelay <= 0)
						heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
					return;
				}
			}
			else
			{
				if (heatOrClip <= 0)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown * 2;
					reloadDelay -= dt;
					if (reloadDelay <= 0)
						heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
					return;
				}

			}


			if (currentValues[PatternDataKey.FireShot] >= 0.99f)
			{
				if (heatOrClip > 0)
				{
					reloadDelay = 0;
				}
				Fire();
			}
			else
			{
				if (ReloadType == GunType.ClipSize)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown;
					reloadDelay -= dt;
					if (reloadDelay <= 0)
						heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
				}
				if (ReloadType == GunType.ClipSize)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown * 2;
					reloadDelay -= dt;
					if (reloadDelay <= 0)
						heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
				}
			}
		}

		public void Fire()
		{
			if (ReloadType == GunType.None) return;
			if (fireDelay > 0 || heatOrClip <= 0) return;
			foreach (Transform tr in muzzles)
				Fire(tr);
			fireDelay = minFireDelay;
		}
		private void Fire(Transform muz)
		{
			GameObject go = Instantiate(bulletClone, muz.position, muz.transform.rotation, RandomBossSelector.instance.GameLayer);
			go.SetActive(true);
			go.layer = gameObject.layer;
			go.GetComponent<SpriteRenderer>().sprite = bulletTexture;
			Bullet b = go.GetComponent<Bullet>();
			b.SetPattern(pattern.childPattern);

			if (ReloadType == GunType.SingleShot) return;
			if (ReloadType == GunType.Shotgun)
			{
				float spreadAngle = 30;
				float randAngle = Random.value * spreadAngle - (spreadAngle/2);
				go.transform.Rotate(Vector3.forward, randAngle);
				b.timeAlive = (Random.value - 0.5f) * 0.15f;
				for (int i = 1; i < (float)heatOrClip / muzzles.Length; i++)
				{
					randAngle = Random.value * spreadAngle - (spreadAngle / 2);
					go = Instantiate(bulletClone, muz.position, muz.transform.rotation, RandomBossSelector.instance.GameLayer);
					go.SetActive(true);
					go.transform.Rotate(Vector3.forward, randAngle);
					go.GetComponent<SpriteRenderer>().sprite = bulletTexture;
					b = go.GetComponent<Bullet>();
					b.SetPattern(pattern.childPattern);
					b.timeAlive = (Random.value - 0.5f) * 0.15f;
				}
				return;
			}

			if (ReloadType == GunType.Spread)
			{
				float spreadAngle = 30;
				float angleDelta = spreadAngle / heatOrClip;
				float angle = -spreadAngle / 2;
				//float randAngle = Random.value * spreadAngle - (spreadAngle/2);
				go.transform.Rotate(Vector3.forward, angle);
				b.timeAlive = (Random.value - 0.5f) * 0.15f;
				for (int i = 1; i < (float)heatOrClip / muzzles.Length; i++)
				{
					//randAngle = Random.value * spreadAngle - (spreadAngle / 2);
					angle += angleDelta;
					go = Instantiate(bulletClone, muz.position, muz.transform.rotation, RandomBossSelector.instance.GameLayer);
					go.SetActive(true);
					go.transform.Rotate(Vector3.forward, angle);
					go.GetComponent<SpriteRenderer>().sprite = bulletTexture;
					b = go.GetComponent<Bullet>();
					b.SetPattern(pattern.childPattern);
					b.timeAlive = (Random.value - 0.5f) * 0.15f;
				}
				return;
			}
			heatOrClip--;
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
			transform.localRotation = Quaternion.identity;
			transform.Rotate(Vector3.forward, pattern.StartAngle);
			heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
			bonusCooldown = reloadDelay = fireDelay = 0;
			timeAlive = pattern.TimeOffset;
		}

		public List<PatternDataKey> GetAllowedValues()
		{
			return new List<PatternDataKey>() { PatternDataKey.FireShot, PatternDataKey.Rotation };
		}

		public Vector3 GetAllowedRange(PatternDataKey dataKey)
		{
			switch (dataKey)
			{
				case PatternDataKey.FireShot:
					return new Vector3(0, 1, 1);
				case PatternDataKey.Rotation:
					return new Vector3(-2, 2, 4);
			}
			return Vector3.zero;
		}

		private void OnMouseUpAsButton()
		{
			PatternEditor.instance.ChangeTarget(gameObject);
		}
	}
}