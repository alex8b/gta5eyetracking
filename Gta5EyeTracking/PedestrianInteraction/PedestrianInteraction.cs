using System;
using System.Collections.Generic;
using System.Linq;
using GTA;

namespace Gta5EyeTracking
{
	public class PedestrianInteraction
	{
		private Ped _lastPed;
		private Dictionary<int, PedInfo> _pedInfos = new Dictionary<int, PedInfo>();

		public PedestrianInteraction()
		{

		}

		public void MindControl(int handle)
		{
			if (_pedInfos.ContainsKey(handle))
			{
				_pedInfos[handle].Pedestrian.Task.ClearAll();
				_pedInfos[handle].Pedestrian.Task.FleeFrom(Game.Player.Character);
			}
		}

		public void ProcessLookingAtPedestrion(Ped ped, TimeSpan time)
		{
			_lastPed = ped;
			var rotationDiff = ped.Rotation - Game.Player.Character.Rotation;
			var rotationDiffBound = Geometry.BoundRotationDeg(rotationDiff.Z);
			//_debugText4.Caption = ped.Rotation.Z + " | " + Game.Player.Character.Rotation.Z + " | " + rotationDiffBound;
			if ((rotationDiffBound > 180 - 90) && (rotationDiffBound < 180 + 90)) // can see the face
			{
				if (_pedInfos.ContainsKey(ped.Handle))
				{
					_pedInfos[ped.Handle].LastLookTime = DateTime.UtcNow;
					_pedInfos[ped.Handle].TotalLookTime += time;
				}
				else
				{
					_pedInfos[ped.Handle] = new PedInfo
					{
						LastLookTime = DateTime.UtcNow,
						TotalLookTime = TimeSpan.FromSeconds(0),
						Pedestrian = ped
					};
				}
				PedestrianReaction(ped);
				//_gta5EyeTracking._debugText1.Caption = "ped: " + ped.Handle + " | " + ped.Gender + " | " +
				//										(int) _pedInfos[ped.Handle].TotalLookTime.TotalMilliseconds;
			}
		}

		private void PedestrianReaction(Ped ped)
		{
			var handle = ped.Handle;
			if (!_pedInfos.ContainsKey(handle)) return;

			var reactionTime = TimeSpan.FromSeconds(2);

			if (_pedInfos[handle].TotalLookTime <= reactionTime || _pedInfos[handle].Triggered) return;
			
			_pedInfos[handle].Triggered = true;
			_pedInfos[handle].TotalLookTime = TimeSpan.FromSeconds(0);
			_pedInfos[handle].Pedestrian.Task.ClearAll();
			_pedInfos[handle].Pedestrian.Task.LookAt(Game.Player.Character);
			_pedInfos[handle].Pedestrian.Task.TurnTo(Game.Player.Character);
			_pedInfos[handle].Pedestrian.Task.ChatTo(Game.Player.Character);
		}

		public void Process()
		{
			var now = DateTime.UtcNow;
			var maxTime = TimeSpan.FromSeconds(5);
			_pedInfos = _pedInfos.Where(pair => (now - pair.Value.LastLookTime) < maxTime)
				.ToDictionary(pair => pair.Key,
					pair => pair.Value);
		}
	}
}