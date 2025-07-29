using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class CountedDrawCall : ICanBeCompacted{
        private static readonly int CounterDataSize = sizeof(uint)*4;
        private static readonly int
            countersID = Shader.PropertyToID("_Counters"),
            shadowCountersID = Shader.PropertyToID("_ShadowCounters"),
            argumentsID = Shader.PropertyToID("_Arguments"),
            shadowArgumentsID = Shader.PropertyToID("_ShadowArguments");


        public Bounds RenderBounds;
        public ComputeBuffer LightIndices{get; private set;}
        public ComputeBuffer ShadowIndices{get; private set;}
        
        public ComputeBuffer LightCounts{get; private set;}
        public ComputeBuffer ShadowCounts{get; private set;}

        public GraphicsBuffer LightArguments{get; private set;}
        public GraphicsBuffer ShadowArguments{get; private set;}

        public ComputeBuffer TargetLODs{get; private set;}
        
        public int TotalInstancesAdded{get; private set;}
        public int Threads{get; private set;}

        private ArgumentsData ArgumentsData;
        
        public float UnusedTime; 
        private Vector3[] bounds;
        private bool WarningSent;

        public CountedDrawCall(int MaxLength, ArgumentsData _argumentsData){
            ArgumentsData = _argumentsData;
            bounds = new Vector3[2];

            var arguments = _argumentsData.Arguments;
            CreateBuffers(MaxLength, arguments);
        }

        private void CreateBuffers(int MaxLength, List<GraphicsBuffer.IndirectDrawIndexedArgs> arguments){
            TargetLODs = new ComputeBuffer(MaxLength, sizeof(int), ComputeBufferType.Structured);
            if(arguments.Count > 0){
                LightIndices = new ComputeBuffer(MaxLength, sizeof(uint), ComputeBufferType.Structured);
                LightCounts = new ComputeBuffer(ArgumentsData.LODLength, CounterDataSize,  ComputeBufferType.Structured);
                
                LightArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, arguments.Count, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                LightArguments.SetData(arguments);
            }
            
            if(ArgumentsData.CastShadows && arguments.Count > 0){
                ShadowIndices = new ComputeBuffer(MaxLength, sizeof(int), ComputeBufferType.Structured);
                ShadowCounts = new ComputeBuffer(ArgumentsData.LODLength, CounterDataSize,  ComputeBufferType.Structured);                
                
                ShadowArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, arguments.Count, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                ShadowArguments.SetData(arguments);
            }
        }

        public void SetInstanceCount(int totalInstances, float threadGroupSize){
            TotalInstancesAdded = totalInstances;
            int ins = Mathf.CeilToInt(totalInstances / threadGroupSize);
            if(ins > 65500 && Application.isPlaying && !WarningSent){
                WarningSent = true;
                Debug.LogWarning("Too many instances prepared for draw call. Increase Distance Between Items or reduce View Distance!");
            }
            Threads = Mathf.Min(ins, 65500);
        }

        public void ApplyCorners(Matrix4x4 localToWorld, Vector3 minBounds, Vector3 maxBounds){
            bounds[0] = minBounds;
            bounds[1] = maxBounds;
            RenderBounds = GeometryUtility.CalculateBounds(bounds, localToWorld);
        }

        public void Reset(CommandBuffer bf, ComputeShader compute)
        {
            int ResetKernel = VegetationRenderer.ResetKernel;
            bf.SetComputeBufferParam(compute, ResetKernel, countersID, LightCounts);
            if (ArgumentsData.CastShadows){
                bf.SetComputeBufferParam(compute, ResetKernel,shadowCountersID, ShadowCounts);
            }

            bf.DispatchCompute(compute, ResetKernel, ArgumentsData.LODLength, 1, 1);
        }

        public void FillArguments(CommandBuffer bf, ComputeShader compute)
        {
            int FillKernel = VegetationRenderer.FillKernel;
            bf.SetComputeBufferParam(compute, FillKernel, countersID, LightCounts);
            bf.SetComputeBufferParam(compute, FillKernel, argumentsID, LightArguments);
            if (ArgumentsData.CastShadows)
            {
                bf.SetComputeBufferParam(compute, FillKernel, shadowCountersID, ShadowCounts);
                bf.SetComputeBufferParam(compute, FillKernel, shadowArgumentsID, ShadowArguments);
            }
            bf.DispatchCompute(compute, FillKernel, ArgumentsData.LODLength, Mathf.Max(ArgumentsData.MaxSubMeshCount, 1), 1);
        }

        public void Dispose(){
            if(LightIndices != null){
                LightIndices.Release();
                LightIndices = null;
            }

            if(ShadowIndices != null){
                ShadowIndices.Release();
                ShadowIndices = null;
            }
            
            if(ShadowCounts != null){
                ShadowCounts.Release();
                ShadowCounts = null;
            }

            if(LightCounts != null){
                LightCounts.Release();
                LightCounts = null;
            }

            if(TargetLODs != null){
                TargetLODs.Release();
                TargetLODs = null;
            } 
            
            if(LightArguments != null){
                LightArguments.Release();
                LightArguments = null;
            }            
            
            if(ShadowArguments != null){
                ShadowArguments.Release();
                ShadowArguments = null;
            } 
        }
    }
}