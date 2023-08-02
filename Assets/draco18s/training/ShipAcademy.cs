using Unity.MLAgents;
using UnityEngine;

namespace Assets.draco18s.training
{
	public class ShipAcademy : MonoBehaviour
	{
		public static ShipAcademy instance;
		public GameObject[] ToggleableThings;
		public float FighterSpawnRate = 0.2f;
		public float GemDropRate = 0.6f;

		void Start()
		{
			instance = this;
			//Academy.Instance.OnEnvironmentReset += ConfigureEnvironment;
		}

		public void ConfigureEnvironment()
		{
			int numOn = 0;
			foreach (GameObject go in ToggleableThings)
			{
				go.SetActive(Random.value < FighterSpawnRate);
				numOn += go.activeSelf ? 1 : 0;
			}

			if (numOn < 3)
			{
				ConfigureEnvironment();
			}
		}
	}
}
