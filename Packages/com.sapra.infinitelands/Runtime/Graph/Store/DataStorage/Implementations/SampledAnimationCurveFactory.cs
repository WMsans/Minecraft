using UnityEngine;

namespace sapra.InfiniteLands{
    public struct SampledAnimationCurveFactory : IFactory<SampledAnimationCurve>
    {
        public AnimationCurve animationCurve;
        public bool Normalized;
        public SampledAnimationCurveFactory(AnimationCurve curve, bool Normalized)
        {
            this.animationCurve = curve;
            this.Normalized = Normalized;
        }
        public SampledAnimationCurve Create()
        {
            return new SampledAnimationCurve(animationCurve, Normalized);
        }
    }
}