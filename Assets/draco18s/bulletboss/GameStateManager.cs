using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class GameStateManager : MonoBehaviour
	{
		public static GameStateManager instance;

		public enum GameState
		{
			MainMenu, Paused, Manage, InGame, GameOver, ActiveTraining
		}

		public GameState state;

		[UsedImplicitly]
		void Start()
		{
			instance = this;
			state = GameState.Manage;
		}

		public void BeginGame()
		{
			state = GameState.ActiveTraining;
		}
	}
}
