using Assets.draco18s.ui;
using Assets.draco18s.util;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class PlayerHealth : MonoBehaviour
	{
		public GameObject playerShip;
		public GameObject heartPrefab;
		public GameObject dimHeartPrefab;
		private IDamageDealer ship;

		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			ship = playerShip.GetComponent<IDamageDealer>();
			ship.OnTakeDamage.AddListener(Recalculate);
			Recalculate(0);
		}

		public void Recalculate(float t)
		{
			transform.Clear();
			for (int i = 0; i < ship.GetMaxHealth(); i++)
			{
				if (i + 1 <= ship.GetCurrentHealth())
					Instantiate(heartPrefab, transform);
				else
					Instantiate(dimHeartPrefab, transform);
			}
		}
	}
}