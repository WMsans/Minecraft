using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class IndexCompactor 
    {
        private static readonly int
            maxInstancesID = Shader.PropertyToID("_MaxInstances"),
            chunksSkippedID = Shader.PropertyToID("_ChunksSkipped"),
            reducedIndicesID = Shader.PropertyToID("_ReducedIndices"),
            instancesPerChunkID = Shader.PropertyToID("_InstancesPerChunk"),
            perInstanceDataID = Shader.PropertyToID("_PerInstanceData"),
            targetLodsID = Shader.PropertyToID("_TargetLODs"),
            itemIndexID = Shader.PropertyToID("_ItemIndex"),
            chunkInstancesRowID = Shader.PropertyToID("_ChunkInstancesRow"),
            distanceBetweenID = Shader.PropertyToID("_DistanceBetween"),
            viewDistanceID = Shader.PropertyToID("_ViewDistance"),
            totalInstancesAddedID = Shader.PropertyToID("_TotalInstancesAdded");

        private VegetationSettings settings;
        public ComputeBuffer ReducedIndices{get; private set;}
        private ComputeBuffer ChunksSkipped;
        private IHoldVegetation asset;
        private List<int> Skips;


        public IndexCompactor(IHoldVegetation vegetationAsset, VegetationSettings vegSettings)
        {
            asset = vegetationAsset;
            settings = vegSettings;
            Skips = new();
            int MaxLength = vegSettings.ChunkInstances * vegSettings.TotalChunksInBuffer;
            ReducedIndices = new ComputeBuffer(MaxLength, sizeof(uint), ComputeBufferType.Structured);
            ChunksSkipped = new ComputeBuffer(vegSettings.TotalChunksInBuffer, sizeof(uint), ComputeBufferType.Structured);
        }

        public void Dispose(){
            if(ReducedIndices != null){
                ReducedIndices.Release();
                ReducedIndices = null;
            }                  
            if(ChunksSkipped != null){
                ChunksSkipped.Release();
                ChunksSkipped = null;
            }        
        }
        public void InitialCompact(CommandBuffer bf, List<int> indices, ICanBeCompacted drawCall, InstancesBuffer buffer, Matrix4x4 localToWorld)
        {
            var compute = VegetationRenderer.FillArguments;
            var kernel = VegetationRenderer.InitialCompactKernel;
            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            Skips.Clear();
            int targetID = 0;
            int currentDif = 0;
            bool init = false;
            Vector3 minBound = Vector3.zero;
            Vector3 maxBound = Vector3.zero;
            foreach (var index in indices)
            {
                VegetationChunk chunk = buffer.ChunksForPosition[index];
                if(!init){
                    minBound = chunk.minBounds;
                    maxBound = chunk.maxBounds;
                    init = true;
                }
                else{
                    minBound = FastMin(minBound, chunk.minBounds);
                    maxBound = FastMax(maxBound, chunk.maxBounds);
                }
                currentDif += index - targetID;
                Skips.Add(currentDif);
                targetID = index + 1;
            }

            drawCall.ApplyCorners(localToWorld, minBound, maxBound);
            int chunksToAdd = Skips.Count;
            drawCall.SetInstanceCount(chunksToAdd * settings.ChunkInstances, x);

            bf.SetBufferData(ChunksSkipped, Skips);
            bf.SetComputeBufferParam(compute, kernel, chunksSkippedID, ChunksSkipped);
            bf.SetComputeBufferParam(compute, kernel, reducedIndicesID, ReducedIndices);

            bf.SetComputeIntParam(compute, instancesPerChunkID, settings.ChunkInstances);
            bf.SetComputeIntParam(compute, maxInstancesID, chunksToAdd * settings.ChunkInstances);

            bf.DispatchCompute(compute, kernel, drawCall.Threads, 1, 1);
        }

        public void VisibilityCheck(CommandBuffer bf, ICanBeCompacted drawCall, InstancesBuffer buffer, IndexCompactor compactor)
        {
            var compute = VegetationRenderer.VisibilityCheck;
            var kernel = VegetationRenderer.VisibilityCheckKernel;
            if (drawCall.TotalInstancesAdded <= 0)
                return;

            asset.SetVisibilityShaderData(bf, compute);
            bf.SetKeyword(compute, VegetationRenderer.CullingKeyword, settings.Culling);
            
            bf.SetComputeIntParam(compute, itemIndexID, settings.ItemIndex);
            bf.SetComputeIntParam(compute, chunkInstancesRowID, settings.ChunkInstancesRow);
            bf.SetComputeFloatParam(compute, viewDistanceID, settings.ViewDistance);
            bf.SetComputeFloatParam(compute, distanceBetweenID, settings.DistanceBetweenItems);
            bf.SetComputeIntParam(compute, totalInstancesAddedID, drawCall.TotalInstancesAdded);
            bf.SetComputeBufferParam(compute, kernel, reducedIndicesID, compactor.ReducedIndices);
            bf.SetComputeBufferParam(compute, kernel, perInstanceDataID, buffer.PerInstanceData);
            bf.SetComputeBufferParam(compute, kernel, targetLodsID, drawCall.TargetLODs);
            bf.DispatchCompute(compute, kernel, drawCall.Threads, 1, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 FastMin(Vector3 vecA, Vector3 vecB){
            Vector3 result = vecA;
            result.x = Mathf.Min(vecA.x, vecB.x);
            result.y = Mathf.Min(vecA.y, vecB.y);
            result.z = Mathf.Min(vecA.z, vecB.z);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 FastMax(Vector3 vecA, Vector3 vecB){
            Vector3 result = vecA;
            result.x = Mathf.Max(vecA.x, vecB.x);
            result.y = Mathf.Max(vecA.y, vecB.y);
            result.z = Mathf.Max(vecA.z, vecB.z);
            return result;
        }
    }
}