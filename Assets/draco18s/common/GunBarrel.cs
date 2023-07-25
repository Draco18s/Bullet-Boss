using System;
using System.Collections.Generic;
using Assets.draco18s.ui;
using Assets.draco18s.util;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.draco18s.training
{
	public class GunBarrel : MonoBehaviour, IHasPattern
	{
		public PatternData pattern;
		public Transform[] muzzles;
		public SerializableDictionary<StatAttribute, float> gunBaseModifiers;
		protected GameObject bulletClone;
		protected Dictionary<PatternDataKey, float> currentValues = PopulateDict();
		protected HardPoint mountingPoint;

		public PatternData Pattern => pattern;
		
		public float CurrentTime => timeAlive % pattern.Lifetime;
		
		public GunType ReloadType
		{
			get => pattern.ReloadType;
			set
			{
				if (value == GunType.SingleShot || value == GunType.Shotgun)
				{
					minFireDelay = actualCooldown * (value == GunType.SingleShot ? 0.3f : 1) * GetStat(StatAttribute.FiringRate, (a, b) => a * b);
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
				heatOrClip = Mathf.RoundToInt(MaxCapacity * actualClip);
			}
		}

		[SerializeField]
		public float MaxCapacity => GetStat(StatAttribute.Capacity, (a, b) => a * b);
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
			if (bulletClone == null)
			{
				bulletClone = Instantiate(GameTransform.instance.basicBulletPrefab);
				bulletClone.SetActive(false);
			}
		}
		public void Init()
		{
			timeAlive = 0;

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

		public void SetMounting(HardPoint point)
		{
			mountingPoint = point;
		}

		public void SetShell(InventoryItem item)
		{
			Destroy(bulletClone);
			bulletClone = null;
			if (item == null) return;

			bulletClone = Instantiate(item.upgradeTypeData.relevantPrefab);
			bulletClone.SetActive(false);
			bulletClone.GetComponent<Bullet>().SetPattern(item.upgradeTypeData.relevantPattern);
			bulletClone.GetComponent<SpriteRenderer>().sprite = item.upgradeTypeData.image;
			pattern.childPattern =  new PatternData();
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
			transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt * GetStat(StatAttribute.GunSpeed, (a, b) => a * b), Space.Self);

			float ang = GetStat(StatAttribute.AngleRestriction, (a, b) => a + b) / 2;
			float f = transform.localEulerAngles.z - pattern.StartAngle;
			if (f > 180) f -= 360;
			transform.localEulerAngles = transform.localEulerAngles.ReplaceZ(Mathf.Clamp(f, -ang, ang) + pattern.StartAngle);
			
			if (ReloadType == GunType.None) return;
			if (ReloadType == GunType.SingleShot)
			{
				GetStat(StatAttribute.FiringRate, (a, b) => a * b);
				minFireDelay = actualCooldown * 0.6f;
				actualClip = 1;
			}
			else if (ReloadType == GunType.Shotgun)
			{
				minFireDelay = actualCooldown;
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
			heatOrClip = Math.Min(Mathf.RoundToInt(MaxCapacity * actualClip), heatOrClip);

			fireDelay -= dt * GetStat(StatAttribute.FiringRate, (a, b) => a*b);

			if (ReloadType == GunType.ClipSize)
			{
				if (heatOrClip <= 0)
				{
					if (reloadDelay <= 0)
						reloadDelay = actualCooldown;
					reloadDelay -= dt * GetStat(StatAttribute.Reload, (a, b) => a * b);
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
					reloadDelay -= dt * GetStat(StatAttribute.Reload, (a, b) => a * b);
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
					reloadDelay -= dt * GetStat(StatAttribute.Reload, (a, b) => a * b);
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

		private float GetStat(StatAttribute stat, Func<float,float,float> combine)
		{
			float r = 1;
			if(gunBaseModifiers.TryGetValue(stat, out var modifier))
				r = combine(r, modifier);
			if(mountingPoint != null)
				r = combine(r, mountingPoint.GetStat(stat, combine));
			return r;
		}

		public void Fire()
		{
			if (ReloadType == GunType.None) return;
			if (fireDelay > 0 || heatOrClip <= 0) return;
			if(muzzles.Length == 0) Debug.LogError("Gun barrel has no muzzles!");
			foreach (Transform tr in muzzles)
				Fire(tr);
			fireDelay = minFireDelay;
		}

		[UsedImplicitly]
		void OnDestroy()
		{
			Destroy(bulletClone);
		}

		private void Fire(Transform muz)
		{
			if (bulletClone == null) return;
			GameObject go = Instantiate(bulletClone, muz.position, muz.transform.rotation, GameTransform.instance.transform);
			go.SetActive(true);
			go.layer = gameObject.layer;
			Bullet b = go.GetComponent<Bullet>();
			SetBulletDetails(b);

			if (ReloadType == GunType.SingleShot) return;
			if (ReloadType == GunType.Shotgun)
			{
				float spreadAngle = 30;
				float randAngle = Random.value * spreadAngle - (spreadAngle/2);
				go.transform.Rotate(Vector3.forward, randAngle);
				SetBulletDetails(b);
				if (b.pattern.effects.MirrorSpreadShots == TriValue.True)
				{
					b.pattern.dataValues[PatternDataKey.Rotation] *= Mathf.Abs(randAngle / (spreadAngle / 2));
				}
				b.pattern.effects.MirrorSpreadShots = randAngle <= 0 ? b.pattern.effects.MirrorSpreadShots : TriValue.Default.Clone();

				b.timeAlive = (Random.value - 0.5f) * 0.15f;
				for (int i = 1; i < (float)heatOrClip / muzzles.Length; i++)
				{
					randAngle = Random.value * spreadAngle - (spreadAngle / 2);
					go = Instantiate(bulletClone, muz.position, muz.transform.rotation, GameTransform.instance.transform);
					go.SetActive(true);
					go.transform.Rotate(Vector3.forward, randAngle);
					b = go.GetComponent<Bullet>();
					b.timeAlive = (Random.value - 0.5f) * 0.15f;
					SetBulletDetails(b);
					if (b.pattern.effects.MirrorSpreadShots == TriValue.True)
					{
						b.pattern.dataValues[PatternDataKey.Rotation] *= Mathf.Abs(randAngle / (spreadAngle / 2));
					}
					b.pattern.effects.MirrorSpreadShots = randAngle <= 0 ? b.pattern.effects.MirrorSpreadShots : TriValue.Default.Clone();
				}
				return;
			}

			if (ReloadType == GunType.Spread)
			{
				float spreadAngle = 30;
				float angleDelta = spreadAngle / heatOrClip;
				float angle = -spreadAngle / 2;
				if (heatOrClip % 2 == 1)
					angle += angleDelta / 2;
				SetBulletDetails(b);
				go.transform.Rotate(Vector3.forward, angle);
				if (b.pattern.effects.MirrorSpreadShots == TriValue.True)
				{
					b.pattern.dataValues[PatternDataKey.Rotation] *= Mathf.Abs(angle / (spreadAngle / 2));
				}
				b.pattern.effects.MirrorSpreadShots = angle <= 0 ? b.pattern.effects.MirrorSpreadShots : TriValue.Default.Clone();
				for (int i = 1; i < (float)heatOrClip / muzzles.Length; i++)
				{
					angle += angleDelta;
					go = Instantiate(bulletClone, muz.position, muz.transform.rotation, GameTransform.instance.transform);
					go.SetActive(true);
					go.transform.Rotate(Vector3.forward, angle);
					b = go.GetComponent<Bullet>();
					SetBulletDetails(b);
					if (b.pattern.effects.MirrorSpreadShots == TriValue.True)
					{
						b.pattern.dataValues[PatternDataKey.Rotation] *= Mathf.Abs(angle / (spreadAngle / 2));
					}
					b.pattern.effects.MirrorSpreadShots = angle <= 0 ? b.pattern.effects.MirrorSpreadShots : TriValue.Default.Clone();
				}
				return;
			}
			heatOrClip--;
		}

		private void SetBulletDetails(Bullet bul)
		{
			if(mountingPoint == null && pattern.childPattern != null)
				bul.SetPattern(pattern.childPattern);
			bul.SetDamage(GetStat(StatAttribute.Damage, (a, b) => a + b));
			bul.SetLifetime(GetStat(StatAttribute.Lifetime, (a, b) => a * b));
			bul.SetStat(PatternDataKey.Speed, GetStat(StatAttribute.BulletSpeed, (a, b) => a * b));
			if (mountingPoint != null)
			{
				bul.pattern.effects = bul.pattern.effects.Merge(mountingPoint.GetPatternModifiers());
			}
			if (bul.pattern.image != null) bul.GetComponent<SpriteRenderer>().sprite = bul.pattern.image;
			if (bul.pattern.childPattern == null) return;
			GunBarrel[] guns = bul.GetComponentsInChildren<GunBarrel>();
			if (guns.Length == 0)
			{
				return;
			}
			foreach (GunBarrel bar in guns)
			{
				bar.SetPattern(bul.pattern.childPattern);
				bar.SetTime(0);
			}
		}

		public Timeline GetTimeline()
		{
			return pattern.timeline;
		}

		public PatternData GetSubsystem()
		{
			return bulletClone.GetComponent<Bullet>().pattern;
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