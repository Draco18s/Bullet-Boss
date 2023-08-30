using System.Collections;
using Assets.draco18s;
using Assets.draco18s.bulletboss;
using Assets.draco18s.util;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainingBoss : MonoBehaviour
{
	public float timeMulti = 3.5f;
	public float timeMin = 0.25f;
	public Transform playerTarget;
	public Transform container;
	[SerializeField] private GunBarrel[] guns;
	[SerializeField] private GunBarrel[] fixedGuns;
	private float maxWaitTime;
	private float timer;
	private float episodeCount = 0;

	[UsedImplicitly]
	void Start()
	{
		maxWaitTime = Random.value * timeMulti + timeMin;
	}

	public void UpdateValues(float score)
	{
		if (score < 5)
		{
			return;
		}
		if (episodeCount % 50 == 0)
		{
			timeMin = Mathf.Max(timeMin - 0.001f, 0.05f);
		}
		if (episodeCount % 10 == 0)
		{
			timeMulti = Mathf.Max(timeMulti - 0.0025f, 0.5f);
		}
		episodeCount++;
	}

	[UsedImplicitly]
	void FixedUpdate()
	{
		if (GameStateManager.instance.state != GameStateManager.GameState.ActiveTraining
		    && GameStateManager.instance.state != GameStateManager.GameState.RecordDemo) return;
		if (guns.Length == 0)
		{
			guns = GetComponentsInChildren<GunBarrel>();
			foreach (GunBarrel g in guns)
			{
				g.pattern.dataValues[PatternDataKey.FireShot] = 0;
			}
			foreach (GunBarrel g in fixedGuns)
			{
				g.pattern.dataValues[PatternDataKey.FireShot] = 0;
			}
		}

		transform.localPosition = transform.localPosition.ReplaceX(playerTarget.localPosition.x);
		timer += Time.fixedDeltaTime;
		if (timer < maxWaitTime) return;
		timer = 0;
		maxWaitTime = Random.value * timeMulti + timeMin;
		int v = Mathf.FloorToInt(Random.value * (guns.Length + fixedGuns.Length));
		if (v < guns.Length)
		{
			GunBarrel gun1 = guns[v];
			StartCoroutine(FireGun(gun1));
		}
		else
		{
			GunBarrel gun2 = fixedGuns[v - guns.Length];
			StartCoroutine(FireGunStraight(gun2));
		}
	}

	private IEnumerator FireGunStraight(GunBarrel gun)
	{
		yield return new WaitForFixedUpdate();
		gun.pattern.dataValues[PatternDataKey.FireShot] = 1;
		yield return new WaitForFixedUpdate();

		gun.pattern.dataValues[PatternDataKey.FireShot] = 0;
	}

	private IEnumerator FireGun(GunBarrel gun)
	{
		if (Random.value > 0.9f)
		{
			Vector3 bestVec = playerTarget.transform.position;
			
			Vector3 objectPos = gun.transform.position;
			bestVec.x = objectPos.x - bestVec.x;
			bestVec.y = objectPos.y - bestVec.y;

			float a = Mathf.Atan2(bestVec.y, bestVec.x) * Mathf.Rad2Deg - 90 + Random.value * 10f - 5;
			gun.pattern.StartAngle = a;
			gun.transform.rotation = Quaternion.Euler(new Vector3(0, 0, a));
		}
		else
		{
			gun.transform.localEulerAngles = Vector3.zero;
		}
		yield return new WaitForFixedUpdate();
		gun.pattern.dataValues[PatternDataKey.FireShot] = 1;
		yield return new WaitForFixedUpdate();

		gun.pattern.dataValues[PatternDataKey.FireShot] = 0;
	}
}
