using System;
using UnityEngine;
using static UnityEngine.Mathf;

namespace TinyGiantStudio.BetterInspector
{
    public static class BetterMath
    {
        public static Vector3 RoundedVector3(Vector3 vector3) =>
            new(RoundedFloat(vector3.x), RoundedFloat(vector3.y), RoundedFloat(vector3.z));

        static float RoundedFloat(float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f))
                return f;

            if (Approximately(f, Round(f)))
                return Round(f);

            return (float)Math.Round(f, BetterTransformSettings.instance.FieldRoundingAmount);
        }

        /// <summary>
        ///     Sanitizes floating point precision errors by rounding and returning the cleaner value only when the difference is
        ///     insignificant.
        /// </summary>
        /// <param name="vector3"></param>
        /// <returns></returns>
        public static Vector3 TrimVectorNoise(Vector3 vector3)
        {
            return new(TrimFloatNoise(vector3.x), TrimFloatNoise(vector3.y),
                TrimFloatNoise(vector3.z));
        }

        public static string Vector3ToCopyableString(Vector3 vector3)
        {
            return TrimVectorNoise(vector3).ToString("R");
        }

        /// <summary>
        ///     Sanitizes floating point precision errors by rounding and returning the cleaner value only when the difference is
        ///     insignificant.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        static float TrimFloatNoise(float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f))
                return f;

            // Snap to integer if nearly whole
            float roundedInt = Round(f);
            if (Approximately(f, roundedInt))
                return roundedInt;

            // Snap to defined decimal precision if close enough
            float rounded = (float)Math.Round(f, BetterTransformSettings.instance.FieldRoundingAmount);
            return Approximately(f, rounded) ? rounded : f;
        }

        public static Vector3 Multiply(Vector3 first, Vector3 second)
        {
            return new(NanFixed(first.x * second.x), NanFixed(first.y * second.y),
                NanFixed(first.z * second.z));
        }

        public static Vector3 Divide(Vector3 first, Vector3 second)
        {
            return new(NanFixed(first.x / second.x),
                NanFixed(first.y / second.y), NanFixed(first.z / second.z));
        }

        static float NanFixed(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1;

            return value;
        }

        public static bool IsInfinity(Vector3 vector3)
        {
            if (float.IsPositiveInfinity(vector3.x))
                return true;
            if (float.IsPositiveInfinity(vector3.y))
                return true;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (float.IsPositiveInfinity(vector3.z))
                return true;

            return false;
        }
    }
}