using System;

namespace Gta5EyeTracking
{
    public static class Mathf
    {
        public static float Clamp(float val, float min, float max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0, 1);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float LerpAngle(float a, float b, float t)
        {
            float num = Mathf.Repeat(b - a, 360f);
            if (num > 180.0f)
                num -= 360f;
            return a + num * Clamp01(t);
        }

        public static float Repeat(float t, float length)
        {
            return (float) (t - Math.Floor(t / length) * length);
        }

        public const float Rad2Deg = 57.29578f;

        public const float Deg2Rad = 0.01745329f;
    }
}
