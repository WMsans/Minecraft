using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands
{
    public class VegetationChunk
    {
        public enum ChunkState { Disabled, WaitingToBeCreated, Created }
        public enum VegetationState { Unrequested, Requested, Created }
        private static readonly int
            perInstanceDataID = Shader.PropertyToID("_PerInstanceData"),
            indexOffsetID = Shader.PropertyToID("_IndexOffset"),
            minmaxBufferID = Shader.PropertyToID("_MinMaxBuffer"),
            itemIndex = Shader.PropertyToID("_ItemIndex"),
            chunkInstancesRowID = Shader.PropertyToID("_ChunkInstancesRow"),
            validIndicesID = Shader.PropertyToID("_ValidIndices"),
            distanceBetweenID = Shader.PropertyToID("_DistanceBetween"),
            textureRandomnessDistanceID = Shader.PropertyToID("_TextureRandomnessDistance"),
            chunkPositionID = Shader.PropertyToID("_ChunkPosition"),
            subTextureIndexID = Shader.PropertyToID("_SubTextureIndex"),
            densityMapID = Shader.PropertyToID("_DensityMap"),
            meshOffsetID = Shader.PropertyToID("_MeshOffset"),
            resolutionID = Shader.PropertyToID("_Resolution"),
            terrainHeightNormalID = Shader.PropertyToID("_TerrainHeightNormal"),
            localToWorldID = Shader.PropertyToID("_localToWorld"),
            worldToLocalID = Shader.PropertyToID("_worldToLocal"),
            chunkSizeID = Shader.PropertyToID("_ChunkSize"),
            meshScaleID = Shader.PropertyToID("_MeshScale");

        public Vector2Int ID { get; private set; }
        public Vector3 minBounds { get; private set; }
        public Vector3 maxBounds { get; private set; }
        public Bounds Bounds { get; private set; }
        public Vector2 FlatPosition { get; private set; }
        public BufferIndex bufferData { get; private set; }

        private Vector3 Position;

        private VegetationSettings Settings;
        private TextureResult TextureSplatMap;
        private VegetationResult VegetationSplatMap;
        private bool TexturesRecived;
        private bool VegetationRecived;

        private IControlTerrain infiniteLandsController;
        private PositionData positionData;
        private FillingData colorData;

        private List<InstanceData> Instances = null;

        private TerrainPainter TerrainPainter;
        private VegetationRenderer Renderer;
        public Action<Vector2Int, VegetationChunk> OnChunkCreated;
        public Action<Vector2Int, List<InstanceData>> OnInstancesCreated;
        private Action<AsyncGPUReadbackRequest> OnFinishedGeneration;
        private Action<AsyncGPUReadbackRequest> OnFinishTextures;

        private Action<AsyncGPUReadbackRequest> OnInstancesGeneratedCallback;
        private Action<AsyncGPUReadbackRequest> OnLoadMinMaxBuffer;

        private ChunkState chunkState;
        private VegetationState instancesState;
        private bool WithInstances;

        public int Uses { get; private set; }

        #region CacheStuff
        private string AssetName;
        private int[] MinMaxArray;

        private bool PositionsCalculated;
        private bool TextureCalculated;
        #endregion
        public void IncreaseUses()
        {
            Uses++;
        }
        public bool DecreaseUses()
        {
            Uses--;
            return Uses <= 0;
        }

        private ComputeBuffer MinMaxBuffer;
        private ComputeBuffer TextureMasksBuffer;

        public VegetationChunk(IHoldVegetation asset, VegetationSettings vegetationSettings, IControlTerrain infiniteLandsController, ComputeBuffer _textureMasksBuffer)
        {
            Settings = vegetationSettings;
            this.infiniteLandsController = infiniteLandsController;
            AssetName = asset.name;
            positionData = asset.GetPositionData();
            colorData = asset.GetColorData();
            TextureMasksBuffer = _textureMasksBuffer;

            OnFinishedGeneration = FinishAllGeneration;
            OnInstancesGeneratedCallback = RecieveInstanceData;
            OnLoadMinMaxBuffer = LoadMinMaxBuffer;
            OnFinishTextures = FinishTextures;

            MinMaxArray = new int[] { int.MaxValue, int.MinValue };

            TerrainPainter = infiniteLandsController.GetInternalComponent<TerrainPainter>();
            if (TerrainPainter != null && !TerrainPainter.ContainsTextures)
                TerrainPainter = null;
            Renderer = infiniteLandsController.GetInternalComponent<VegetationRenderer>();
            DisableChunk();

            MinMaxBuffer = new ComputeBuffer(2, sizeof(int));
            Renderer.onProcessDone += OnChunkGenerated;
            if (TerrainPainter != null)
                TerrainPainter.onProcessDone += OnTextureGenerated;

        }
        public void EnableChunk(BufferIndex buffer, Vector2Int id, Vector2 position)
        {
            ID = id;
            Position = new Vector3(position.x, 0, position.y);
            FlatPosition = position;
            bufferData = buffer;
            chunkState = ChunkState.WaitingToBeCreated;
            instancesState = VegetationState.Unrequested;

            Reset();

            if (Renderer.TryGetDataAt(FlatPosition, out var existingData))
                SetInstancesToBuffer(existingData);
            if (TerrainPainter != null)
            {
                if (TerrainPainter.TryGetDataAt(FlatPosition, out var retrieveData))
                    UpdateTextureIndices(retrieveData);
            }
        }

        public void DisableChunk()
        {
            Reset();
            chunkState = ChunkState.Disabled;
            instancesState = VegetationState.Unrequested;
            if (Instances != null)
            {
                ListPoolLight<InstanceData>.Release(Instances);
                Instances = null;
            }
        }


        private void Reset()
        {
            TexturesRecived = false;
            VegetationRecived = false;
            Uses = 0;
            TextureCalculated = false;
            PositionsCalculated = false;
        }

        public void Dispose()
        {
            OnInstancesCreated = null;
            OnChunkCreated = null;
            Renderer.onProcessDone -= OnChunkGenerated;
            if (TerrainPainter != null)
                TerrainPainter.onProcessDone -= OnTextureGenerated;
            MinMaxBuffer.Release();
            MinMaxBuffer = null;
        }
        public void OriginShift(Vector3 offset)
        {
            if (instancesState == VegetationState.Created)
            {
                for (int i = 0; i < Instances.Count; i++)
                {
                    var instance = Instances[i];
                    instance.PerformShift(offset);
                    Instances[i] = instance;
                }
            }
        }
        private void FinishTextures(AsyncGPUReadbackRequest request)
        {
            if (request.done)
            {
                TextureCalculated = true;
                ValidateTextureIndices();
            }
        }
        private void OnChunkGenerated(VegetationResult result)
        {
            if (chunkState == ChunkState.Disabled) return;
            if (VegetationRecived && result.TerrainConfiguration.ID.z >= VegetationSplatMap.TerrainConfiguration.ID.z) return;
            if (!infiniteLandsController.IsPointInChunkAtGrid(FlatPosition, result.TerrainConfiguration)) return;

            SetInstancesToBuffer(result);
        }

        private void OnTextureGenerated(TextureResult result)
        {
            if (chunkState == ChunkState.Disabled) return;
            if (TexturesRecived && result.TerrainConfiguration.ID.z >= TextureSplatMap.TerrainConfiguration.ID.z) return;
            if (!infiniteLandsController.IsPointInChunkAtGrid(FlatPosition, result.TerrainConfiguration)) return;

            UpdateTextureIndices(result);
        }

        private void SetCommonParameters(CommandBuffer bf, ComputeShader compute, int kernel, Vector3Int ID, MeshSettings meshSettings, TerrainConfiguration terrainConfiguration)
        {
            bf.SetComputeIntParam(compute, itemIndex, Settings.ItemIndex);
            bf.SetComputeIntParam(compute, chunkInstancesRowID, Settings.ChunkInstancesRow);
            bf.SetComputeFloatParam(compute, distanceBetweenID, Settings.DistanceBetweenItems);
            bf.SetComputeBufferParam(compute, kernel, perInstanceDataID, bufferData.instanceBuffer.PerInstanceData);
            bf.SetComputeFloatParam(compute, chunkSizeID, Settings.ChunkSize);

            var offset = Position - terrainConfiguration.Position + new Vector3(meshSettings.MeshScale, 0, meshSettings.MeshScale) / 2.0f;
            var ind = Vector3Int.FloorToInt(offset / Settings.ChunkSize);
            var chunkOffset = new Vector3(ind.x, 0, ind.z) * Settings.ChunkSize + new Vector3(Settings.ChunkSize / 2.0f, 0, Settings.ChunkSize / 2.0f);
            bf.SetComputeVectorParam(compute, chunkPositionID, chunkOffset);
            bf.SetComputeIntParam(compute, indexOffsetID, bufferData.chunkIndex * Settings.ChunkInstancesRow * Settings.ChunkInstancesRow);

            positionData.SetDataToBuffers(compute, bf);

        }

        private void SetInstancesToBuffer(VegetationResult result)
        {
            VegetationSplatMap = result;
            VegetationRecived = true;

            CommandBuffer bf = CommandBufferPool.Get(AssetName);
            ComputeShader compute = VegetationRenderer.CalculatePositions;
            int kernel = VegetationRenderer.CalculatePositionsKernel;

            bf.SetComputeMatrixParam(compute, localToWorldID, infiniteLandsController.localToWorldMatrix);
            bf.SetComputeMatrixParam(compute, worldToLocalID, infiniteLandsController.worldToLocalMatrix);

            Texture2D splatMapTexture = VegetationSplatMap.GetTextureOf(Settings.TextureIndex);
            Texture2D heightMapTexture = VegetationSplatMap.NormalAndHeight;

            MeshSettings meshSettings = VegetationSplatMap.MeshSettings;
            TerrainConfiguration configuration = VegetationSplatMap.TerrainConfiguration;
            SetCommonParameters(bf, compute, kernel, VegetationSplatMap.TerrainConfiguration.ID, meshSettings, configuration);

            bf.SetComputeTextureParam(compute, kernel, densityMapID, splatMapTexture);
            bf.SetComputeIntParam(compute, subTextureIndexID, Settings.SubTextureIndex);
            bf.SetComputeTextureParam(compute, kernel, terrainHeightNormalID, heightMapTexture);
            bf.SetComputeFloatParam(compute, meshScaleID, meshSettings.MeshScale);
            bf.SetComputeIntParam(compute, resolutionID, meshSettings.TextureResolution);
            bf.SetComputeVectorParam(compute, meshOffsetID, configuration.Position);

            bf.SetBufferData(MinMaxBuffer, MinMaxArray);
            bf.SetComputeBufferParam(compute, kernel, minmaxBufferID, MinMaxBuffer);

            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            int bladesInChunk = Mathf.CeilToInt(Settings.ChunkInstancesRow / (float)x);
            bf.DispatchCompute(compute, kernel, bladesInChunk, bladesInChunk, 1);
            bf.RequestAsyncReadback(MinMaxBuffer, OnLoadMinMaxBuffer);
            Graphics.ExecuteCommandBuffer(bf);
            CommandBufferPool.Release(bf);
        }

        private void UpdateTextureIndices(TextureResult textureData)
        {
            TextureSplatMap = textureData;
            TexturesRecived = true;

            CommandBuffer bf = CommandBufferPool.Get(AssetName);
            ComputeShader compute = VegetationRenderer.CalculateTextures;
            int kernel = VegetationRenderer.CalculateTexturesKernel;
            SetCommonParameters(bf, compute, kernel, TextureSplatMap.TerrainConfiguration.ID, TextureSplatMap.MeshSettings, TextureSplatMap.TerrainConfiguration);
            TextureSplatMap.DynamicMeshResultApply(bf, compute, kernel);
            TerrainPainter.AssignTexturesToMaterials(bf, compute, kernel, colorData.colorSamplingMode);
            bf.SetComputeFloatParam(compute, textureRandomnessDistanceID, colorData.samplingRandomness);

            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            int bladesInChunk = Mathf.CeilToInt(Settings.ChunkInstancesRow / (float)x);
            bf.DispatchCompute(compute, kernel, bladesInChunk, bladesInChunk, 1);
            bf.RequestAsyncReadback(bufferData.instanceBuffer.PerInstanceData, 1, 0, OnFinishTextures);
            Graphics.ExecuteCommandBuffer(bf);
            CommandBufferPool.Release(bf);
        }



        public void ValidateTextureIndices()
        {
            if (!PositionsCalculated)
                return;

            if (TerrainPainter == null || (TextureMasksBuffer == null || (TextureMasksBuffer != null && !TextureMasksBuffer.IsValid())))
            {
                FinishAllGeneration();
                return;
            }

            if (!TextureCalculated)
                return;

            CommandBuffer bf = CommandBufferPool.Get(AssetName);
            ComputeShader compute = VegetationRenderer.ValidateTextureIndices;
            int kernel = VegetationRenderer.ValidateTextureIndicesKernel;
            bf.SetComputeBufferParam(compute, kernel, perInstanceDataID, bufferData.instanceBuffer.PerInstanceData);
            bf.SetComputeIntParam(compute, indexOffsetID, bufferData.chunkIndex * Settings.ChunkInstancesRow * Settings.ChunkInstancesRow);
            bf.SetComputeIntParam(compute, chunkInstancesRowID, Settings.ChunkInstancesRow);
            bf.SetComputeBufferParam(compute, 0, validIndicesID, TextureMasksBuffer);

            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            int bladesInChunk = Mathf.CeilToInt(Settings.ChunkInstancesRow / (float)x);
            bf.DispatchCompute(compute, kernel, bladesInChunk, bladesInChunk, 1);
            bf.RequestAsyncReadback(TextureMasksBuffer, OnFinishedGeneration);
            Graphics.ExecuteCommandBuffer(bf);
            CommandBufferPool.Release(bf);
        }

        public void FinishAllGeneration(AsyncGPUReadbackRequest result)
        {
            if (result.done)
            {
                FinishAllGeneration();
            }
        }

        public void FinishAllGeneration()
        {
            if (WithInstances && instancesState != VegetationState.Unrequested)
            {
                instancesState = VegetationState.Unrequested;
                GetInstances();
            }

            if (chunkState == ChunkState.WaitingToBeCreated)
            {
                chunkState = ChunkState.Created;
                OnChunkCreated?.Invoke(ID, this);
            }
        }

        public bool IsVisible(Vector3 cameraPosition, Plane[] planes)
        {
            if (!WithInstances)
                return false;
            if (chunkState != ChunkState.Created)
                return false;
            Vector3 closestPoint = Bounds.ClosestPoint(cameraPosition);
            float distance = Vector3.Distance(cameraPosition, closestPoint);
            bool InView = GeometryUtility.TestPlanesAABB(planes, Bounds) && distance < Settings.ViewDistance;
            return InView || distance < Settings.ChunkSize / 2.0f;
        }

        private bool ValidData(Vector2Int id)
        {
            return ID.Equals(id) && chunkState != ChunkState.Disabled;
        }

        private void LoadMinMaxBuffer(AsyncGPUReadbackRequest result)
        {
            if (!ValidData(ID))
                return;
            if (result.done)
            {
                PositionsCalculated = true;
                NativeArray<int> data = result.GetData<int>(0);
                int minHeight = data[0];
                int maxHeight = data[1];
                data.Dispose();
                Vector3 center = new Vector3(Position.x, (maxHeight + minHeight) / 2f, Position.z);
                Vector3 size = new Vector3(Settings.ChunkSize, maxHeight - minHeight + positionData.verticalPosition, Settings.ChunkSize);
                Bounds = new Bounds(center, size);
                minBounds = Bounds.min;
                maxBounds = Bounds.max;
                WithInstances = minHeight != int.MaxValue || maxHeight != int.MinValue;
                ValidateTextureIndices();
            }
        }

        public bool IsInsideCollision(Vector3 playerPosition, float CollisionDistance)
        {
            if (!WithInstances)
                return false;
            if (chunkState != ChunkState.Created)
                return false;
            Vector3 closestPoint = Bounds.ClosestPoint(playerPosition);
            float distance = Vector3.Distance(playerPosition, closestPoint);
            return distance < CollisionDistance;
        }

        public void RecieveInstanceData(AsyncGPUReadbackRequest request)
        {
            if (!ValidData(ID) || instancesState != VegetationState.Requested)
                return;

            if (request.done)
            {
                Instances = ListPoolLight<InstanceData>.Get();
                NativeArray<InstanceData> allInstances = request.GetData<InstanceData>(0);
                for (int i = 0; i < allInstances.Length; i++)
                {
                    Instances.Add(allInstances[i]);
                }
                allInstances.Dispose();
                instancesState = VegetationState.Created;
                OnInstancesCreated?.Invoke(ID, Instances);
            }
        }

        public List<InstanceData> GetInstances()
        {
            if (!WithInstances)
                return null;

            if (instancesState == VegetationState.Unrequested)
            {
                instancesState = VegetationState.Requested;
                CommandBuffer bf = CommandBufferPool.Get();
                int offset = bufferData.chunkIndex * Settings.ChunkInstances;
                bf.RequestAsyncReadback(bufferData.instanceBuffer.PerInstanceData, Settings.ChunkInstances * InstanceData.size, offset * InstanceData.size, OnInstancesGeneratedCallback);
                Graphics.ExecuteCommandBuffer(bf);
                CommandBufferPool.Release(bf);
            }

            if (instancesState != VegetationState.Created)
                return null;
            else
                return Instances;
        }
        public void DrawGizmos()
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

#if UNITY_EDITOR
            GUI.color = Color.gray;
            Handles.Label(Bounds.center, ID.ToString());
#endif
        }
    }
}