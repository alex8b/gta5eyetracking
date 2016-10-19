using System;

namespace Gta5EyeTracking
{
    public static class Mathf
    {
        public static float Clamp(float val, float min, float max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        public static float Lerp(float from, float to, float delta)
        {
            return from + (to - from)*delta;
        }

        public static float LerpAngle(float from, float to, float delta)
        {
            return Lerp(from, to, delta);
        }
    }
}
