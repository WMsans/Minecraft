using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class LandmarkManager 
    {
        public GameObject prefab;
        public Transform parent;
        public List<GameObject> enabledObjects = new();
        public bool hasToAddAlignment;
        public bool hasToAddFloatingPoint;

        public LandmarkManager(GameObject prefab, Transform parent, bool AlignWithTerrain, bool FloatingPoint){
            this.prefab = prefab;
            this.parent = parent;

            hasToAddAlignment = AlignWithTerrain && prefab.GetComponent<AlignWithTerrain>() == null;
            hasToAddFloatingPoint = FloatingPoint && prefab.GetComponent<FloatingPoint>() == null;

            if(hasToAddAlignment)
                Debug.LogWarningFormat("{0} wants to be aligned with terrain, but doesn't contain an AlignWithTerrain component. Automatically adding one, but it's recommended to add one to the prefab to improve performance",prefab.name);
            
            if(hasToAddFloatingPoint)
                Debug.LogWarningFormat("{0} should contain a FloatingPoint component but doesn't Automatically adding one, but it's recommended to add one to the prefab to improve performance",prefab.name);
        }

        public void CreateObject(PointTransform pointTransform, Matrix4x4 localToWorldMatrix){
            pointTransform.MultiplyMatrix(localToWorldMatrix, prefab.transform.localScale, prefab.transform.eulerAngles,
                out Vector3 position, out Quaternion rotation, out Vector3 finalScale);

            var generatedInstance = GameObject.Instantiate(prefab, position, rotation, parent);
            generatedInstance.transform.localScale = finalScale;
            if(hasToAddAlignment)
                generatedInstance.AddComponent<AlignWithTerrain>();
            
            if(hasToAddFloatingPoint)
                generatedInstance.AddComponent<FloatingPoint>();
            enabledObjects.Add(generatedInstance);
        }

        public void Disable(){
            if(parent != null)
                RuntimeTools.AdaptiveDestroy(parent.gameObject);
            foreach(GameObject gameObject in enabledObjects){
                if(gameObject != null)
                    RuntimeTools.AdaptiveDestroy(gameObject);
            }
        }
            
    }
}