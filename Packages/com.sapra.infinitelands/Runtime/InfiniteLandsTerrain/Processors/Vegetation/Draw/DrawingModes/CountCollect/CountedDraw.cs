using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands
{
    public class CountedDraw : IDrawInstances
    {
        private static readonly int
            countersID = Shader.PropertyToID("_Counters"),
            shadowCountersID = Shader.PropertyToID("_ShadowCounters"),
            shadowLodOffsetID = Shader.PropertyToID("_ShadowLodOffset"),
            perInstanceDataID = Shader.PropertyToID("_PerInstanceData"),
            targetLodsID = Shader.PropertyToID("_TargetLODs"),
            shadowMode = Shader.PropertyToID("shadow_mode"),
            shadowIndicesID = Shader.PropertyToID("_ShadowIndices"),
            subMeshCountID = Shader.PropertyToID("_SubMeshCount"),
            maxInstancesID = Shader.PropertyToID("_MaxInstances"),
            lodValueID = Shader.PropertyToID("_LODValue"),
            lodCountID = Shader.PropertyToID("_LODCount"),
            indicesID = Shader.PropertyToID("_Indices"),
            transitionEnabled = Shader.PropertyToID("_TransitionEnabled"),
            reducedIndicesID = Shader.PropertyToID("_ReducedIndices");

        private VegetationSettings settings;
        private List<CountedDrawCall> activeDrawers;

        private ArgumentsData ArgumentsData;
        private RenderParams[] renderParams;

        public CountedDraw(VegetationSettings vegSettings, ArgumentsData argumentsData)
        {
            ArgumentsData = argumentsData;
            settings = vegSettings;
            activeDrawers = new();

            var materials = argumentsData.Lods.SelectMany(a => a.materials).ToArray();
            renderParams = new RenderParams[materials.Length];
            for(int i = 0; i < materials.Length; i++){
                RenderParams rParams = new RenderParams(materials[i]);
                renderParams[i] = rParams;
            }
        }

        public ICanBeCompacted GetAvailableCompactable(int askingFor, Camera camera){
            var cam = settings.GlobalRendering ? null : camera;
            if(askingFor >= activeDrawers.Count)
                activeDrawers.Add(new CountedDrawCall(settings.ChunkInstances*settings.TotalChunksInBuffer,ArgumentsData));

            var targetDrawCall = activeDrawers[askingFor];
            targetDrawCall.UnusedTime = 0;
            return targetDrawCall;
        }
        public void PrepareDrawData(CommandBuffer bf, IndexCompactor compactor, int targetDrawIndex)
        {
            CountedDrawCall drawCall = activeDrawers[targetDrawIndex];
            ComputeShader compute = VegetationRenderer.FillArguments;
            if (drawCall.TotalInstancesAdded <= 0)
                return;
            bf.SetKeyword(compute, VegetationRenderer.ShadowKeyword, ArgumentsData.CastShadows);
            
            drawCall.Reset(bf, compute);
            Count(bf, compute, drawCall, compactor);
            Sum(bf, compute, drawCall);
            Compact(bf, compute, drawCall, compactor);
            drawCall.FillArguments(bf, compute);
        }
        private void Count(CommandBuffer bf, ComputeShader compute, CountedDrawCall drawCall, IndexCompactor compactor)
        {
            int kernel = VegetationRenderer.CountKernel;

            bf.SetComputeIntParam(compute, lodCountID, ArgumentsData.LODLength);
            bf.SetComputeIntParam(compute, subMeshCountID, ArgumentsData.MaxSubMeshCount);
            bf.SetComputeIntParam(compute, maxInstancesID, drawCall.TotalInstancesAdded);

            bf.SetComputeBufferParam(compute, kernel, reducedIndicesID, compactor.ReducedIndices);
            bf.SetComputeBufferParam(compute, kernel, targetLodsID, drawCall.TargetLODs);

            bf.SetComputeBufferParam(compute, kernel, countersID, drawCall.LightCounts);
            if (ArgumentsData.CastShadows)
            {
                bf.SetComputeBufferParam(compute, kernel, shadowCountersID, drawCall.ShadowCounts);
                bf.SetComputeIntParam(compute, shadowLodOffsetID, ArgumentsData.ShadowLODOffset);
            }
            bf.DispatchCompute(compute, kernel, drawCall.Threads, 1, 1);
        }

        private void Sum(CommandBuffer bf, ComputeShader compute, CountedDrawCall drawCall)
        {
            int SumKernel = VegetationRenderer.SumKernel;
            bf.SetComputeBufferParam(compute, SumKernel, countersID, drawCall.LightCounts);
            if (ArgumentsData.CastShadows){
                bf.SetComputeBufferParam(compute, SumKernel, shadowCountersID, drawCall.ShadowCounts);
            }

            bf.DispatchCompute(compute, SumKernel, 1, 1, 1);
        }

        private void Compact(CommandBuffer bf, ComputeShader compute, CountedDrawCall drawCall, IndexCompactor compactor)
        {
            int kernel = VegetationRenderer.CompactKernel;
            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            bf.SetComputeBufferParam(compute, kernel, reducedIndicesID, compactor.ReducedIndices);
            bf.SetComputeBufferParam(compute, kernel, targetLodsID, drawCall.TargetLODs);
            bf.SetComputeBufferParam(compute, kernel, countersID, drawCall.LightCounts);
            bf.SetComputeBufferParam(compute, kernel, indicesID, drawCall.LightIndices);
            if (ArgumentsData.CastShadows)
            {
                bf.SetComputeBufferParam(compute, kernel, shadowCountersID, drawCall.ShadowCounts);
                bf.SetComputeBufferParam(compute, kernel, shadowIndicesID, drawCall.ShadowIndices);
            }

            bf.DispatchCompute(compute, kernel, drawCall.Threads, 1, 1);
        }

        #region Draw
        public void DrawItems(MaterialPropertyBlock propertyBlock, InstancesBuffer buffer, int targetDrawIndex, Camera camera){
            CountedDrawCall drawCall = activeDrawers[targetDrawIndex];
            if(drawCall.TotalInstancesAdded <= 0)
                return;
            var cam = settings.GlobalRendering ? null : camera;

            propertyBlock.Clear();            
            propertyBlock.SetBuffer(targetLodsID, drawCall.TargetLODs);
            propertyBlock.SetBuffer(perInstanceDataID, buffer.PerInstanceData);
            propertyBlock.SetInt(lodCountID, ArgumentsData.LODLength);
            propertyBlock.SetInt(transitionEnabled, 1);

            propertyBlock.SetInt(shadowMode, 0);
            propertyBlock.SetBuffer(indicesID, drawCall.LightIndices);
            propertyBlock.SetBuffer(countersID, drawCall.LightCounts);
            propertyBlock.SetInt(shadowLodOffsetID,-1);

            int counter = 0;
            for(int l = 0; l < ArgumentsData.LODLength; l++){
                MeshLOD lod = ArgumentsData.Lods[l];
                if (!lod.valid)
                    continue;
                propertyBlock.SetInt(lodValueID, l);
                for(int lm = 0; lm < lod.SubMeshCount; lm++){
                    RenderParams selected = renderParams[counter];
                    selected.matProps = propertyBlock;
                    selected.receiveShadows = true;
                    selected.camera = cam;
                    selected.layer = settings.Layers;
                    selected.shadowCastingMode = ShadowCastingMode.Off;
                    selected.worldBounds = drawCall.RenderBounds;

                    Graphics.RenderMeshIndirect(selected, lod.mesh, drawCall.LightArguments, 1, l*ArgumentsData.MaxSubMeshCount+lm);
                    counter++;
                }
            }            

            if(ArgumentsData.CastShadows){
                counter = 0;
                propertyBlock.SetBuffer(indicesID, drawCall.ShadowIndices);
                propertyBlock.SetBuffer(countersID, drawCall.ShadowCounts);
                propertyBlock.SetInt(shadowLodOffsetID,ArgumentsData.ShadowLODOffset);
                propertyBlock.SetInt(shadowMode, 1);

                for(int l = 0; l < ArgumentsData.MaxShadowLOD; l++){
                    MeshLOD lod = ArgumentsData.Lods[l];
                    if (!lod.valid)
                        continue;

                    propertyBlock.SetInt(lodValueID, l);
                    for(int lm = 0; lm < lod.SubMeshCount; lm++){
                        RenderParams selected = renderParams[counter];
                        selected.matProps = propertyBlock;
                        selected.receiveShadows = false;
                        selected.camera = cam;
                        selected.layer = settings.Layers;
                        selected.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                        selected.worldBounds = drawCall.RenderBounds;

                        Graphics.RenderMeshIndirect(selected, lod.mesh, drawCall.ShadowArguments, 1, l*ArgumentsData.MaxSubMeshCount+lm);
                        counter++;
                    }
                }
            } 
        }

        #endregion
        public void Dispose(){
            foreach(CountedDrawCall drawCall in activeDrawers){
                drawCall.Dispose();
            }
        }
        public void OnDrawGizmos(){
            Gizmos.color = Color.red;
            foreach(CountedDrawCall drawCall in activeDrawers){  
                Gizmos.DrawWireCube(drawCall.RenderBounds.center, drawCall.RenderBounds.size);
            }
        }
        public void AutoRelease(){
            for(int i = activeDrawers.Count-1; i >= 0; i--){
                var targetDrawCall = activeDrawers[i];
                targetDrawCall.UnusedTime += Time.deltaTime;
                if(targetDrawCall.UnusedTime >= 3){
                    targetDrawCall.Dispose();
                    activeDrawers.RemoveAt(i);
                }
            }
        }
    }
}