using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{  
    public interface IHoldVegetation
    {
        // Placement and Variation Enums
        public enum HeightVariation { No, Random, SimplexNoise, Mixed }
        public enum DensityHeightMode { No, BetweenMinMaxSize, BetweenZeroMaxSize }
        public enum AlignmentMode { Up, Terrain, Ground }
        public enum ColorSamplingMode { HeightMapBlend, WeightBlend }
        public enum SpawnMode{GPUInstancing, CPUInstancing}
        public enum ColliderMode{Minimal, ByDistance, AllObjects}
        public SpawnMode GetSpawningMode();
        
        // Identification
        string name { get; }

        public bool SkipRendering();
        public bool DrawBoundingBox();
        public bool DrawColliderData();

        // Object Handling
        public ObjectData GetObjectData();
        public PositionData GetPositionData();
        public FillingData GetColorData();

        // Debugging and Visualization
        public void GizmosDrawDistances(Vector3 position);

        // Initialization
        public ArgumentsData InitializeMeshes();
        public void SetVisibilityShaderData(CommandBuffer bf, ComputeShader compute);
        

    }
}