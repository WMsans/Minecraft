using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands
{
    [RequireComponent(typeof(Camera))]
    public class CreateDisplacementTexture : MonoBehaviour
    {
        public static int displaceTextureID = Shader.PropertyToID("_DisplaceTexture"),
            displaceTexturePositionID = Shader.PropertyToID("_DisplaceTexturePosition"),
            displaceTextureBoundary = Shader.PropertyToID("_DisplaceTextureBoundary"),
            displaceID = Shader.PropertyToID("_DisplaceSize");

        private RenderTexture DisplaceTexture;
        private Material SmootherMaterial;
        static int ResolutionPerDistance = 10;

        private bool Visualize;


        private bool Initalized = false;
        private Transform TargetPosition;
        private Camera Cam;

        private static Vector2 FromTo = new Vector2(-20, 20);
        private static float Offset = 20;
        private PointStore store;

        public void Initialize(IControlTerrain infiniteLandsController, Transform player, LayerMask cullMask, float displaceDistance, bool RenderToDisplay)
        {
            if(GraphicsSettings.defaultRenderPipeline != null)
                return;
            this.store = infiniteLandsController.GetInternalComponent<PointStore>();
            TargetPosition = player;

            Shader smootherShader = Resources.Load<Shader>("Shaders/Others/TextureCleaner");
            SmootherMaterial = new Material(smootherShader);

            Visualize = RenderToDisplay;
            int resolution = Mathf.CeilToInt(ResolutionPerDistance * displaceDistance);
            
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(resolution,
                resolution, RenderTextureFormat.ARGB32, 8, 0, RenderTextureReadWrite.Linear)
            {
/*                 #if UNITY_6000_0_OR_NEWER
                graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.None, GraphicsFormatUsage.Linear)
                #endif       */     
            };
            Clear();

            DisplaceTexture = new RenderTexture(descriptor);
            DisplaceTexture.name = "DisplacementTexture";

            Cam = GetComponent<Camera>();
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.5f, 1, 0.5f, 0);
            Cam.orthographicSize = displaceDistance;
            Cam.nearClipPlane = 1;
            Cam.farClipPlane = FromTo.y - FromTo.x;
            Cam.cullingMask = cullMask;

            transform.eulerAngles = new Vector3(90, 0, 0);

            Shader.SetGlobalFloat(displaceID, displaceDistance);
            Shader.SetGlobalTexture(displaceTextureID, DisplaceTexture);

            if (!Visualize)
                Cam.targetTexture = DisplaceTexture;
            Initalized = true;
        }

        private void FixedUpdate()
        {
            if (!Initalized || store == null)
                return;

            if (!store.GetCoordinateDataAt(TargetPosition.position, out CoordinateData currentPosition))
                return;
                
            Vector3 targetPosition = new Vector3(TargetPosition.position.x, 
                currentPosition.position.y + Offset,
                TargetPosition.position.z);

            float pixelGrid = ResolutionPerDistance / 2.0f;
            targetPosition *= pixelGrid;
            targetPosition = Vector3Int.FloorToInt(targetPosition);
            targetPosition /= pixelGrid;
            transform.position = targetPosition;
            Shader.SetGlobalVector(displaceTexturePositionID, targetPosition);
            Shader.SetGlobalVector(displaceTextureBoundary,
                new Vector2(targetPosition.y + FromTo.x - Offset, targetPosition.y + FromTo.y - Offset));
        }

        private void OnDisable()
        {
            Clear();
        }

        void Clear(){
            if(Cam != null)
                Cam.targetTexture = null;
            if (DisplaceTexture){
                DisplaceTexture.Release();
                AdaptiveDestroy(DisplaceTexture);
            }
        }
        private void AdaptiveDestroy(UnityEngine.Object obj) {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}