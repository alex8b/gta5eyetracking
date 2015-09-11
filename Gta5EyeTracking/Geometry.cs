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
            return Util.GetFollowPedCamViewMode() == 4;
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

		public static Vector3 RaycastEverything(Vector2 screenCoord, out Entity hitEntity, bool skipProjection)
		{
		    hitEntity = null;
			var camPos = GameplayCamera.Position;
			var camRot = GameplayCamera.Rotation;
			const float raycastToDist = 200.0f;
			const float raycastFromDist = 1f;

            //var target3D = ScreenRelToWorld(screenCoord);
            var target3D = ScreenRelToWorld(new Vector2(0.0f,0.0f));
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
			    if (raycastResults.DitHitEntity)
			    {
			        hitEntity = raycastResults.HitEntity;
			    }
				return raycastResults.HitCoords;
			}

			return camPos + dir * raycastToDist;
		}

        public static Vector3 ConecastPedsAndVehicles(Vector2 screenCoords, out  Entity hitEntity)
        {
            var numPoints = 5;
            var angleStep = Math.PI * 0.5;
            var distStep = 0.01;
            var resultCoord = new Vector3();
            hitEntity = null;
            for (var i = 0; i < numPoints; i++)
            {
                var angle = i * angleStep;
                var dist = i * distStep;
                var offsetX = Math.Sin(angle) * dist;
                var offsetY = Math.Cos(angle) * dist;
                var coord = screenCoords + new Vector2((float)offsetX, (float)offsetY);
                Entity entity;
                var hitcoord = RaycastEverything(coord, out entity, i!=0);
                if (i == 0)
                {
                    resultCoord = hitcoord;
                }

                if ((entity != null)
                    && ((Util.IsEntityAPed(entity)
                            && entity.Handle != Game.Player.Character.Handle)
                        || (Util.IsEntityAVehicle(entity)
                            && !(Game.Player.Character.IsInVehicle()
                                && entity.Handle == Game.Player.Character.CurrentVehicle.Handle))))
                {
                    hitEntity = entity;
                    resultCoord = hitcoord;
                    break;
                }
            }
            return resultCoord;
        }

		public static Ped SearchPed(Vector2 screenCoords)
		{
			const double searchRange = 0.1;
			var peds = World.GetNearbyPeds(Game.Player.Character.Position, 200);
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

		public static Vehicle SearchVehicle(Vector2 screenCoords)
		{
			const double searchRange = 0.1;
			var vehs = World.GetNearbyVehicles(Game.Player.Character.Position, 200);
			var mindist = Double.MaxValue;
			Vehicle foundVeh = null;
			foreach (var vehicle in vehs)
			{
				if ((Game.Player.Character.IsInVehicle()) && (vehicle.Handle == Game.Player.Character.CurrentVehicle.Handle)) continue; //you own veh
				//if (vehicle.IsOccluded) continue;
				var vehOffset = vehicle.Position;
				Vector2 vehScreenCoords;
			    
                if (!WorldToScreenRel(vehOffset, out vehScreenCoords)) continue;
			    
                var dist = (screenCoords - vehScreenCoords).Length();
			    if (!(dist < mindist)) continue;
			    mindist = dist;
			    foundVeh = vehicle;
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

        public static bool WorldToScreenRel2(Vector3 entityPosition, out Vector2 screenCoords)
        {
            var mView = Util.GetCameraMatrix();

            var vForward = mView.Row4;
            var vRight = mView.Row2;
            var vUpward = mView.Row3;

            var result = new SharpDX.Vector3(0,0,0);
            result.Z = (vForward.X * entityPosition.X) + (vForward.Y * entityPosition.Y) + (vForward.Z * entityPosition.Z) + vForward.W;
            result.X = (vRight.X * entityPosition.X) + (vRight.Y * entityPosition.Y) + (vRight.Z * entityPosition.Z) + vRight.W;
            result.Y = (vUpward.X * entityPosition.X) + (vUpward.Y * entityPosition.Y) + (vUpward.Z * entityPosition.Z) + vUpward.W;
            if (result.Z < 0.001f)
            {
                screenCoords = new Vector2(0, 0);
                return false;
            }

            float invw = 1.0f / result.Z;
            result.X *= invw;
            result.Y *= invw;
            screenCoords = new Vector2(result.X, result.Y);
            return true;
        }

        public static Vector3 ScreenRelToWorld(Vector2 screenCoordsRel)
	    {
            var mView = Util.GetCameraMatrix();


            var camMat = mView;
            var screenPointVector = new SharpDX.Matrix(screenCoordsRel.X, screenCoordsRel.Y, 1, 0,
                                                       0, 0, 0, 0,
                                                       0, 0, 0, 0,
                                                       0, 0, 0, 0);


		    var epsilon = 0.00001;
            UI.ShowSubtitle("Cam: " + Math.Round(camMat.M11, 1) + " " + Math.Round(camMat.M12, 1) + " " + Math.Round(camMat.M13, 1) + " " + Math.Round(camMat.M14, 1)
             + "\n | " + Math.Round(camMat.M21, 1) + " " + Math.Round(camMat.M22, 1) + " " + Math.Round(camMat.M23, 1) + " " + Math.Round(camMat.M24, 1)
             + "\n | " + Math.Round(camMat.M31, 1) + " " + Math.Round(camMat.M32, 1) + " " + Math.Round(camMat.M33, 1) + " " + Math.Round(camMat.M34, 1)
             + "\n | " + Math.Round(camMat.M41, 1) + " " + Math.Round(camMat.M42, 1) + " " + Math.Round(camMat.M43, 1) + " " + Math.Round(camMat.M44, 1));
		    if (Math.Abs(camMat.Determinant()) > epsilon)
		    {
		        var camMatInvert = camMat;
                camMatInvert.Invert();
		        var worldPointVector = screenPointVector * camMatInvert;
                var result = new Vector3(worldPointVector.M11, worldPointVector.M12, worldPointVector.M13);
                
		        return result;
		    }
            return new Vector3(0,0,0);
	    }

		public static RaycastResult Raycast(Vector3 source, Vector3 target, int options, Entity entity)
		{
            var result = Function.Call<int>(Hash._CAST_RAY_POINT_TO_POINT, source.X, source.Y, source.Z, target.X, target.Y, target.Z, options,
                (entity != null) ? entity.Handle : 0, 7);
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