using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class AppendDrawCall : ICanBeCompacted{
        public Bounds RenderBounds;
        public ComputeBuffer[] LightAppendIndices{get; private set;}
        public ComputeBuffer[] ShadowAppendIndices{get; private set;}

        public GraphicsBuffer LightArguments{get; private set;}
        public GraphicsBuffer ShadowArguments{get; private set;}

        public ComputeBuffer TargetLODs{get; private set;}
        public ComputeBuffer Counters{get; private set;}

        public int TotalInstancesAdded{get; private set;}
        public int Threads{get; private set;}
        private ArgumentsData ArgumentsData;
        
        public float UnusedTime; 
        private Vector3[] bounds;
        private bool WarningSent;
        public AppendDrawCall(int MaxLength, ArgumentsData _argumentsData){
            ArgumentsData = _argumentsData;
            bounds = new Vector3[2];

            CreateBuffers(MaxLength, _argumentsData);
        }

        private void CreateBuffers(int MaxLength, ArgumentsData argumentsData){
            var arguments = argumentsData.Arguments;
            TargetLODs = new ComputeBuffer(MaxLength, sizeof(int), ComputeBufferType.Structured);
            Counters = new ComputeBuffer(1, sizeof(uint)*4,  ComputeBufferType.Structured);
            Counters.SetData(new uint[]{0,0,0,0});

            if(arguments.Count > 0){
                LightAppendIndices = new ComputeBuffer[argumentsData.LODLength];
                for(int i = 0; i < argumentsData.LODLength; i++){
                    LightAppendIndices[i] = new ComputeBuffer(MaxLength, sizeof(uint), ComputeBufferType.Append);
                }
                
                LightArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, arguments.Count, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                LightArguments.SetData(arguments);
            }
            
            if(ArgumentsData.CastShadows && arguments.Count > 0){
                ShadowAppendIndices = new ComputeBuffer[argumentsData.LODLength];
                for(int i = 0; i < argumentsData.LODLength; i++){
                    ShadowAppendIndices[i] = new ComputeBuffer(MaxLength, sizeof(uint), ComputeBufferType.Append);
                }
               
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

        public void Reset(CommandBuffer bf)
        {
            foreach(var buf in LightAppendIndices){
                bf.SetBufferCounterValue(buf, 0);
            }
            if(ShadowAppendIndices != null){
                foreach(var buf in ShadowAppendIndices){
                    bf.SetBufferCounterValue(buf, 0);
                }
            }
        }

        public void FillArguments(CommandBuffer bf)
        {   
            for(uint x = 0; x < ArgumentsData.LODLength; x++){
                for(uint y = 0; y < ArgumentsData.MaxSubMeshCount; y++){
                    uint offset = (uint)(x*ArgumentsData.MaxSubMeshCount+y)*5+1;
                    bf.CopyCounterValue(LightAppendIndices[x], LightArguments, offset*4);

                    if(ShadowAppendIndices != null){
                        bf.CopyCounterValue(ShadowAppendIndices[x], ShadowArguments, offset*4);
                    }
                }
            }
        }

        public void Dispose(){
            if(LightAppendIndices != null){
                for(uint i = 0; i < LightAppendIndices.Length; i++){
                    LightAppendIndices[i].Release();
                }
                LightAppendIndices = null;
            }

            if(ShadowAppendIndices != null){
                for(uint i = 0; i < ShadowAppendIndices.Length; i++){
                    ShadowAppendIndices[i].Release();
                }
                ShadowAppendIndices = null;
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
            if(Counters != null){
                Counters.Release();
                Counters = null;
            } 
        }
    }
}