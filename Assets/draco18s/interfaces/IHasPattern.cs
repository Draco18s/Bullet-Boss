using System.Collections.Generic;
using UnityEngine;

namespace Assets.draco18s
{
	public interface IHasPattern
	{
		public float CurrentTime { get; }
		public PatternData Pattern { get; }
		void SetTime(int v);
		void Init();
		PatternData GetSubsystem();
		void SetPattern(PatternData pattern);
	}
}
