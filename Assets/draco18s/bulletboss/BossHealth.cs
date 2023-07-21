using JetBrains.Annotations;
using Assets.draco18s.ui;
using Assets.draco18s.util;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.bulletboss
{
	public class BossHealth : MonoBehaviour
	{
		public GameObject barPrefab;
		public float[] HealthSegments = {50,50,50};
		public UnityAction DamageThreshold;

		[UsedImplicitly]
		void Start()
		{
			Recalculate();
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
