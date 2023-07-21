using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using Assets.draco18s.training;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentTrainer : MonoBehaviour
{
	public static AgentTrainer instance;
	public Transform FieldArea;
	public const int no_decisions = 3;
	public const int no_observations = 128;
	[System.Serializable]
	public class PlayerObservationDecision
	{
		public float[] observation;
		public float[] decision;
		public float weight;
	}

	public string loadFile = "";

	public int numAI = 10;
	public GameObject playerPrefab;
	public List<AgentShip> AiFighters = new List<AgentShip>();
	public PlayerRecorder humanShip;

	public bool simStarted = false;
	
	void Start()
	{
		instance = this;
	}

	private float startTime = 0;

	// Update is called once per frame
	void Update()
	{
		if (simStarted && AiFighters.Count != numAI)
		{
			foreach (var ai in AiFighters)
			{
				Destroy(ai.gameObject);
			}
			AiFighters.Clear();
			for (int i = 0; i < numAI; i++)
			{
				AgentShip f = Instantiate(playerPrefab, FieldArea).GetComponent<AgentShip>();
				f.gameObject.transform.Translate(Vector3.forward * i, Space.Self);
				AiFighters.Add(f);
			}
			ResetSim();
			return;
		}
		if (simStarted && Time.time - startTime > 45)
		{
			simStarted = false;
			ResetSim();
		}
	}

	private int lastBoss = -1;

	private void ResetSim()
	{
		simStarted = false;
		RandomBossSelector.instance.NoBoss();
		int r = Mathf.FloorToInt(Random.value * RandomBossSelector.instance.BossChoices.Length);
		if (r == lastBoss) r = Mathf.FloorToInt(Random.value * RandomBossSelector.instance.BossChoices.Length);
		RandomBossSelector.instance.FightBoss(r);
		lastBoss = r;
		startTime = Time.time;
		simStarted = true;
	}

	public void StartGame()
	{
		ResetSim();
	}
}
