using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTransform : MonoBehaviour
{
	public static GameTransform instance;
	public GameObject basicBulletPrefab;

	void Start()
	{
		instance = this;
	}
}
