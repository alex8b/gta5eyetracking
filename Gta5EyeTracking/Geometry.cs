using System;
using GTA;
using GTA.Native;
using SharpDX;
using Vector2 = GTA.Math.Vector2;
using Vector3 = GTA.Math.Vector3;

namespace Gta5EyeTracking
{
    public static class Geometry
    {
        public static bool IsInFrontOfThePlayer(Vector3 shootCoord)
        {
            //TODO
            return true;

            //var playerRotation = Game.Player.Character.Rotation;
            //var shootVector = shootCoord - Game.Player.Character.Position;
            //var shootRotation = Util.DirectionToRotation(shootVector);
            //var rotationDiff = shootRotation - playerRotation;
            //var rotationDiffBound = BoundRotationDeg(rotationDiff.Z);
            //return ((rotationDiffBound < 90) || (rotationDiffBound > 270));
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord, out Entity hitEntity, float radius, float ignoreRange)
        {
            Vector3 source3D;
            Vector3 target3D;
            ScreenRelToWorld(screenCoord, out source3D, out target3D);
            return RaycastEverything(out hitEntity, target3D, source3D, radius, ignoreRange);
        }

        public static Vector3 RaycastEverything(out Entity hitEntity, Vector3 target3D, Vector3 source3D, float radius, float ignoreRange)
        {
            hitEntity = null;
            const float raycastToDist = 200.0f;
            float raycastFromDist = Mathf.Max(1f, ignoreRange);
            const float defaultDist = 60.0f;
            Entity ignoreEntity = Game.Player.Character;
            if (Game.Player.Character.IsInVehicle())
            {
                ignoreEntity = Game.Player.Character.CurrentVehicle;
            }

            var dir = (target3D - source3D);
            dir.Normalize();
            
            RaycastResult raycastResults;
            if (radius > 0)
            {
                raycastResults = World.RaycastCapsule(source3D + dir * raycastFromDist,
                    source3D + dir * raycastToDist,
                    radius,
                    (IntersectFlags)(1 | 16 | 256 | 2 | 4 | 8) // | peds + vehicles
                    , ignoreEntity);
            }
            else
            {
                raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                    source3D + dir * raycastToDist,
                    (IntersectFlags)(1 | 16 | 256 | 2 | 4 | 8) // | peds + vehicles
                    , ignoreEntity);
            }


            if (raycastResults.DidHit)
            {
                if (raycastResults.HitEntity != null)
                {
                    hitEntity = raycastResults.HitEntity;
                }
                return raycastResults.HitPosition;
            }

            return source3D + dir * defaultDist;
        }

        public static Vector3 ConecastPedsAndVehicles(Vector2 screenCoords, out Entity hitEntity, float ignoreRange)
        {
            var radius = 1;
            var numPoints = 5;
            var angleStep = Math.PI * 0.2;
            var distStep = 0.05 / numPoints;
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
                var hitcoord = RaycastEverything(coord, out entity, radius, ignoreRange);
                if (i == 0)
                {
                    resultCoord = hitcoord;
                }

                if ((entity != null)
                    && ((ScriptHookExtensions.IsEntityAPed(entity)
                            && entity.Handle != Game.Player.Character.Handle)
                        || (ScriptHookExtensions.IsEntityAVehicle(entity)
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

        //public static bool IsOccluded(Entity entity, Vector3 target3D)
        //{
        //	const float raycastToDist = 200.0f;
        //	const float raycastFromDist = 1f;

        //	var mView = ScriptHookExtensions.GetCameraMatrix();
        //	var source3D = ViewMatrixToCameraPosition(mView);

        //	Entity ignoreEntity = Game.Player.Character;
        //	if (Game.Player.Character.IsInVehicle())
        //	{
        //		ignoreEntity = Game.Player.Character.CurrentVehicle;
        //	}

        //	var dir = (target3D - source3D);
        //	dir.Normalize();
        //	var raycastResults = World.Raycast(source3D + dir*raycastFromDist,
        //		source3D + dir*raycastToDist,
        //		(IntersectOptions) (1 | 16 | 256 | 2 | 4 | 8) // | peds + vehicles
        //		, ignoreEntity);


        //	if (raycastResults.DitHitEntity)
        //	{
        //		return raycastResults.HitEntity.Handle != entity.Handle;
        //	}
        //	return true;
        //}

        //public static Ped SearchPed(Vector2 screenCoords)
        //{
        //	const double searchRange = 0.1;
        //	const double thresholdRange = 0.025;
        //	const float raycastToDist = 200.0f;
        //	var peds = World.GetNearbyPeds(Game.Player.Character.Position, raycastToDist);
        //	var mindist = Double.MaxValue;
        //	Ped foundPed = null;
        //	//Util.Log("Peds - " + peds.Length);
        //	//Util.Log("P - " + DateTime.UtcNow.Ticks);
        //	foreach (var ped in peds)
        //	{
        //		if (ped.Handle == Game.Player.Character.Handle) continue;
        //              if (!ped.IsAlive) continue;
        //              //if (ped.IsOccluded) continue; slow?

        //              {
        //			var headOffest = ped.GetBoneCoord(Bone.SKEL_ROOT);
        //			Vector2 pedScreenCoords;
        //			if (WorldToScreenRel(headOffest, out pedScreenCoords))
        //			{
        //				var dist = (screenCoords - pedScreenCoords).Length();
        //				if ((dist < mindist) && (dist < searchRange))
        //				{
        //					if (ped.IsOccluded) continue;
        //					//if (IsOccluded(ped, headOffest)) continue;
        //					mindist = dist;
        //					foundPed = ped;
        //				}
        //				if (dist < thresholdRange)
        //				{
        //					break;
        //				}
        //			}
        //		}

        //		{
        //			var headOffest = ped.GetBoneCoord(Bone.SKEL_Head);
        //			Vector2 pedScreenCoords;

        //			if (!WorldToScreenRel(headOffest, out pedScreenCoords)) continue;

        //			var dist = (screenCoords - pedScreenCoords).Length();

        //			if (!(dist < mindist) || !(dist < searchRange)) continue;
        //			if (ped.IsOccluded) continue;
        //			//if (IsOccluded(ped, headOffest)) continue;
        //			mindist = dist;
        //			foundPed = ped;
        //			if (dist < thresholdRange)
        //			{
        //				break;
        //			}
        //		}

        //	}
        //	//Util.Log("Q - " + DateTime.UtcNow.Ticks);
        //	return foundPed;
        //}

        //public static Vehicle SearchVehicle(Vector2 screenCoords)
        //{
        //	const double searchRange = 0.1;
        //	const double thresholdRange = 0.025;
        //	const float raycastToDist = 200.0f;
        //	var vehs = World.GetNearbyVehicles(Game.Player.Character.Position, raycastToDist);
        //	var mindist = Double.MaxValue;
        //	Vehicle foundVeh = null;
        //	//Util.Log("Vehs - " + vehs.Length);
        //	//Util.Log("V - " + DateTime.UtcNow.Ticks);
        //	foreach (var vehicle in vehs)
        //	{
        //		if (Game.Player.Character.IsInVehicle() && (vehicle.Handle == Game.Player.Character.CurrentVehicle.Handle)) continue; //you own veh
        //	    if (!vehicle.IsAlive) continue;

        //		var vehOffset = vehicle.Position;
        //		Vector2 vehScreenCoords;

        //              if (!WorldToScreenRel(vehOffset, out vehScreenCoords)) continue;

        //              var dist = (screenCoords - vehScreenCoords).Length();
        //	    if (!(dist < mindist) || !(dist < searchRange)) continue;
        //		if (vehicle.IsOccluded) continue;
        //		//if (IsOccluded(vehicle, vehOffset)) continue;
        //		mindist = dist;
        //	    foundVeh = vehicle;
        //		if (dist < thresholdRange)
        //		{
        //			break;
        //		}
        //	}
        //	//Util.Log("W - " + DateTime.UtcNow.Ticks);

        //	return foundVeh;
        //}


        public static bool WorldToScreenRel(Vector3 entityPosition, out Vector2 screenCoords)
        {
            var mView = CameraHelper.GetCameraMatrix();
            mView.Transpose();

            var vForward = mView.Row4;
            var vRight = mView.Row2;
            var vUpward = mView.Row3;

            var result = new SharpDX.Vector3(0, 0, 0);
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

        private static Vector3 ViewMatrixToCameraPosition(Matrix mView)
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

            var epsilon = 0.0000001;
            if (Math.Abs(denom) < epsilon) return new Vector3();

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
            return Math.Abs(det) < epsilon ? new Vector3() : new Vector3(detx / det, dety / det, detz / det);
        }

        public static void ScreenRelToWorld(Vector2 screenCoordsRel, out Vector3 camPoint, out Vector3 farPoint)
        {
            var mView = CameraHelper.GetCameraMatrix();

            camPoint = ViewMatrixToCameraPosition(mView);
            farPoint = ScreenRelToWorld(mView, screenCoordsRel);
        }

        //public static RaycastResult Raycast(Vector3 source, Vector3 target, int options, Entity entity)
        //{
        //          var result = Function.Call<int>(Hash._CAST_RAY_POINT_TO_POINT, source.X, source.Y, source.Z, target.X, target.Y, target.Z, options,
        //              (entity != null) ? entity.Handle : 0, 7);
        //	var obj = (RaycastResult) typeof(RaycastResult).GetConstructor(
        //		BindingFlags.NonPublic | BindingFlags.Instance,
        //		null, Type.EmptyTypes, null).Invoke(new[] {(object)result});
        //	return obj;
        //}

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            var z = Mathf.Deg2Rad * rotation.Z;
            var x = Mathf.Deg2Rad * rotation.X;
            var num = Math.Abs(Math.Cos(x));
            var dir = new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };

            return dir.Normalized;
        }

        public static GTA.Math.Quaternion GtaRotationToQuaternion(Vector3 euler)
        {
            Vector3 rotVec = Mathf.Deg2Rad * euler;

            GTA.Math.Quaternion xRot = GTA.Math.Quaternion.RotationAxis(new Vector3(1.0f, 0.0f, 0.0f), rotVec.X);
            GTA.Math.Quaternion yRot = GTA.Math.Quaternion.RotationAxis(new Vector3(0.0f, 1.0f, 0.0f), rotVec.Y);
            GTA.Math.Quaternion zRot = GTA.Math.Quaternion.RotationAxis(new Vector3(0.0f, 0.0f, 1.0f), rotVec.Z);

            GTA.Math.Quaternion rot = zRot * yRot * xRot;
            return rot;
        }

        public static Vector3 QuaternionToGtaRotation(GTA.Math.Quaternion q)
        {
            float r11 = -2 * (q.X * q.Y - q.W * q.Z);
            float r12 = q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z;
            float r21 = 2 * (q.Y * q.Z + q.W * q.X);
            float r31 = -2 * (q.X * q.Z - q.W * q.Y);
            float r32 = q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z;

            float ax = (float)Math.Asin(r21);
            float ay = (float)Math.Atan2(r31, r32);
            float az = (float)Math.Atan2(r11, r12);

            const float f = 360.0f / 2.0f / 3.1415926535897f;
            ax *= f;
            ay *= f;
            az *= f;

            Vector3 ret = new Vector3(ax, ay, az);

            return ret;
        }

        public static Vector3 DirectionToRotation(Vector3 direction)
        {
            direction.Normalize();

            var x = (float)Math.Atan2(direction.Z, Math.Sqrt(direction.Y * direction.Y + direction.X * direction.X));
            var y = 0;
            var z = (float)-Math.Atan2(direction.X, direction.Y);

            return new Vector3
            {
                X = Mathf.Rad2Deg * x,
                Y = Mathf.Rad2Deg * y,
                Z = Mathf.Rad2Deg * z
            };
        }

        public static float BoundRotationDeg(float angleDeg)
        {
            while (angleDeg > 180)
            {
                angleDeg -= 360;
            }
            while (angleDeg < -180)
            {
                angleDeg += 360;
            }
            return angleDeg;
        }


    }
}