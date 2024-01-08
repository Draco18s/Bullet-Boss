using Assets.draco18s;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetPrefabs : MonoBehaviour
{
	public static AssetPrefabs instance;
	public GameObject basicBulletPrefab;
	// Start is called before the first frame update
	void Start()
	{
		instance = this;
	}
}
