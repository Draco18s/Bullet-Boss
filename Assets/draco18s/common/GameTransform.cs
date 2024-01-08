using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.draco18s
{
	public class GameTransform : MonoBehaviour
	{
		public static GameTransform instance => FindAnyObjectByType<GameTransform>();
	}
}