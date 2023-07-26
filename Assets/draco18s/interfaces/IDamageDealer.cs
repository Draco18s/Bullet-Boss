using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.draco18s
{
	public interface IDamageDealer : IDamageTaker
	{
		void AddScore(float amt, Collider2D col);
		public float GetCurrentHealth();
		public float GetMaxHealth();
		float ApplyGraze(float damage, Collider2D col);
	}
}
