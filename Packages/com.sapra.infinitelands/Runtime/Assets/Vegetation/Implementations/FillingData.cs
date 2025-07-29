using System.Collections.Generic;
using static sapra.InfiniteLands.IHoldVegetation;

namespace sapra.InfiniteLands{
    public readonly struct FillingData{
        public readonly bool lodCrossFade;
        public readonly ColorSamplingMode colorSamplingMode;
        public readonly float samplingRandomness;
        public readonly List<TextureAsset> removeAtTextures;
        public FillingData(bool lodCrossFade, ColorSamplingMode colorSamplingMode, float samplingRandomness, List<TextureAsset> removeAtTextures)
        {
            this.lodCrossFade = lodCrossFade;
            this.colorSamplingMode = colorSamplingMode;
            this.samplingRandomness = samplingRandomness;
            this.removeAtTextures = removeAtTextures;
        }
    }
}