using JetBrains.Annotations;
using Assets.draco18s.ui;
using Assets.draco18s.util;
using UnityEngine;
using UnityEngine.Events;
using Assets.draco18s.training;

namespace Assets.draco18s.bulletboss
{
	public class BossHealthBar : MonoBehaviour
	{
		public GameObject barPrefab;
		public float[] HealthSegments = {50,50,50};
		public UnityEvent DamageThreshold = new UnityEvent();

		[UsedImplicitly]
		void Start()
		{
			Recalculate();
			FindFirstObjectByType<BossHealth>().OnTakeDamage.AddListener((amt) =>
			{
				ApplyDamage(amt);
			});
		}

		public void Recalculate()
		{
			transform.Clear();
			foreach (float f in HealthSegments)
			{
				GameObject bar = Instantiate(barPrefab, transform);
				PercentageBar perc = bar.GetComponent<PercentageBar>();
				perc.total = f;
				perc.current = f;
			}
		}

		public void ApplyDamage(float amt)
		{
			for (int i = HealthSegments.Length - 1; i >= 0; --i)
			{
				if (HealthSegments[i] <= 0) continue;
				HealthSegments[i] -= amt;
				if (HealthSegments[i] <= 0)
					DamageThreshold.Invoke();
				break;
			}
		}
	}
}
