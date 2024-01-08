using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.draco18s.util;
using UnityEngine;

namespace Assets.draco18s
{
	[Serializable]
	public class PatternEffects
	{
		public TriValue MirrorSpreadShots = TriValue.Default.Clone();
		public TriValue HomingShots = TriValue.Default.Clone();
		public TriValue AimAtPlayer = TriValue.Default.Clone();
		public TriValue AimScreenDown = TriValue.Default.Clone();

		public PatternEffects CombineIntoNew(PatternEffects other)
		{
			PatternEffects ret = new PatternEffects();
			ret.MirrorSpreadShots = MirrorSpreadShots || other.MirrorSpreadShots;
			ret.HomingShots = HomingShots || other.HomingShots;
			ret.AimAtPlayer = AimAtPlayer || other.AimAtPlayer;
			ret.AimScreenDown = AimScreenDown || other.AimScreenDown;
			return ret;
		}

		public void MergeWith(PatternEffects other)
		{
			MirrorSpreadShots = MirrorSpreadShots || other.MirrorSpreadShots;
			HomingShots = HomingShots || other.HomingShots;
			AimAtPlayer = AimAtPlayer || other.AimAtPlayer;
			AimScreenDown = AimScreenDown || other.AimScreenDown;
		}

		public PatternEffects Copy()
		{
			PatternEffects ret = new PatternEffects();
			ret.MirrorSpreadShots = MirrorSpreadShots.Clone();
			ret.HomingShots = HomingShots.Clone();
			ret.AimAtPlayer = AimAtPlayer.Clone();
			ret.AimScreenDown = AimScreenDown.Clone();
			return ret;
		}

		public float GetCost()
		{
			return (AimAtPlayer ? 2.5f : 0) + (AimScreenDown ? -1 : 0) + (HomingShots ? 5 : 0);
		}
	}
}
