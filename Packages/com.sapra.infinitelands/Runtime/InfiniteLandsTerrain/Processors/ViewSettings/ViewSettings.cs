using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.ObjectModel;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class ViewSettings : InfiniteLandsComponent
    {
        public bool ForceStayGameMode;
        public bool AutomaticallyFetchCameras = true;
        public bool AutomaticallyFetchTransforms = true;

        public List<Camera> BaseCameras = new();
        public List<Transform> BaseTransforms = new();

        private List<Camera> FetchedCameras = new();
        private List<Transform> FetchedTransforms = new();

        private HashSet<Camera> AllCamerasSet = new();
        private HashSet<Transform> AllTransformsSet = new();

        [SerializeField][Disabled] protected List<Camera> AllCameras = new();
        [SerializeField][Disabled] protected List<Transform> AllTransforms = new();

        public Action<Transform> OnTransformAdded;
        public Action<Transform> OnTransformRemoved;
        public Action<Camera> OnCameraAdded;
        public Action<Camera> OnCameraRemoved;

        private int EditorTargetDisplay;
        private bool GameViewFocused;
        public override void Initialize(IControlTerrain lands)
        {
            base.Initialize(lands);

/* #if UNITY_EDITOR
            EditorApplication.update -= CheckIfEditorChanges;
            EditorApplication.update += CheckIfEditorChanges;
#endif */
            OnTransformAdded = null;
            OnTransformRemoved = null;
            OnCameraAdded = null;
            OnCameraRemoved = null;
            UpdateCamerasAndTransforms();
        }
        #region Public Methods
        public void AddNewTransform(Transform body)
        {
            if (!BaseTransforms.Contains(body))
            {
                BaseTransforms.Add(body);
                RecollectTransforms();
                OnTransformAdded?.Invoke(body);
            }
        }

        public void RemoveTransform(Transform body)
        {
            if (BaseTransforms.Contains(body))
            {
                BaseTransforms.Remove(body);
                RecollectTransforms();
                OnTransformRemoved?.Invoke(body);
            }
        }

        public void AddNewCamera(Camera cam)
        {
            if (!BaseCameras.Contains(cam))
            {
                BaseCameras.Add(cam);
                RecollectCameras();
                OnCameraAdded?.Invoke(cam);
            }
        }

        public void RemoveCamera(Camera cam)
        {
            if (BaseCameras.Contains(cam))
            {
                BaseCameras.Remove(cam);
                RecollectCameras();
                OnCameraRemoved?.Invoke(cam);
            }
        }

        public List<Camera> GetCurrentCameras() => AllCameras;
        public List<Transform> GetCurrentTransforms() => AllTransforms;
        #endregion
        private void UpdateCamerasAndTransforms()
        {
            GetCameras();
            GetAvailableTransforms();

            var prevCameras = new List<Camera>(AllCamerasSet);
            RecollectCameras();
            CameraModification(AllCamerasSet.Except(prevCameras), prevCameras.Except(AllCamerasSet));

            var prevTransforms = new List<Transform>(AllTransformsSet);
            RecollectTransforms();
            TransformModification(AllTransformsSet.Except(prevTransforms), prevTransforms.Except(AllTransformsSet));

        }

        private void RecollectCameras()
        {
            AllCamerasSet.Clear();
            AllCameras.Clear();
            foreach (var cam in FetchedCameras)
            {
                AllCamerasSet.Add(cam);
            }
            foreach (var cam in BaseCameras)
            {
                AllCamerasSet.Add(cam);
            }

            foreach (var cam in AllCamerasSet)
            {
                AllCameras.Add(cam);
            }
        }

        private void RecollectTransforms()
        {
            AllTransformsSet.Clear();
            AllTransforms.Clear();
            foreach (var cam in FetchedTransforms)
            {
                AllTransformsSet.Add(cam);
            }
            foreach (var cam in BaseTransforms)
            {
                AllTransformsSet.Add(cam);
            }

            foreach (var cam in AllTransformsSet)
            {
                AllTransforms.Add(cam);
            }
        }
        private void TransformModification(IEnumerable<Transform> added, IEnumerable<Transform> removed)
        {
            foreach (Transform tr in removed)
            {
                OnTransformRemoved?.Invoke(tr);
            }

            foreach (Transform tr in added)
            {
                OnTransformAdded?.Invoke(tr);
            }
        }

        private void CameraModification(IEnumerable<Camera> added, IEnumerable<Camera> removed)
        {
            foreach (Camera cm in removed)
            {
                OnCameraRemoved?.Invoke(cm);
            }

            foreach (Camera cm in added)
            {
                OnCameraAdded?.Invoke(cm);
            }
        }
        public override void Disable()
        {
/* #if UNITY_EDITOR
            EditorApplication.update -= CheckIfEditorChanges;
#endif */
        }

        private void GetAvailableTransforms()
        {
            FetchedTransforms.Clear();
            if (!AutomaticallyFetchTransforms) return;

            var found = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
                .Select(a => a.transform)
                .Concat(FindObjectsByType<CharacterController>(FindObjectsSortMode.None)
                .Select(a => a.transform));
            FetchedTransforms.AddRange(found);
        }
        #if UNITY_EDITOR
        private void GetCameras(){
            FetchedCameras.Clear();
            if (!AutomaticallyFetchCameras) return;

            IEnumerable<Camera> target;
            //We are in editor mode and the scene is not playing. lets firts find if gameview is enabled

            //If this is the case, we should get the gameview camera
            GameViewFocused = true;
            EditorTargetDisplay = Display.activeEditorGameViewTarget;
            var enabledCameras = Camera.allCameras.Where(a => a.isActiveAndEnabled && a.targetTexture == null);
            target = enabledCameras.Where(a => a.targetDisplay.Equals(EditorTargetDisplay));

            if(!RuntimeTools.IsGameViewOpenAndFocused() && !ForceStayGameMode){
                GameViewFocused = false;
                target = target.Concat(SceneView.GetAllSceneCameras());
            }

            var TextureCameras = Camera.allCameras.Where(a => a.isActiveAndEnabled && a.targetTexture != null);
            FetchedCameras.AddRange(target);
            FetchedCameras.AddRange(TextureCameras);
        }
        private void CheckIfEditorChanges(){
            var target = RuntimeTools.IsGameViewOpenAndFocused();
            bool shouldSwap = target != GameViewFocused;
            var shouldChangeCamera = target && EditorTargetDisplay != Display.activeEditorGameViewTarget;
            if(shouldSwap || shouldChangeCamera){
                UpdateCamerasAndTransforms();
            }
        }
        #else
        private void GetCameras(){
            FetchedCameras.Clear();
            FetchedCameras.AddRange(Camera.allCameras.Where(a => a.isActiveAndEnabled));
        }
        #endif
    }
}