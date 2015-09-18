using System;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using GTA;
using GTA.Native;
using SharpDX;
using Tobii.EyeX.Client.Interop;
using Vector2 = GTA.Math.Vector2;
using Vector3 = GTA.Math.Vector3;

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
			const float raycastToDist = 200.0f;
			const float raycastFromDist = 1f;
		    const float defaultDist = 60.0f;

		    Vector3 source3D;
		    Vector3 target3D;
            ScreenRelToWorld(screenCoord, out source3D, out target3D);

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

			return source3D + dir * defaultDist;
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

		public static bool WorldToScreenRel_Native(Vector3 worldCoords, out Vector2 screenCoords)
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

        public static bool WorldToScreenRel(Vector3 entityPosition, out Vector2 screenCoords)
        {
            var mView = Util.GetCameraMatrix();
            mView.Transpose();

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
            screenCoords = new Vector2(result.X, -result.Y);
            return true;
        }

        private static Vector3 ViewMatrixToCamPos(Matrix mView)
        {
            mView.Transpose();

            var vForward = mView.Row4;
            var vRight = mView.Row2;
            var vUpward = mView.Row3;

            var n1 = new Vector3(vForward.X, vForward.Y, vForward.Z);
            var n2 = new Vector3(vRight.X, vRight.Y, vRight.Z);
            var n3 = new Vector3(vUpward.X, vUpward.Y, vUpward.Z);

            var d1 = vForward.W;
            var d2 = vRight.W;
            var d3 = vUpward.W;

            var n2n3 = Vector3.Cross(n2, n3);
            var n3n1 = Vector3.Cross(n3, n1);
            var n1n2 = Vector3.Cross(n1, n2);

            var top = (n2n3 * d1) + (n3n1 * d2) + (n1n2 * d3);
            var denom = Vector3.Dot(n1, n2n3);

            return top / -denom;
        }

        private static Vector3 ScreenRelToWorld(Matrix mView, Vector2 screenCoordsRel)
	    {
            mView.Transpose();

            var vForward = mView.Row4;
            var vRight = mView.Row2;
            var vUpward = mView.Row3;

            var d = 1 - vForward.W;
            var h = screenCoordsRel.X - vRight.W;
            var s = -screenCoordsRel.Y - vUpward.W;

            var m = new Matrix(vForward.X, vForward.Y, vForward.Z, 0,
                vRight.X, vRight.Y, vRight.Z, 0,
                vUpward.X, vUpward.Y, vUpward.Z, 0,
                0, 0, 0, 1);
            var det = m.Determinant();

            var mx = new Matrix(d, vForward.Y, vForward.Z, 0,
                h, vRight.Y, vRight.Z, 0,
                s, vUpward.Y, vUpward.Z, 0,
                0, 0, 0, 1);
            var detx = mx.Determinant();

            var my = new Matrix(vForward.X, d, vForward.Z, 0,
                vRight.X, h, vRight.Z, 0,
                vUpward.X, s, vUpward.Z, 0,
                0, 0, 0, 1);
            var dety = my.Determinant();

            var mz = new Matrix(vForward.X, vForward.Y, d, 0,
                vRight.X, vRight.Y, h, 0,
                vUpward.X, vUpward.Y, s, 0,
                0, 0, 0, 1);
            var detz = mz.Determinant();

            var epsilon = 0.0000001;
            if (!(Math.Abs(d) > epsilon))
            {
                return new Vector3();
            }

            return new Vector3(detx / det, dety / det, detz / det);
        }
        public static void ScreenRelToWorld(Vector2 screenCoordsRel, out Vector3 camPoint, out Vector3 farPoint)
	    {
            var mView = Util.GetCameraMatrix();

            camPoint = ViewMatrixToCamPos(mView);
            farPoint = ScreenRelToWorld(mView, screenCoordsRel);

            //UI.ShowSubtitle("Cam: " + Math.Round(camPoint.X, 1) + " " + Math.Round(camPoint.Y, 1) + " " +
            //                Math.Round(camPoint.Z, 1)
            //                + "\nResult: " + Math.Round(farPoint.X, 1) + " " + Math.Round(farPoint.Y, 1) + " " +
            //                Math.Round(farPoint.Z, 1));

//+ "\n Cam: " + Math.Round(mView.M11, 1) + " " + Math.Round(mView.M12, 1) + " " + Math.Round(mView.M13, 1) + " " + Math.Round(mView.M14, 1)
//+ "\n " + Math.Round(mView.M21, 1) + " " + Math.Round(mView.M22, 1) + " " + Math.Round(mView.M23, 1) + " " + Math.Round(mView.M24, 1)
//+ "\n " + Math.Round(mView.M31, 1) + " " + Math.Round(mView.M32, 1) + " " + Math.Round(mView.M33, 1) + " " + Math.Round(mView.M34, 1)
//+ "\n " + Math.Round(mView.M41, 1) + " " + Math.Round(mView.M42, 1) + " " + Math.Round(mView.M43, 1) + " " + Math.Round(mView.M44, 1));
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