using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    public struct SampledAnimationCurve : IDisposableJob
    {
        public static SampledAnimationCurve Default =>
            new SampledAnimationCurve(new AnimationCurve(), true);

        [NativeDisableParallelForRestriction]
        private NativeArray<float> FunctionModifier;

        /// <param name="samples">Must be 2 or higher</param>
        private readonly static int samples = 255;
        public readonly float timeFrom;
        public readonly float timeTo;
        private readonly bool normalized;

        public SampledAnimationCurve(AnimationCurve ac, bool normalized)
        {
            FunctionModifier = new NativeArray<float>(samples, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            timeFrom = 0f;
            timeTo = 1f;
            this.normalized = normalized;
            if (!normalized)
            {
                timeFrom = ac.keys[0].time;
                timeTo = ac.keys[ac.keys.Length - 1].time;
            }

            float timeStep = (timeTo - timeFrom) / (samples - 1);

            for (int i = 0; i < samples; i++)
            {
                var value = ac.Evaluate(timeFrom + (i * timeStep));
                if (normalized)
                    value = math.saturate(value);
                FunctionModifier[i] = value;
            }
        }

        public void Dispose(JobHandle dependancy)
        {
            if (FunctionModifier.IsCreated)
                FunctionModifier.Dispose(dependancy);
        }

        public bool IsCreated => FunctionModifier.IsCreated;

        public float EvaluateLerp(float time, float ogFrom, float ogTo, float finalFrom, float finalTo)
        {
            if (normalized)
                time = JobExtensions.invLerp(ogFrom, ogTo, time);
            else
            {
                if (time < timeFrom || time > timeTo)
                    return time;
                time = JobExtensions.invLerp(timeFrom, timeTo, time);
            }
            float value = getValue(FunctionModifier, time, samples);

            if (normalized)
                value = math.lerp(finalFrom, finalTo, value);
            return value;
        }

        static float getValue(NativeArray<float> func, float value, int samples)
        {
            int len = samples - 1;

            float floatIndex = math.saturate(value) * len;
            int floorIndex = (int)floatIndex;
            int ceilIndex = math.clamp(floorIndex + 1, 0, len);

            float lowerValue = func[floorIndex];
            float higherValue = func[ceilIndex];

            return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
        }

        public enum CurveMode { Normalized, Global }

        public static SampledAnimationCurveFactory GetFactory(CurveMode curveMode, AnimationCurve globalCurve, AnimationCurve function)
        {
            switch (curveMode)
            {
                case CurveMode.Global:
                    return new SampledAnimationCurveFactory(globalCurve, false);
                default:
                    return new SampledAnimationCurveFactory(function, true);

            }
        }
    }
}