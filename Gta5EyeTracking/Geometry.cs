using System;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Gta5EyeTracking
{
	public static class Geometry
	{
		public static bool IsFirstPersonCameraActive()
		{
			//return !GameplayCamera.IsRendering;
			var dist = Game.Player.Character.Position - GameplayCamera.Position;
			dist.Z = 0;

			//_debugText5.Caption = Math.Round(Game.Player.Character.Position.X, 1) + " | "
			//	+ Math.Round(Game.Player.Character.Position.Y, 1) + " ||| "
			//	+ Math.Round(GameplayCamera.Position.X, 1) + " | "
			//	+ Math.Round(GameplayCamera.Position.Y, 1) + " | "
			//	+ Math.Round(GameplayCamera.Position.Z, 1) + " | "
			//	+ Math.Round(dist.Length(), 1);

			return (dist.Length() < 0.2);
		}

		public static bool IsInFrontOfThePlayer(Vector3 shootCoord)
		{
			return true;

			//var playerRotation = Game.Player.Character.Rotation;
			//var shootVector = shootCoord - Game.Player.Character.Position;
			//var shootRotation = Util.DirectionToRotation(shootVector);
			//var rotationDiff = shootRotation - playerRotation;
			//var rotationDiffBound = BoundRotationDeg(rotationDiff.Z);
			//return ((rotationDiffBound < 90) || (rotationDiffBound > 270));
		}

		public static Vector3 RaycastEverything(Vector2 screenCoord)
		{
			var camPos = GameplayCamera.Position;
			var camRot = GameplayCamera.Rotation;
			const float raycastToDist = 100.0f;
			const float raycastFromDist = 1f;

			var target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
			var source3D = camPos;

			Entity ignoreEntity = Game.Player.Character;
			if (Game.Player.Character.IsInVehicle())
			{
				ignoreEntity = Game.Player.Character.CurrentVehicle;
			} 

			var dir = (target3D - source3D);
			dir.Normalize();
			var raycastResults = World.Raycast(source3D + dir * raycastFromDist,
				source3D + dir * raycastToDist,
				(IntersectOptions)(1 | 16 | 256 | 2 | 4 | 8)// | peds + vehicles
				, ignoreEntity);

			if (raycastResults.DitHitAnything)
			{
				return raycastResults.HitCoords;
			}

			return camPos + dir * raycastToDist;
		}

		public static Ped RaycastPed(Vector2 screenCoords)
		{
			const double searchRange = 0.1;
			var peds = World.GetNearbyPeds(Game.Player.Character, 100);
			var mindist = Double.MaxValue;
			Ped foundPed = null;
			foreach (var ped in peds)
			{
				if (ped.Handle == Game.Player.Character.Handle) continue;
				if (ped.IsOccluded) continue;

				{
					var headOffest = ped.GetBoneCoord(Bone.SKEL_ROOT);
					Vector2 pedScreenCoords;
					if (WorldToScreenRel(headOffest, out pedScreenCoords))
					{
						var dist = (screenCoords - pedScreenCoords).Length();
						if (dist < mindist)
						{
							mindist = dist;
							foundPed = ped;
						}
					}
				}

				{
					var headOffest = ped.GetBoneCoord(Bone.SKEL_Head);
					Vector2 pedScreenCoords;
					
					if (!WorldToScreenRel(headOffest, out pedScreenCoords)) continue;
					
					var dist = (screenCoords - pedScreenCoords).Length();
					
					if (!(dist < mindist)) continue;
					mindist = dist;
					foundPed = ped;
				}

			}
			return mindist < searchRange ? foundPed : null;
		}

		public static Vehicle RaycastVehicle(Vector2 screenCoords)
		{
			const double searchRange = 0.1;
			var vehs = World.GetNearbyVehicles(Game.Player.Character, 200);
			var mindist = Double.MaxValue;
			Vehicle foundVeh = null;
			foreach (var vehicle in vehs)
			{
				if ((Game.Player.Character.IsInVehicle()) && (vehicle.Handle == Game.Player.Character.CurrentVehicle.Handle)) continue; //you own veh
				if (vehicle.IsOccluded) continue;

				var headOffest = vehicle.Position;
				Vector2 pedScreenCoords;
				if (WorldToScreenRel(headOffest, out pedScreenCoords))
				{
					var dist = (screenCoords - pedScreenCoords).Length();
					if (dist < mindist) 
					{
						mindist = dist;
						foundVeh = vehicle;
					}
				}
			}
			return mindist < searchRange ? foundVeh : null;
		}

		public static bool WorldToScreenRel(Vector3 worldCoords, out Vector2 screenCoords)
		{
		    var num1 = new OutputArgument();
            var num2 = new OutputArgument();
            if (!Function.Call<bool>(Hash._WORLD3D_TO_SCREEN2D, worldCoords.X, worldCoords.Y, worldCoords.Z, num1, num2))
			{
				screenCoords = new Vector2();
				return false;
			}
			screenCoords = new Vector2((num1.GetResult<float>() - 0.5f) * 2, (num2.GetResult<float>() - 0.5f) * 2);
			return true;
		}

		public static Vector3 ScreenRelToWorld(Vector3 camPos, Vector3 camRot, Vector2 coord)
		{			
			var camForward = RotationToDirection(camRot);
			var rotUp = camRot + new Vector3(10, 0, 0);
			var rotDown = camRot + new Vector3(-10, 0, 0);
			var rotLeft = camRot + new Vector3(0, 0, -10);
			var rotRight = camRot + new Vector3(0, 0, 10);

			var camRight = RotationToDirection(rotRight) - RotationToDirection(rotLeft);
			var camUp = RotationToDirection(rotUp) - RotationToDirection(rotDown);

			var rollRad = -DegToRad(camRot.Y);

			var camRightRoll = camRight * (float)Math.Cos(rollRad) - camUp * (float)Math.Sin(rollRad);
			var camUpRoll = camRight * (float)Math.Sin(rollRad) + camUp * (float)Math.Cos(rollRad);

			var point3D = camPos + camForward * 10.0f + camRightRoll + camUpRoll;
			Vector2 point2D;
			if (!WorldToScreenRel(point3D, out point2D)) return camPos + camForward * 10.0f;
			var point3DZero = camPos + camForward * 10.0f;
			Vector2 point2DZero;
			if (!WorldToScreenRel(point3DZero, out point2DZero)) return camPos + camForward * 10.0f;

			const double eps = 0.001;
			if (Math.Abs(point2D.X - point2DZero.X) < eps || Math.Abs(point2D.Y - point2DZero.Y) < eps) return camPos + camForward * 10.0f;
			var scaleX = (coord.X - point2DZero.X) / (point2D.X - point2DZero.X);
			var scaleY = (coord.Y - point2DZero.Y) / (point2D.Y - point2DZero.Y);
			var point3Dret = camPos + camForward * 10.0f + camRightRoll * scaleX + camUpRoll * scaleY;
			return point3Dret;
		}

		public static RaycastResult Raycast(Vector3 source, Vector3 target, int options, Entity entity)
		{
			var inputArgumentArray = new InputArgument[9];
			var inputArgument1 = new InputArgument(source.X);
			inputArgumentArray[0] = inputArgument1;
			var inputArgument2 = new InputArgument(source.Y);
			inputArgumentArray[1] = inputArgument2;
			var inputArgument3 = new InputArgument(source.Z);
			inputArgumentArray[2] = inputArgument3;
			var inputArgument4 = new InputArgument(target.X);
			inputArgumentArray[3] = inputArgument4;
			var inputArgument5 = new InputArgument(target.Y);
			inputArgumentArray[4] = inputArgument5;
			var inputArgument6 = new InputArgument(target.Z);
			inputArgumentArray[5] = inputArgument6;
			var inputArgument7 = new InputArgument(options);
			inputArgumentArray[6] = inputArgument7;
			var inputArgument8 = new InputArgument((entity != null) ? entity.Handle : 0);
			inputArgumentArray[7] = inputArgument8;
			var inputArgument9 = new InputArgument(7);
			inputArgumentArray[8] = inputArgument9;
			var result = (Function.Call<int>(Hash._CAST_RAY_POINT_TO_POINT, inputArgumentArray));
			var obj = (RaycastResult) typeof(RaycastResult).GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				null, Type.EmptyTypes, null).Invoke(new[] {(object)result});
			return obj;
		}

		public static Vector3 RotationToDirection(Vector3 rotation)
		{
			var z = DegToRad(rotation.Z);
			var x = DegToRad(rotation.X);
			var num = Math.Abs(Math.Cos(x));
			return new Vector3
			{
				X = (float)(-Math.Sin(z) * num),
				Y = (float)(Math.Cos(z) * num),
				Z = (float)Math.Sin(x)
			};
		}

		public static Vector3 DirectionToRotation(Vector3 direction)
		{
			direction.Normalize();

			var x = Math.Atan2(direction.Z, direction.Y);
			var y = 0;
			var z = -Math.Atan2(direction.X, direction.Y);

			return new Vector3
			{
				X = (float)RadToDeg(x),
				Y = (float)RadToDeg(y),
				Z = (float)RadToDeg(z)
			};
		}

		public static double DegToRad(double deg)
		{
			return deg * Math.PI / 180.0;
		}

		public static double RadToDeg(double deg)
		{
			return deg * 180.0 / Math.PI;
		}

		public static double BoundRotationDeg(double angleDeg)
		{
			var twoPi = (int)(angleDeg / 360);
			var res = angleDeg - twoPi * 360;
			if (res < 0) res += 360;
			return res;
		}
	}
}