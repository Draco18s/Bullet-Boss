using System;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.bulletboss;
using Assets.draco18s.bulletboss.ui;
using Assets.draco18s.util;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.draco18s.bulletboss
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
				pattern.ReloadType = value;
				availableShots = Mathf.RoundToInt(MaxCapacity);
				fireDelay = 0;
			}
		}

		public float MaxCapacity => GetBaseCapacity() * GetStat(StatAttribute.Capacity, ()=>1, (a, b) => a * b);
		public float FiringRate => GetBaseFiringRate() * GetStat(StatAttribute.FiringRate, () => 1, (a, b) => a * b);
		public float ReloadTime => GetBaseReload() * GetStat(StatAttribute.Reload, () => 1, (a, b) => a * b);

		private float GetBaseReload()
		{
			switch (ReloadType)
			{
				case GunType.SingleShot:
					return 1;
				case GunType.Burst:
					return 5f;
				case GunType.Continuous:
					return 10f;
			}

			return 10;
		}

		private float GetBaseFiringRate()
		{
			switch (ReloadType)
			{
				case GunType.SingleShot:
					return 1;
				case GunType.Burst:
					return 0.6f;
				case GunType.Continuous:
					return 0.25f;
			}

			return 10;
		}

		private float GetBaseCapacity()
		{
			switch (ReloadType)
			{
				case GunType.SingleShot:
					return 1;
				case GunType.Burst:
					return 5;
				case GunType.Continuous:
					return 20f;
			}

			return 1;
		}


		[NonSerialized]
		public bool active = true;
		[NonSerialized]
		public float timeAlive;
		[NonSerialized]
		public float fireDelay;
		[NonSerialized]
		public float reloadTime;
		[NonSerialized]
		public int availableShots;

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
		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			timeAlive = 0;
			if (pattern.Lifetime == 0)
			{
				pattern.Lifetime = 10;
			}
			pattern.StartAngle = transform.localEulerAngles.z;
			Init();
			if (bulletClone == null)
			{
				bulletClone = Instantiate(AssetPrefabs.instance.basicBulletPrefab);
				bulletClone.SetActive(false);
			}
		}
		public void Init()
		{
			timeAlive = 0;
			
			pattern.dataValues[PatternDataKey.FireShot] = 1;
			pattern.dataValues[PatternDataKey.Rotation] = 36f;
			ReloadType = ReloadType;

			if (pattern.timeline.data.Count > 0) return;
			foreach (PatternDataKey d in GetAllowedValues())
			{
				pattern.timeline.data.Add(d, new AnimationCurve()
				{
					preWrapMode = WrapMode.Loop,
					postWrapMode = WrapMode.Loop
				});
			}
		}

		public void SetMounting(HardPoint point)
		{
			mountingPoint = point;
		}

		public void SetShell(InventoryItem item)
		{
			Destroy(bulletClone);
			bulletClone = null;
			if (item == null || item.upgradeTypeData.type != UpgradeType.Bullet) return;

			bulletClone = Instantiate(item.upgradeTypeData.relevantPrefab);
			bulletClone.SetActive(false);
			bulletClone.GetComponent<Bullet>().SetPattern(item.upgradeTypeData.relevantPattern);
			bulletClone.GetComponent<SpriteRenderer>().sprite = item.upgradeTypeData.image;
		}

		public void SetShell(GameObject prefab, PatternData patt)
		{
			bulletClone = Instantiate(prefab);
			bulletClone.SetActive(false);
			bulletClone.GetComponent<Bullet>().SetPattern(patt);
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			if (!active) return;
			float dt = Time.fixedDeltaTime;
			timeAlive += dt;
			foreach (PatternDataKey d in GetAllowedValues())
			{
				currentValues[d] = pattern.dataValues[d] * pattern.timeline.Evaluate(d, timeAlive * (pattern.Lifetime / 10));
			}
			
			if (pattern.effects.AimAtPlayer)
			{
				float bestAngle = 180;
				Vector3 bestVec = Vector3.zero;
				foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 20, LayerMask.GetMask(new[] { "AIPlayer" })))
				{
					Transform playerTransform = c.transform;

					Vector3 relativePos = playerTransform.position.ReplaceZ(0) - transform.position.ReplaceZ(0);
					Vector3 forward = transform.right;
					var angle = Vector3.SignedAngle(relativePos.ReplaceZ(0), forward, transform.forward);

					if (Math.Abs(angle) < Math.Abs(bestAngle))
					{
						bestAngle = angle;
						bestVec = playerTransform.position.ReplaceZ(0);
					}
				}
				
				Vector3 objectPos = transform.position;
				bestVec.x = objectPos.x - bestVec.x;
				bestVec.y = objectPos.y - bestVec.y;

				transform.rotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(bestVec.y, bestVec.x) * Mathf.Rad2Deg - 90));
			}
			else if (pattern.effects.AimScreenDown)
			{
				transform.localEulerAngles = -transform.parent.localEulerAngles;
			}
			else
			{
				transform.Rotate(Vector3.forward, currentValues[PatternDataKey.Rotation] * dt * GetStat(StatAttribute.GunSpeed, () => 1, (a, b) => a * b), Space.Self);
				
				float ang = GetStat(StatAttribute.AngleRestriction, () => 0, (a, b) => a + b) / 2;
				float f = transform.localEulerAngles.z - pattern.StartAngle;
				if (f > 180) f -= 360;
				transform.localEulerAngles = transform.localEulerAngles.ReplaceZ(Mathf.Clamp(f, -ang, ang) + pattern.StartAngle);
			}
			
			if (ReloadType == GunType.None) return;

			fireDelay -= dt;

			if (currentValues[PatternDataKey.FireShot] >= 0.95f)
			{
				if(availableShots > 0)
				{
					if (ReloadType == GunType.Continuous)
					{
						availableShots += Mathf.RoundToInt(reloadTime / ReloadTime * MaxCapacity);
					}
					
					reloadTime = 0;
				}
				Fire();
			}

			if (availableShots <= 0 || currentValues[PatternDataKey.FireShot] < 0.95f)
			{
				reloadTime += dt * (currentValues[PatternDataKey.FireShot] >= 0.999f ? 1 : 5);
				if (reloadTime >= ReloadTime)
				{
					availableShots = Mathf.RoundToInt(MaxCapacity);
				}
			}
		}

		private float GetStat(StatAttribute stat, Func<float> baseValue, Func<float,float,float> combine)
		{
			float r = baseValue();
			if(gunBaseModifiers.TryGetValue(stat, out var modifier))
			{
				r = combine(r, modifier);
			}
			if(mountingPoint != null)
			{
				r = combine(r, mountingPoint.GetStat(stat, baseValue, combine));
			}
			return r;
		}

		public void Fire()
		{
			if (ReloadType == GunType.None) return;
			if (fireDelay > 0 || availableShots <= 0) return;
			if(muzzles.Length == 0) Debug.LogError("Gun barrel has no muzzles!");
			foreach (Transform tr in muzzles)
				Fire(tr);
			
			availableShots--;

			fireDelay = FiringRate;
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
			Bullet b = go.GetComponent<Bullet>();
			SetBulletDetails(b);
		}

		private void SetBulletDetails(Bullet bul)
		{
			if(mountingPoint == null && pattern.childPattern != null)
				bul.SetPattern(pattern.childPattern);
			bul.gameObject.layer = gameObject.layer + 2;
			bul.SetDamage(GetStat(StatAttribute.Damage, () => 0, (a, b) => a + b));
			bul.SetLifetime(GetStat(StatAttribute.Lifetime, () => 1, (a, b) => a * b));

			bul.SetStat(PatternDataKey.Speed, GetStat(StatAttribute.BulletSpeed, () => 1, (a, b) => a * b));
			if (mountingPoint != null)
			{
				bul.pattern.effects = bul.pattern.effects.CombineIntoNew(mountingPoint.GetPatternModifiers());
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

		public PatternData GetPatternData()
		{
			return pattern;
		}

		public IHasPattern GetSubsystem()
		{
			if(bulletClone == null) return null;
			return bulletClone.GetComponent<Bullet>();
		}

		public void SetPattern(PatternData patternIn)
		{
			pattern.CopyFrom(patternIn);
		}

		public void SetTime(int v)
		{
			transform.localRotation = Quaternion.identity;
			transform.Rotate(Vector3.forward, pattern.StartAngle);
			availableShots = Mathf.RoundToInt(MaxCapacity);
			fireDelay = 0;
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