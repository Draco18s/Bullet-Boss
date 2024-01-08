using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using Assets.draco18s.bulletboss;
using Assets.draco18s.training;
using UnityEngine;

public class RandomBossSelector : MonoBehaviour
{
	public static RandomBossSelector instance;
	public GameObject[] BossChoices;
	//public Transform GameLayer;

	void Start()
	{
		instance = this;
	}

	public void NoBoss()
	{
		for (int j = 0; j < BossChoices.Length; j++)
		{
			BossChoices[j].SetActive(false);
		}
	}

	public void FightBoss(int i)
	{
		i = i % BossChoices.Length;
		for (int j = 0; j < BossChoices.Length; j++)
		{
			BossChoices[j].SetActive(j == i);
			foreach (GunBarrel gun in BossChoices[j].GetComponentsInChildren<GunBarrel>())
			{
				gun.SetTime(0);
			}
		}
	}
}
