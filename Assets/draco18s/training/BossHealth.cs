using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.draco18s.training
{
	internal class BossHealth : MonoBehaviour, IDamageTaker
	{
		public UnityEvent OnTakeDamage { get; set; } = new UnityEvent();
		public float ApplyDamage(float damage, Collider2D col)
		{
			return damage;
		}

		public float ApplyGraze(float damage, Collider2D col)
		{
			return 0;
		}
	}
}
