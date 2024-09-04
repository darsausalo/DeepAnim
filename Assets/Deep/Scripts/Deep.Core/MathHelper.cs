using System.Runtime.CompilerServices;
using UnityEngine;

namespace AurigaGames.Deep.Core
{
    public static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRangePct(float minValue, float maxValue, float value)
        {
            var divisor = maxValue - minValue;
            if (Mathf.Approximately(divisor, 0f))
            {
                return value >= maxValue ? 1f : 0f;
            }

            return (value - minValue) / divisor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetMappedRangeValueClamped(float inputMinValue, float inputMaxValue,
            float outputMinValue, float outputMaxValue, float value)
        {
            var pct = Mathf.Clamp01(GetRangePct(inputMinValue, inputMaxValue, value));
            return Mathf.Lerp(outputMinValue, outputMaxValue, pct);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetMappedRangeValueUnclamped(float inputMinValue, float inputMaxValue,
            float outputMinValue, float outputMaxValue, float value)
        {
            var pct = GetRangePct(inputMinValue, inputMaxValue, value);
            return Mathf.LerpUnclamped(outputMinValue, outputMaxValue, pct);
        }
    }
}