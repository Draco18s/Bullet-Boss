using System;
using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using Assets.draco18s.util;
using UnityEditor;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	[CreateAssetMenu(fileName = "Upgrade", menuName = "BulletBoss/Upgrade", order = 1)]
	public class UpgradeScriptable : ScriptableObject
	{
		public UpgradeRuntime data;

		void OnValidate()
		{
			data.upgradeID = this.name;
		}
	}
}