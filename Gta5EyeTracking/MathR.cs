using System;
using GTA.Math;

namespace Gta5EyeTracking
{
    public static class MathR
    {
        public const float PI = 3.1415926535897932f;

        public static float Sqrt(float f)
        {
            return (float)Math.Sqrt((double)f);
        }

        public static float Sign(float f)
        {
            return (double)f >= 0.0 ? 1f : -1f;
        }

        public static float Atan2(float f, float t)
        {
            return (float)Math.Atan2((double)f, (double)t);
        }

        public static float Magnitude(this Vector3 vector)
        {
            return (vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        }

        public static Vector3 ToEulerAngles(this Quaternion qt)
        {
            var vector = new Vector3();

            double sqw = qt.W * qt.W;
            double sqx = qt.X * qt.X;
            double sqy = qt.Y * qt.Y;
            double sqz = qt.Z * qt.Z;

            vector.X = (float)Math.Asin(2f * (qt.X * qt.Z - qt.W * qt.Y));                             // Pitch 
            vector.Y = (float)Math.Atan2(2f * qt.X * qt.W + 2f * qt.Y * qt.Z, 1 - 2f * (sqz + sqw));     // Yaw 
            vector.Z = (float)Math.Atan2(2f * qt.X * qt.Y + 2f * qt.Z * qt.W, 1 - 2f * (sqy + sqz)); // Roll

            return vector;
        }

        public static Quaternion QuaternionFromEuler(float X, float Y, float Z)
        {
            float rollOver2 = Z * 0.5f;
            float sinRollOver2 = (float)Math.Sin((double)rollOver2);
            float cosRollOver2 = (float)Math.Cos((double)rollOver2);
            float pitchOver2 = X * 0.5f;
            float sinPitchOver2 = (float)Math.Sin((double)pitchOver2);
            float cosPitchOver2 = (float)Math.Cos((double)pitchOver2);
            float yawOver2 = Y * 0.5f;
            float sinYawOver2 = (float)Math.Sin((double)yawOver2);
            float cosYawOver2 = (float)Math.Cos((double)yawOver2);
            Quaternion result;
            result.X = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
            result.Y = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
            result.Z = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
            result.W = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
            return result;
        }

        public static Quaternion QuaternionFromEuler(this Vector3 vec)
        {
            return QuaternionFromEuler(vec.X, vec.Y, vec.Z);
        }

        // This method can be BUGGY !
        public static Quaternion QuaternionLookRotation(Vector3 forward, Vector3 up)
        {
            forward = forward.Normalized;

            Vector3 vector = Vector3.Normalize(forward);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
            Vector3 vector3 = Vector3.Cross(vector, vector2);
            var m00 = vector2.X;
            var m01 = vector2.Y;
            var m02 = vector2.Z;
            var m10 = vector3.X;
            var m11 = vector3.Y;
            var m12 = vector3.Z;
            var m20 = vector.X;
            var m21 = vector.Y;
            var m22 = vector.Z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        // This method can be BUGGY !
        private static Quaternion LookRotationInternalY(Vector3 forward, Vector3 up)
        {
            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);
            var m00 = right.X;
            var m01 = right.Y;
            var m02 = right.Z;
            var m10 = up.X;
            var m11 = up.Y;
            var m12 = up.Z;
            var m20 = forward.X;
            var m21 = forward.Y;
            var m22 = forward.Z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;

            return quaternion;
        }

        public static Quaternion QuaternionLookRotation(Vector3 forward)
        {
            Vector3 up = Vector3.WorldUp;

            return QuaternionLookRotation(forward, up);
        }

        // Cheaper than Slerp
        public static Quaternion EulerNlerp(Quaternion start, Quaternion end, float ammount)
        {
            return QuaternionFromEuler(Vector3.Lerp(start.ToEulerAngles(), end.ToEulerAngles(), MathR.Clamp01(ammount)).Normalized);
        }

        public static Quaternion QuatNlerp(Quaternion start, Quaternion end, float ammount, bool shortestPath = true)
        {
            Quaternion result;
                float fCos = Quaternion.Dot(start, end);
		    if (fCos< 0.0f && shortestPath)
		    {
			    result = start + ammount* ((-end) - start);
		    }
		    else
		    {
			    result = start + ammount* (end - start);
		    }
                result.Normalize();
                return result;
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = (float)(-2.0 * (double)t * (double)t * (double)t + 3.0 * (double)t * (double)t);
            return (float)((double)to * (double)t + (double)from * (1.0 - (double)t));
        }

        public static float Clamp01(float value)
        {
            if ((double)value < 0.0)
                return 0.0f;
            if ((double)value > 1.0)
                return 1f;
            return value;
        }

        public static float Lerp(float value1, float value2, float ammount)
        {
            return value1 + (value2 - value1) * MathR.Clamp01(ammount);
        }

        public static float InverseLerp(float from, float to, float value)
        {
            if (from < to)
            {
                if (value < from)
                    return 0.0f;
                else if (value > to)
                    return 1.0f;
            }
            else
            {
                if (value < to)
                    return 1.0f;
                else if (value > from)
                    return 0.0f;
            }
            return (value - from) / (to - from);
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static Vector3 Vector3SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float num1 = 2f / smoothTime;
            float num2 = num1 * deltaTime;
            float num3 = (float)(1.0 / (1.0 + (double)num2 + 0.479999989271164 * (double)num2 * (double)num2 + 0.234999999403954 * (double)num2 * (double)num2 * (double)num2));
            Vector3 vector = current - target;
            Vector3 vector3_1 = target;
            float maxLength = maxSpeed * smoothTime;
            Vector3 vector3_2 = Vector3ClampMagnitude(vector, maxLength);
            target = current - vector3_2;
            Vector3 vector3_3 = (currentVelocity + num1 * vector3_2) * deltaTime;
            currentVelocity = (currentVelocity - num1 * vector3_3) * num3;
            Vector3 vector3_4 = target + (vector3_2 + vector3_3) * num3;
            if ((double)Vector3.Dot(vector3_1 - current, vector3_4 - vector3_1) > 0.0)
            {
                vector3_4 = vector3_1;
                currentVelocity = (vector3_4 - vector3_1) / deltaTime;
            }
            return vector3_4;
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float num1 = 2f / smoothTime;
            float num2 = num1 * deltaTime;
            float num3 = (float)(1.0 / (1.0 + (double)num2 + 0.479999989271164 * (double)num2 * (double)num2 + 0.234999999403954 * (double)num2 * (double)num2 * (double)num2));
            float num4 = current - target;
            float num5 = target;
            float max = maxSpeed * smoothTime;
            float num6 = Clamp(num4, -max, max);
            target = current - num6;
            float num7 = (currentVelocity + num1 * num6) * deltaTime;
            currentVelocity = (currentVelocity - num1 * num7) * num3;
            float num8 = target + (num6 + num7) * num3;
            if ((double)num5 - (double)current > 0.0 == (double)num8 > (double)num5)
            {
                num8 = num5;
                currentVelocity = (num8 - num5) / deltaTime;
            }
            return num8;
        }

        public static float Max(float a, float b)
        {
            if ((double)a > (double)b)
                return a;
            return b;
        }

        public static float Min(float a, float b)
        {
            if ((double)a < (double)b)
                return a;
            return b;
        }

        public static Vector3 Vector3ClampMagnitude(Vector3 vector, float maxLength)
        {
            if ((double)Vector3SqrMagnitude(vector) > (double)maxLength * (double)maxLength)
                return vector.Normalized * maxLength;
            return vector;
        }

        public static float Vector3SqrMagnitude(Vector3 a)
        {
            return (float)((double)a.X * (double)a.X + (double)a.Y * (double)a.Y + (double)a.Z * (double)a.Z);
        }

        public static float LerpAngle(float a, float b, float t)
        {
            float num = MathR.Repeat(b - a, 360f);
            if ((double)num > 180.0)
                num -= 360f;
            return a + num * MathR.Clamp01(t);
        }

        public static float Repeat(float t, float length)
        {
            return t - MathR.Floor(t / length) * length;
        }

        public static float Floor(float f)
        {
            return (float)Math.Floor((double)f);
        }

        /// <summary>
        /// Evaluates a rotation needed to be applied to an object positioned at sourcePoint to face destPoint
        /// </summary>
        /// <param name="sourcePoint">Coordinates of source point</param>
        /// <param name="destPoint">Coordinates of destionation point</param>
        /// <returns></returns>
        public static Quaternion LookAt(Vector3 sourcePoint, Vector3 destPoint)
        {
            Vector3 forwardVector = Vector3.Normalize(destPoint - sourcePoint);

            float dot = Vector3.Dot(Vector3.RelativeFront, forwardVector);

            if (Math.Abs(dot - (-1.0f)) < float.Epsilon)
            {
                return new Quaternion(Vector3.WorldUp.X, Vector3.WorldUp.Y, Vector3.WorldUp.Z, 3.1415926535897932f);
            }
            if (Math.Abs(dot - (1.0f)) < float.Epsilon)
            {
                return Quaternion.Identity;
            }

            float rotAngle = (float)Math.Acos(dot);
            Vector3 rotAxis = Vector3.Cross(Vector3.RelativeFront, forwardVector);
            rotAxis = Vector3.Normalize(rotAxis);
            return CreateFromAxisAngle(rotAxis, rotAngle);
        }

        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            float halfAngle = angle * .5f;
            float s = (float)System.Math.Sin(halfAngle);
            Quaternion q = Quaternion.Identity;
            q.X = axis.X * s;
            q.Y = axis.Y * s;
            q.Z = axis.Z * s;
            q.W = (float)System.Math.Cos(halfAngle);
            return q;
        }

        public static Vector3 OrthoNormalize(Vector3 normal, Vector3 tangent)
        {
            normal = normal.Normalized;

            Vector3 proj = normal * Vector3.Dot(tangent, normal);
            tangent = Vector3.Subtract(tangent, proj);

            return tangent.Normalized;
        }

        public static Matrix Matrix4LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 z = Vector3.Normalize(eye - target);
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
            Vector3 y = Vector3.Normalize(Vector3.Cross(z, x));

            Matrix rot = new Matrix();

            rot.M11 = x.X;
            rot.M12 = y.X;
            rot.M13 = z.X;
            rot.M14 = 0.0f;

            rot.M21 = x.Y;
            rot.M22 = y.Y;
            rot.M23 = z.Y;
            rot.M24 = 0.0f;

            rot.M31 = x.Z;
            rot.M32 = y.Z;
            rot.M33 = z.Z;
            rot.M34 = 0.0f;

            rot.M41 = 0.0f;
            rot.M42 = 0.0f;
            rot.M43 = 0.0f;
            rot.M44 = 1.0f;

            Matrix trans = Matrix.Translation(-eye);

            return trans * rot;
        }

        public static Matrix Matrix4LookAt(Vector3 eye, Vector3 target)
        {
            Vector3 up = Vector3.WorldUp;

            return Matrix4LookAt(eye, target, up);
        }

        public static Quaternion QuaternionFromMatrix(Matrix m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.W = MathR.Sqrt(MathR.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.X = MathR.Sqrt(MathR.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.Y = MathR.Sqrt(MathR.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.Z = MathR.Sqrt(MathR.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.X *= MathR.Sign(q.X * (m[2, 1] - m[1, 2]));
            q.Y *= MathR.Sign(q.Y * (m[0, 2] - m[2, 0]));
            q.Z *= MathR.Sign(q.Z * (m[1, 0] - m[0, 1]));
            return q;
        }

        public static Quaternion XLookRotation(Vector3 direction, Vector3 up)
        {
            Quaternion rightToForward = Quaternion.Euler(0f, 90f, 0f);
            Quaternion forwardToTarget = MathR.LookRotationInternalY(direction, up);

            return forwardToTarget * rightToForward;
        }

        public static Quaternion XLookRotation(Vector3 direction)
        {
            var up = Vector3.WorldUp;

            return XLookRotation(direction, up);
        }
    }
}
