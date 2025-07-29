using UnityEngine;
using UnityEngine.Rendering;
using static sapra.InfiniteLands.IHoldVegetation;

namespace sapra.InfiniteLands{
    public readonly struct PositionData{
        private static readonly int 
            verticalPositionID = Shader.PropertyToID("_VerticalPosition"),
            positionRandomnessID = Shader.PropertyToID("_PositionRandomness"),
            densityIsHeightID = Shader.PropertyToID("_DensityIsHeight"),
            alignToGroundID = Shader.PropertyToID("_AlignToGround"),
            heightVariationID = Shader.PropertyToID("_HeightVariation"),
            sizeID = Shader.PropertyToID("_Size"),
            patchSizeID = Shader.PropertyToID("_PatchSize");

        public readonly float distanceBetweenItems;
        public readonly float verticalPosition;
        public readonly float viewDistance;
        public readonly float positionRandomness;
        public readonly Vector2 minimumMaximumScale;

        public readonly AlignmentMode alignmentMode;
        public readonly HeightVariation heightVariationMode;
        public readonly float heightVariationSimplexSize;
        public readonly DensityHeightMode densityHeightMode;

        public PositionData(float distanceBetweenItems, float verticalPosition, float viewDistance, float positionRandomness, float heightVariationSimplexSize,
            Vector2 minimumMaximumScale, AlignmentMode alignmentMode, HeightVariation heightVariationMode, DensityHeightMode densityHeightMode)
        {
            this.distanceBetweenItems = distanceBetweenItems;
            this.verticalPosition = verticalPosition;
            this.positionRandomness = positionRandomness;
            this.viewDistance = viewDistance;
            this.alignmentMode = alignmentMode;
            this.heightVariationMode = heightVariationMode;
            this.heightVariationSimplexSize = heightVariationSimplexSize;
            this.densityHeightMode = densityHeightMode;
            this.minimumMaximumScale = minimumMaximumScale;
        }

        public void SetDataToBuffers(ComputeShader compute, CommandBuffer bf){
            bf.SetComputeFloatParam(compute, positionRandomnessID, positionRandomness);
            bf.SetComputeFloatParam(compute, verticalPositionID, verticalPosition);
            bf.SetComputeIntParam(compute, densityIsHeightID, (int)densityHeightMode);
            bf.SetComputeIntParam(compute, alignToGroundID, (int)alignmentMode);
            bf.SetComputeIntParam(compute, heightVariationID, (int)heightVariationMode);
            bf.SetComputeFloatParam(compute, patchSizeID, heightVariationSimplexSize);
            bf.SetComputeVectorParam(compute, sizeID, minimumMaximumScale);
        }
    }
}