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
		float ApplyGraze(float damage, Collider2D bulletCol);
		void DamagedEnemy(float damage, Collider2D enemyCol);
	}
}
