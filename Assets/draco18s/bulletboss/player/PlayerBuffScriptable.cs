using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	[CreateAssetMenu(fileName = "Upgrade", menuName = "BulletBoss/PlayerCollectable", order = 2)]
	public class PlayerBuffScriptable : ScriptableObject
	{
		public enum BuffType
		{
			Unknown, Score, Energy, PowerUp
		}

		public BuffType BonusType;
		public int ScoreValue;
		public Sprite Image;
		public GameObject RelevantPrefab;
	}
}