using System.Collections;
using System.Collections.Generic;
using Assets.draco18s.bulletboss;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HiddenSprite : MonoBehaviour
{
	private SpriteRenderer renderer;
	void Start()
	{
		renderer = GetComponent<SpriteRenderer>();
		GameStateManager.instance.onChanged += UpdateState;
	}

	private void UpdateState(GameStateManager.GameState state)
	{
		renderer.enabled = state == GameStateManager.GameState.Manage;
	}
}
