using System;
using GTA;

namespace Gta5EyeTracking
{
	public class PedInfo
	{
		public DateTime LastLookTime;
		public TimeSpan TotalLookTime;
		public Ped Pedestrian;
		public bool Triggered;
	}
}