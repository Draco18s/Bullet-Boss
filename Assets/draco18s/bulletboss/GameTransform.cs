using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTransform : MonoBehaviour
{
	public static GameTransform instance;

	void Start()
	{
		instance = this;
	}
}
