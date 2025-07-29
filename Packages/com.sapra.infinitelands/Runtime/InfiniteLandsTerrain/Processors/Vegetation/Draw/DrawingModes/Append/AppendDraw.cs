using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands
{
    public class AppendDraw : IDrawInstances
    {
        private static readonly int
            shadowLodOffsetID = Shader.PropertyToID("_ShadowLodOffset"),
            perInstanceDataID = Shader.PropertyToID("_PerInstanceData"),
            targetLodsID = Shader.PropertyToID("_TargetLODs"),
            countersID = Shader.PropertyToID("_Counters"),
            maxInstancesID = Shader.PropertyToID("_MaxInstances"),
            lodValueID = Shader.PropertyToID("_LODValue"),
            transitionEnabled = Shader.PropertyToID("_TransitionEnabled"),
            shadowMode = Shader.PropertyToID("shadow_mode"),
            lodCountID = Shader.PropertyToID("_LODCount"),
            appendableIndicesID = Shader.PropertyToID("_Indices"),
            reducedIndicesID = Shader.PropertyToID("_ReducedIndices");

        private static readonly int[] appendableIDs = {
            Shader.PropertyToID("_AppendableIndices_1"),
            Shader.PropertyToID("_AppendableIndices_2"),
            Shader.PropertyToID("_AppendableIndices_3"),
            Shader.PropertyToID("_AppendableIndices_4"),
        };
        
        private static readonly int[] shadowAppendableIDs = {
            Shader.PropertyToID("_AppendableShadowsIndices_1"),
            Shader.PropertyToID("_AppendableShadowsIndices_2"),
            Shader.PropertyToID("_AppendableShadowsIndices_3"),
            Shader.PropertyToID("_AppendableShadowsIndices_4"),
        };
        private VegetationSettings settings;
        private List<AppendDrawCall> activeDrawers;

        private ArgumentsData ArgumentsData;
        private RenderParams[] renderParams;
        private bool CrossFadeEnabled;

        public AppendDraw(VegetationSettings vegSettings, ArgumentsData argumentsData, bool CrossFade)
        {
            ArgumentsData = argumentsData;
            settings = vegSettings;
            CrossFadeEnabled = CrossFade;
            activeDrawers = new();

            var materials = argumentsData.Lods.SelectMany(a => a.materials).ToArray();
            renderParams = new RenderParams[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                RenderParams rParams = new RenderParams(materials[i]);
                renderParams[i] = rParams;
            }
        }
        public ICanBeCompacted GetAvailableCompactable(int askingFor, Camera camera){      
            if(askingFor >= activeDrawers.Count)
                activeDrawers.Add(new AppendDrawCall(settings.ChunkInstances*settings.TotalChunksInBuffer,ArgumentsData));

            var targetDrawCall = activeDrawers[askingFor];
            targetDrawCall.UnusedTime = 0;
            return targetDrawCall;
        }

        public void PrepareDrawData(CommandBuffer bf, IndexCompactor compactor, int targetDrawIndex)
        {
            ComputeShader compute = VegetationRenderer.AppendingInstances;
            AppendDrawCall drawCall = activeDrawers[targetDrawIndex];
            if (drawCall.TotalInstancesAdded <= 0)
                return;
            bf.SetKeyword(compute, VegetationRenderer.ShadowKeyword, ArgumentsData.CastShadows);

            //Perform appending of the valid index
            drawCall.Reset(bf);
            Append(bf, compute, drawCall, compactor);
            drawCall.FillArguments(bf);
        }

        private void Append(CommandBuffer bf, ComputeShader compute, AppendDrawCall drawCall, IndexCompactor compactor){
            
            int kernel = VegetationRenderer.AppenderKernel;
            bf.SetComputeIntParam(compute, maxInstancesID, drawCall.TotalInstancesAdded);
            bf.SetComputeBufferParam(compute, kernel, reducedIndicesID, compactor.ReducedIndices);
            bf.SetComputeBufferParam(compute, kernel, targetLodsID, drawCall.TargetLODs);

            foreach (var keyword in VegetationRenderer.LightKeywords){
                bf.DisableKeyword(compute, keyword);
            }
            foreach (var keyword in VegetationRenderer.ShadowKeywords){
                bf.DisableKeyword(compute, keyword);
            }
            
            bf.EnableKeyword(compute, VegetationRenderer.LightKeywords[drawCall.LightAppendIndices.Length-1]);
            for(uint i = 0; i < drawCall.LightAppendIndices.Length; i++){
                bf.SetComputeBufferParam(compute, kernel, appendableIDs[i], drawCall.LightAppendIndices[i]);
            }

            if (ArgumentsData.CastShadows)
            {
                bf.EnableKeyword(compute, VegetationRenderer.ShadowKeywords[drawCall.ShadowAppendIndices.Length-1]);
                for(uint i = 0; i < drawCall.ShadowAppendIndices.Length; i++){
                    bf.SetComputeBufferParam(compute, kernel, shadowAppendableIDs[i], drawCall.ShadowAppendIndices[i]);
                }
                bf.SetComputeIntParam(compute, shadowLodOffsetID, ArgumentsData.ShadowLODOffset);
            }
            bf.DispatchCompute(compute, kernel, drawCall.Threads, 1, 1);
        }
        
        #region Draw
        public void DrawItems(MaterialPropertyBlock propertyBlock, InstancesBuffer buffer, int targetDrawIndex, Camera camera){
            AppendDrawCall drawCall = activeDrawers[targetDrawIndex];
            if(drawCall.TotalInstancesAdded <= 0)
                return;
            var cam = settings.GlobalRendering ? null : camera;

            propertyBlock.Clear();       
            propertyBlock.SetBuffer(countersID, drawCall.Counters);     
            propertyBlock.SetBuffer(targetLodsID, drawCall.TargetLODs);
            propertyBlock.SetBuffer(perInstanceDataID, buffer.PerInstanceData);
            propertyBlock.SetInt(lodCountID, ArgumentsData.LODLength);
            propertyBlock.SetInt(shadowLodOffsetID,-1);
            propertyBlock.SetInt(lodValueID, 0);
            propertyBlock.SetInt(transitionEnabled, CrossFadeEnabled?1:0);
            propertyBlock.SetInt(shadowMode, 0);

            int counter = 0;
            for(int l = 0; l < ArgumentsData.LODLength; l++){
                MeshLOD lod = ArgumentsData.Lods[l];
                if (!lod.valid)
                    continue;
                propertyBlock.SetBuffer(appendableIndicesID, drawCall.LightAppendIndices[l]);
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
                propertyBlock.SetInt(shadowLodOffsetID,ArgumentsData.ShadowLODOffset);
                propertyBlock.SetInt(shadowMode, 1);

                for(int l = 0; l < ArgumentsData.MaxShadowLOD; l++){
                    MeshLOD lod = ArgumentsData.Lods[l];
                    if (!lod.valid)
                        continue;
                    propertyBlock.SetBuffer(appendableIndicesID, drawCall.ShadowAppendIndices[l]);
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
            foreach(AppendDrawCall drawCall in activeDrawers){
                drawCall.Dispose();
            }
        }

        public void OnDrawGizmos(){
            Gizmos.color = Color.red;
            foreach(AppendDrawCall drawCall in activeDrawers){  
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