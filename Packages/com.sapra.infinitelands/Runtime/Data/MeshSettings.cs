using System;

using UnityEngine;

namespace sapra.InfiniteLands
{
    [Serializable]
    public struct MeshSettings
    {
        public enum GenerationMode
        {
            RelativeToWorld,
            RelativeToTerrain
        };

        public int Seed;
        [Range(1, 255)] public int Resolution;
        public bool CustomSplatMapResolution;
        [SerializeField][ShowIf(nameof(CustomSplatMapResolution))] private int _textureResolution;
        [Min(100)] public float MeshScale;
        public GenerationMode generationMode;

        public float ScaleToResolution => Resolution / MeshScale;
        public int TextureResolution => CustomSplatMapResolution ? _textureResolution : Resolution;
        public bool SeparatedBranch => CustomSplatMapResolution && TextureResolution != Resolution;
        
        public MeshSettings(MeshSettings og)
        {
            Seed = og.Seed;
            Resolution = og.Resolution;
            CustomSplatMapResolution = og.CustomSplatMapResolution;
            _textureResolution = og._textureResolution;
            MeshScale = og.MeshScale;
            generationMode = og.generationMode;

        }
        public static MeshSettings Default => new MeshSettings
        {
            Seed = 0,
            Resolution = 255,
            _textureResolution = 255,
            CustomSplatMapResolution = false,
            MeshScale = 1000,
            generationMode = GenerationMode.RelativeToTerrain,
        };

        public MeshSettings ModifyResolution(int resolution)
        {
            float initialRatio = ScaleToResolution;
            this.Resolution = resolution;
            this.MeshScale = resolution / initialRatio;
            return this;
        }


        public bool SoftEqual(MeshSettings meshSettings)
        {
            return Resolution == meshSettings.Resolution && MeshScale == meshSettings.MeshScale;
        }
    }
}