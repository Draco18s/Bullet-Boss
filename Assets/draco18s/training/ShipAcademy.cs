using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.training;
using Assets.draco18s.util;
using Unity.MLAgents;
using UnityEngine;

public class ShipAcademy : MonoBehaviour
{
	public AgentShip agent;
	private EnvironmentParameters m_ResetParams;

	public void Awake()
	{
		Academy.Instance.OnEnvironmentReset += EnvironmentReset;
		m_ResetParams = Academy.Instance.EnvironmentParameters;
	}

	void EnvironmentReset()
	{
		Debug.Log("Reset environment!");
		RandomBossSelector.instance.GameLayer.transform.Clear();
		RandomBossSelector.instance.FightBoss(Mathf.RoundToInt(m_ResetParams.GetWithDefault("BossNumber",0)));
	}
}
