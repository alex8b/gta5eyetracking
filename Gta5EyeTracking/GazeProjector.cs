using System;
using System.Diagnostics;
using GTA;
using GTA.Math;

namespace Gta5EyeTracking
{
	public class GazeProjector
	{
		private readonly Settings _settings;

		private readonly Stopwatch _missileLockedStopwatch;
		private readonly TimeSpan _missileLockedMinTime;
		private Entity _missileTarget;
		private Vector2 _filteredGazePoint;

		public GazeProjector(Settings settings)
		{
			_settings = settings;

			_missileLockedStopwatch = new Stopwatch();
			_missileLockedStopwatch.Restart();
			_missileLockedMinTime = TimeSpan.FromSeconds(0.75);
		}

		public void FindGazeProjection(
			Vector2 gazePoint,
			Vector2 joystickDelta,
            out Vector3 shootCoord, 
			out Vector3 shootCoordSnap, 
			out Vector3 shootMissileCoord, 
			out Ped ped, 
			out Entity missileTarget
			)
		{
			Entity target = null;
			
			var w = (float)(1 - _settings.GazeFiltering * 0.9);
			_filteredGazePoint = new Vector2(_filteredGazePoint.X + (gazePoint.X - _filteredGazePoint.X) * w,
				_filteredGazePoint.Y + (gazePoint.Y - _filteredGazePoint.Y) * w);

			var filteredGazePointPlusJoystickDelta = _filteredGazePoint + joystickDelta;
			var unfilteredGazePointPlusJoystickDelta = gazePoint;

			Entity unfilteredEntity;
			Entity filteredEntity;
			var hitUnfiltered = Geometry.ConecastPedsAndVehicles(unfilteredGazePointPlusJoystickDelta, out unfilteredEntity);
			shootMissileCoord = hitUnfiltered;
			shootCoordSnap = hitUnfiltered;


			var hitFiltered = Geometry.RaycastEverything(filteredGazePointPlusJoystickDelta, out filteredEntity, true);
			shootCoord = hitFiltered;

			if (unfilteredEntity != null
				&& Util.IsEntityAPed(unfilteredEntity))
			{
				ped = unfilteredEntity as Ped;
			}
			else
			{
				ped = Geometry.SearchPed(unfilteredGazePointPlusJoystickDelta);
			}

			if ((ped != null)
				&& (ped.Handle != Game.Player.Character.Handle))
			{
				shootCoordSnap = ped.GetBoneCoord(Bone.SKEL_L_Clavicle);
				target = ped;
				if (_settings.SnapAtPedestriansEnabled)
				{
					shootCoord = shootCoordSnap;
				}
			}
			else
			{
				Vehicle vehicle;
				if (unfilteredEntity != null
					&& Util.IsEntityAVehicle(unfilteredEntity))
				{
					vehicle = unfilteredEntity as Vehicle;
				}
				else
				{
					vehicle = Geometry.SearchVehicle(unfilteredGazePointPlusJoystickDelta);
				}

				if (vehicle != null
					&& !((Game.Player.Character.IsInVehicle())
						&& (vehicle.Handle == Game.Player.Character.CurrentVehicle.Handle)))
				{
					shootCoordSnap = vehicle.Position + vehicle.Velocity * 0.06f;
					shootMissileCoord = shootCoordSnap;
					target = vehicle;
				}
			}

			ProcessMissileLock(target);

			missileTarget = _missileTarget;
		}

		private void ProcessMissileLock(Entity target)
		{
			if (target != null && target.IsAlive)
			{
				_missileTarget = target;
				_missileLockedStopwatch.Restart();
			}

			if (_missileLockedStopwatch.Elapsed > _missileLockedMinTime)
			{
				_missileTarget = null;
			}
		}
	}
}
