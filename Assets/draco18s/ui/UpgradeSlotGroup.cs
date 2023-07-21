using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.draco18s.training;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.ui
{
	public class UpgradeSlotGroup : MonoBehaviour
	{
		public static UpgradeSlotGroup instance;
		public GunSlotTarget[] slots;

		[UsedImplicitly]
		void Start()
		{
			instance = this;
		}

		public void Detail(HardPointTarget hardPoint)
		{
			transform.position = Camera.main.WorldToScreenPoint(hardPoint.transform.position);
			GunBarrel gun = hardPoint.gun;
		}
	}
}
