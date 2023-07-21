using System.Collections;
using System.Collections.Generic;
using Assets.draco18s;
using Assets.draco18s.training;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimTrainer : MonoBehaviour
{
	public static SimTrainer instance;
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
	public List<AIFighter> AiFighters = new List<AIFighter>();
	public PlayerRecorder humanShip;

	public bool simStarted = false;
	public int currentRun;
	public float randomness = 1;
	public int runsPerTraining = 20;
	public float StartTime = 0;

	private List<PlayerObservationDecision> trainingSet = new List<PlayerObservationDecision>();
	private Noedify_Solver trainingSolver;
	public Noedify.Net net;

	[Space(10)]
	[Header("Training Parameters")]
	public int trainingBatchSize = 4;
	public int trainingEpochs = 100;
	public float trainingRate = 1f;
	const int N_threads = 8;

	void Start()
	{
		instance = this; net = new Noedify.Net();
		trainingSolver = Noedify.CreateSolver();
		Noedify.Layer inputLayer = new Noedify.Layer(Noedify.LayerType.Input, no_observations, "input");
		net.AddLayer(inputLayer);
		Noedify.Layer hiddenLayer1 = new Noedify.Layer(Noedify.LayerType.FullyConnected, no_observations/2, Noedify.ActivationFunction.Sigmoid, "L1");
		net.AddLayer(hiddenLayer1);
		Noedify.Layer hiddenLayer2 = new Noedify.Layer(Noedify.LayerType.FullyConnected, no_observations/4, Noedify.ActivationFunction.Sigmoid, "L2");
		net.AddLayer(hiddenLayer2);
		Noedify.Layer hiddenLayer3 = new Noedify.Layer(Noedify.LayerType.FullyConnected, no_observations/8, Noedify.ActivationFunction.Sigmoid, "L2");
		net.AddLayer(hiddenLayer3);
		Noedify.Layer outputLayer = new Noedify.Layer(Noedify.LayerType.Output, no_decisions, "output");
		net.AddLayer(outputLayer);
		net.BuildNetwork();
	}
	internal void AddTrainingSet(PlayerObservationDecision newSet)
	{
		trainingSet.Add(newSet);
	}

	private float startTime = 0;

	// Update is called once per frame
	void Update()
	{
		if (simStarted && !string.IsNullOrEmpty(loadFile))
		{
			net.LoadModel(loadFile, "E:\\HellTraining");
			loadFile = "";
		}
		if (simStarted && AiFighters.Count != numAI)
	    {
		    foreach (var ai in AiFighters)
		    {
				Destroy(ai.gameObject);
		    }
		    AiFighters.Clear();
		    for (int i = 0; i < numAI; i++)
		    {
			    AIFighter f = Instantiate(playerPrefab, FieldArea).GetComponent<AIFighter>();
				f.gameObject.transform.Translate(Vector3.forward * i, Space.Self);
				AiFighters.Add(f);
			}
		    ResetSim();
		    return;
	    }
		if (simStarted && (CheckPlayersDead() || Time.time - startTime > 45))
		{
			if (currentRun % runsPerTraining == 0 & currentRun >= runsPerTraining)
			{
				StartCoroutine(TrainNetwork());
			}
			simStarted = false;
			ResetSim();
		}
	}

	private void ResetSim()
	{
		StartCoroutine(ResetSimCo());
	}

	private int lastBoss = -1;

	private IEnumerator ResetSimCo()
	{
		simStarted = false;
		RandomBossSelector.instance.NoBoss();
		yield return new WaitForSeconds(3);
		while (trainingSolver.trainingInProgress) yield return new WaitForSeconds(1);
		foreach (AIFighter f in AiFighters)
		{
			f.Reset();
		}
		humanShip.Reset();
		int r = Mathf.FloorToInt(Random.value * RandomBossSelector.instance.BossChoices.Length);
		if (r == lastBoss) r = Mathf.FloorToInt(Random.value * RandomBossSelector.instance.BossChoices.Length);
		RandomBossSelector.instance.FightBoss(r);
		lastBoss = r;
		StartTime = Time.time;
		currentRun++;
		if (currentRun % (runsPerTraining * 2) == 0 && currentRun > runsPerTraining)
			randomness = randomness - 0.05f;
		if (randomness < 0.3f)
			randomness = 0.9f;
		simStarted = true;
		startTime = Time.time;
	}

	List<float[,,]> observation_inputs = new List<float[,,]>();
	List<float[]> decision_outputs = new List<float[]>();
	List<float> trainingSetWeights = new List<float>();

	private IEnumerator TrainNetwork()
	{
		if (trainingSet != null)
		{
			if(trainingSolver.trainingInProgress) yield break;
			if (trainingSet.Count > 0)
			{
				yield return null;
				for (int n = trainingSetWeights.Count; n < trainingSet.Count; n++)
				{
					observation_inputs.Add(Noedify_Utils.AddTwoSingularDims(trainingSet[n].observation));
					decision_outputs.Add(trainingSet[n].decision);
					trainingSetWeights.Add(trainingSet[n].weight);
				}
				trainingSolver.TrainNetwork(net, observation_inputs, decision_outputs, trainingEpochs, trainingBatchSize, trainingRate, Noedify_Solver.CostFunction.MeanSquare, Noedify_Solver.SolverMethod.Background, trainingSetWeights, N_threads);
				float[] cost = trainingSolver.cost_report;
				yield return (PlotCostWhenComplete(trainingSolver, cost));
			}
		}
	}

	private int networkChkt = 1;
	IEnumerator PlotCostWhenComplete(Noedify_Solver solver, float[] cost)
	{
		while (solver.trainingInProgress)
		{
			yield return null;
		}
		net.SaveModel("trainedModel_" + networkChkt, "E:\\HellTraining");
		networkChkt++;
		trainingSet.Clear();
	}

	private bool CheckPlayersDead()
	{
		int totalMaxHP = 0;
		int totalHPlost = 0;
		foreach (AIFighter f in AiFighters)
		{
			totalMaxHP += f.MaxHP;
			totalHPlost += (f.MaxHP - f.CurHP);
			//if (!f.IsDead())
			//	return false;
		}

		return (totalHPlost > 0.75f * totalMaxHP);
	}

	public void StartGame()
	{
		StartCoroutine(ResetSimCo());
	}
}
