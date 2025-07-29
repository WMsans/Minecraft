using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static sapra.InfiniteLands.IHoldVegetation;
using System.Linq;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands{
    [AssetNode(typeof(AssetOutputNode))]
    [CreateAssetMenu(menuName = "Infinite Lands/Assets/Vegetation")]
    public class VegetationAsset : InfiniteLandsAsset, IHoldVegetation, IHoldMaterials, IHaveAssetPreview
    {
        private static readonly int
            lodDistanceID = Shader.PropertyToID("_LodDistance"),
            shadowDistanceID = Shader.PropertyToID("_ShadowDistance"),
            minBoundsID = Shader.PropertyToID("_MinBounds"),
            maxBoundsID = Shader.PropertyToID("_MaxBounds"),
            halfInstancesDistanceID = Shader.PropertyToID("_HalfInstancesDistance"),
            lodCountID = Shader.PropertyToID("_LODCount");

        [Header("Mesh Data")]
        public bool skipRendering = false;
        public SpawnMode spawnMode = SpawnMode.GPUInstancing;
        public SpawnMode GetSpawningMode() => spawnMode;

        //Mesh configuration
                //GPU
            public bool GenerateColliders;
            public ColliderMode ColliderMode;
            private bool isByDistance => ColliderMode == ColliderMode.ByDistance;
            [ShowIf(nameof(isByDistance))][Min(1)] public float CollisionDistance;
            public Collider ColliderObject;
            public LODGroup LodGroups;
            public MeshLOD[] LOD;
            [ShowIf(nameof(HasLods))] public float HighLodDistance = 50;
            public bool CrossFadeLODDithering = true;

                //CPU   
            public GameObject InstanceObject;
            public Material[] Materials;

        
        [Header("Position Data")] 
        [Min(1)] public float ViewDistance = 200;
        [Min(0.01f)] public float DistanceBetweenItems = 100;
        [Range(0,1)] public float PositionRandomness = 1;
        
        public float VerticalPosition = 0;
        public AlignmentMode AlignToGround = AlignmentMode.Up;
        public HeightVariation HeightVariation = HeightVariation.Random;
        public float SimplexNoiseSize = 1;
        public DensityHeightMode DensityAffectsHeight = DensityHeightMode.No;
        [Min(0.01f)] public float MinimumScale = 1;
        [Min(0.01f)] public float MaximumScale = 1;

                //GPU Instancing
            public bool HalfInstancesAtDistance;
            [Min(1)] public float HalfInstancesDistance = 30;   
            public bool CastShadows = false;
            [Min(0)] public int ShadowsLODOffset = 0;
            [Min(1)] public float _shadowDistance = 100;

        [Header("Color Data")]
        public ColorSamplingMode HowToSampleColor = ColorSamplingMode.HeightMapBlend;
        [Min(0)] public float SamplingRandomness;
        public List<TextureAsset> RemoveAtTextures;
           
       
        [Header("Debugging")] 
        public bool DrawDistances;
        private bool GPUDrawMode => spawnMode == SpawnMode.GPUInstancing && DrawDistances;
        [ShowIf(nameof(GPUDrawMode))] public bool DrawLODBoundaries;
        private bool GPUTransitionMode => GPUDrawMode && CrossFadeLODDithering;
        [ShowIf(nameof(GPUTransitionMode))] public bool DrawTransitions;
        private bool GpuShadowMode => GPUDrawMode && DrawDistances && CastShadows;
        [ShowIf(nameof(GpuShadowMode))] public bool DrawShadowBoundaries;
        private bool GpuColliderMode => GPUDrawMode && DrawDistances && ColliderObject != null && GenerateColliders;
        [ShowIf(nameof(GpuColliderMode))] public bool DrawSpawnedColliders;

        [ShowIf(nameof(DrawDistances))] public bool DrawItAsSpheres;
        public bool drawBoundingBox;

        private Vector3 MaxMeshBounds;
        private Vector3 MinMeshBounds;
        private bool SmallObjectMode => (MaxMeshBounds-MinMeshBounds).y < 5;
        private float ShadowDistance => Mathf.Min(_shadowDistance, ViewDistance);
        private bool HasLods => (LOD != null && LOD.Length > 1) || (LodGroups != null && LodGroups.lodCount > 1);

        public bool SkipRendering() => skipRendering;
        public bool DrawBoundingBox() => drawBoundingBox;
        public bool DrawColliderData() => GpuColliderMode && DrawSpawnedColliders;

        public ObjectData GetObjectData()
        {
            switch (spawnMode)
            {
                case SpawnMode.GPUInstancing:
                    var targetGameobject = (GenerateColliders && ColliderObject != null) ? ColliderObject.gameObject : null;
                    return new ObjectData(ColliderObject != null, targetGameobject, ColliderMode, CollisionDistance);
                default:
                    return new ObjectData(true, InstanceObject, ColliderMode.AllObjects, -1);
            }
        }

        public PositionData GetPositionData()
        {
            return new PositionData(DistanceBetweenItems, VerticalPosition, ViewDistance, PositionRandomness, SimplexNoiseSize, 
                new Vector2(MinimumScale, MaximumScale), AlignToGround, HeightVariation, DensityAffectsHeight);
        }

        public FillingData GetColorData()
        {
            return new FillingData(CrossFadeLODDithering && spawnMode == SpawnMode.GPUInstancing, HowToSampleColor, SamplingRandomness, RemoveAtTextures);
        }
        public IEnumerable<Material> GetMaterials()
        {
            if(spawnMode == SpawnMode.GPUInstancing){
                if(LodGroups != null)
                    return LodGroups.GetLODs().SelectMany(a => a.renderers.SelectMany(a => a.sharedMaterials)).Where(a => a != null).Distinct();
                else
                    return LOD.SelectMany(a => a.materials).Where(a => a != null).Distinct();
            }
            else{
                if(Materials.Length > 0){
                    var distinctMats = Materials.Distinct();
                    foreach(Material mat in distinctMats){
                        mat.enableInstancing = true;
                    }
                    return distinctMats;
                }
            }

            return new Material[0];

        }

        public void SetVisibilityShaderData(CommandBuffer bf, ComputeShader compute){
            if(spawnMode == SpawnMode.GPUInstancing){
                //Calculate Positions Data
                bf.SetComputeFloatParam(compute, lodDistanceID, HasLods? HighLodDistance:ViewDistance);
                bf.SetComputeFloatParam(compute, shadowDistanceID, CastShadows?_shadowDistance:0);
                bf.SetComputeIntParam(compute,lodCountID, LOD.Length);
                bf.SetComputeFloatParam(compute, halfInstancesDistanceID, HalfInstancesAtDistance?HalfInstancesDistance:ViewDistance);
                bf.SetComputeVectorParam(compute, minBoundsID, MinMeshBounds);
                bf.SetComputeVectorParam(compute, maxBoundsID, MaxMeshBounds);

                bf.SetKeyword(compute, VegetationRenderer.SmallObjectMode, SmallObjectMode);
                bf.SetKeyword(compute, VegetationRenderer.VisibilityShadows, CastShadows);
            }
        }

        #region MeshInitalization
        public ArgumentsData InitializeMeshes()
        {
            if(spawnMode == SpawnMode.GPUInstancing){
                return GPUInitialize();
            }
            else
                return CPUInitalize();
        }

        private ArgumentsData GPUInitialize(){
            var LODLength = Mathf.Min(Mathf.CeilToInt(CalculateLOD(ViewDistance, HighLodDistance)), LOD.Length);
            var MaxShadowLOD = Mathf.Min(Mathf.CeilToInt(CalculateLOD(_shadowDistance, HighLodDistance)), LODLength);
            var MaxSubMeshCount = 0;
            //If there's an LOD group
            if (LodGroups != null)
            {
                LOD[] ld = LodGroups.GetLODs();
                LOD = new MeshLOD[ld.Length];
                for (int i = 0; i < ld.Length; i++)
                {
                    UnityEngine.Mesh msh = ld[i].renderers[0].GetComponent<MeshFilter>().sharedMesh;
                    Material[] mts = ld[i].renderers[0].sharedMaterials;
                    LOD[i] = new MeshLOD(msh, mts);
                }
            }

            for (int i = 0; i < LOD.Length; i++)
            {
                LOD[i].VerifyMesh();
                if(LOD[i].valid){
                    MaxSubMeshCount = Mathf.Max(LOD[i].mesh.subMeshCount, MaxSubMeshCount);
                }
            }

            //Mesh data
            List<GraphicsBuffer.IndirectDrawIndexedArgs> arguments = new List<GraphicsBuffer.IndirectDrawIndexedArgs>();
            for (int i = 0; i < LOD.Length; i++)
            {
                MeshLOD lod = LOD[i];
                arguments.AddRange(lod.InitializeMeshLOD(MaxSubMeshCount, this.name));
            }

            if(LOD.Length > 0 && LOD[0].mesh != null){
                Bounds meshBounds = LOD[0].mesh.bounds;
                float maxExtent = Mathf.Max(meshBounds.extents.x,meshBounds.extents.z);
                float verticalSize = meshBounds.extents.y*MaximumScale;
                MinMeshBounds = meshBounds.center-new Vector3(maxExtent,verticalSize,maxExtent);
                MaxMeshBounds = meshBounds.center + new Vector3(maxExtent,verticalSize,maxExtent); 
            }   
          
            return new ArgumentsData(arguments, LOD, LODLength, MaxSubMeshCount, MaxShadowLOD, ShadowsLODOffset, CastShadows);
        }
              
        private float CalculateLOD(in float dist, in float LodDistance){
            return Mathf.Log((dist + LodDistance) / (LodDistance), 2);
        }

        private ArgumentsData CPUInitalize(){           
            var arguments = new List<GraphicsBuffer.IndirectDrawIndexedArgs>(){
                new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = 0,
                    instanceCount = 0,
                    startIndex = 0,
                    baseVertexIndex = 0,
                    startInstance = 0
                }};

            return new ArgumentsData(arguments, null, 1, 1, 0, 0, false);
        }
        #endregion
        
        public void GizmosDrawDistances(Vector3 position){
            if (DrawDistances)
            {
#if UNITY_EDITOR
                switch (spawnMode)
                {
                    case SpawnMode.GPUInstancing:
                        DrawGPUDistances(position);
                        break;
                    case SpawnMode.CPUInstancing:
                        DrawCPUDistances(position);
                        break;
                }
#endif
            }
        }

        private void DrawGPUDistances(Vector3 position)
        {
            DrawCircleWithName(position, string.Format("View Distance and LOD {0}", LOD.Length-1), ViewDistance, Color.yellow, DrawItAsSpheres);
            if (CrossFadeLODDithering && DrawTransitions)
                DrawCircleWithName(position, "Start Disabling Asset", ViewDistance * 0.75f, new Color(0.7f, 0.52f, 0.016f, 1), DrawItAsSpheres);
            if (CastShadows && DrawShadowBoundaries)
            {
                DrawCircleWithName(position, "Shadow Distance", ShadowDistance, Color.red, DrawItAsSpheres);
                if (CrossFadeLODDithering && DrawTransitions)
                    DrawCircleWithName(position, "Start Disabling Shadow", ShadowDistance * 0.75f, new Color(0.7f, 0, 0.2f, 1), DrawItAsSpheres);

            }
            if (DrawLODBoundaries)
            {
                for (int i = 0; i < LOD.Length - 1; i++)
                {
                    var edge = HighLodDistance * Mathf.Pow(2, i);
                    if (i > 0)
                        edge += HighLodDistance * Mathf.Pow(2, i - 1);
                    DrawCircleWithName(position, string.Format("LOD {0}", i), edge, Color.green, DrawItAsSpheres);
                    if (DrawTransitions && CrossFadeLODDithering)
                    {
                        GetDistanceRangeForLOD(edge, out var min, out var max);
                        DrawCircleWithName(position, string.Format("Fully LOD {0}", i), min, new Color(0, 0.62f, 0.2f, 1), DrawItAsSpheres);
                        DrawCircleWithName(position, string.Format("Fully LOD {0}", i + 1), max, new Color(0, 0.62f, 0.2f, 1), DrawItAsSpheres);
                    }
                }
            }
        }
        private void DrawCPUDistances(Vector3 position)
        {
            DrawCircleWithName(position, string.Format("View Distance and LOD {0}", LOD.Length-1), ViewDistance, Color.yellow, DrawItAsSpheres);

        }
        void GetDistanceRangeForLOD(in float edge, out float distMin, out float distMax) {
            var amountToSwap = edge / 10.0f;
            distMin = edge - amountToSwap;
            distMax = edge + amountToSwap;
        }

        private void DrawCircleWithName(Vector3 position, string labelName, float radiousSize, Color color, bool fullSphere = false)
        {
#if UNITY_EDITOR
            Handles.color = color;
            GUI.color = color;
            var labelPos = position + Vector3.right * radiousSize;
            if (fullSphere)
            {
                Handles.DrawWireDisc(position, Vector3.up, radiousSize);
                Handles.DrawWireDisc(position, Vector3.right, radiousSize);
                Handles.DrawWireDisc(position, Vector3.forward, radiousSize);
            }
            else
                Handles.DrawWireDisc(position, Vector3.up, radiousSize);
            Handles.Label(labelPos, labelName);
#endif
        }

        public VisualElement Preview(bool BigPreview)
        {
#if UNITY_EDITOR
            Object targetObject = null;
            if (spawnMode == SpawnMode.CPUInstancing)
                targetObject = InstanceObject;
            else
            {
                if (LodGroups != null)
                    targetObject = LodGroups;
                else if (LOD != null && LOD.Length > 0)
                    targetObject = LOD[0].mesh;
            }

            if (gameObjectEditor != null && gameObjectEditor.target != targetObject)
            {
                RuntimeTools.CallOnDisableINTERNAL(gameObjectEditor);
                gameObjectEditor = null;
            }

            if (targetObject != null)
            {
                if (BigPreview)
                {
                    return new IMGUIContainer(() => { OnInspectorGUI(targetObject); });
                }
                else
                {
                    var imagePreview = new Image();
                    imagePreview.image = AssetPreview.GetAssetPreview(targetObject);
                    return imagePreview;
                }
            }
#endif
            return null;
        }

        #if UNITY_EDITOR
        Editor gameObjectEditor;
        public void OnInspectorGUI(Object gameObject)
        {
            GUIStyle bgColor = new GUIStyle();
            var rect = GUILayoutUtility.GetRect(200, 200);
            if (gameObject != null)
            {
                if (gameObjectEditor == null)
                    gameObjectEditor = Editor.CreateEditor(gameObject);

                gameObjectEditor.OnInteractivePreviewGUI(rect, bgColor);
            }
        }

        #endif

        private void OnDisable() {
            #if UNITY_EDITOR
            if(gameObjectEditor != null){
                RuntimeTools.CallOnDisableINTERNAL(gameObjectEditor);  
                gameObjectEditor = null;
            }             
            #endif
        }
    }
}
