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

		public PatternEffects Merge(PatternEffects other)
		{
			PatternEffects ret = new PatternEffects();
			ret.MirrorSpreadShots = MirrorSpreadShots || other.MirrorSpreadShots;
			ret.HomingShots = HomingShots || other.HomingShots;
			ret.AimAtPlayer = AimAtPlayer || other.AimAtPlayer;
			ret.AimScreenDown = AimScreenDown || other.AimScreenDown;
			return ret;
		}
	}
}
