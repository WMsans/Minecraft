using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands{  
    public static class RuntimeTools
    {
        public static void GetFrustrumPlanes(Camera cam, float ViewDistance, ref Plane[] planes){
            float ogViewDistance = cam.farClipPlane;
            cam.farClipPlane = ViewDistance;//over ? Mathf.Min(ViewDistance, ogViewDistance) : ViewDistance;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            cam.farClipPlane = ogViewDistance;
        }
        
        public static void LogTimings(System.Diagnostics.Stopwatch _watch){
            Debug.Log("<----------- START GENERATION ----------->");
            Debug.Log(string.Format("Chunk generated in: {0} ms, {1} ticks", _watch.ElapsedMilliseconds,
                _watch.ElapsedTicks));
            Debug.Log("<!-------------- THE END ----------------!>");
        }
        
        public static void AdaptiveDestroy(UnityEngine.Object obj) => GameObject.DestroyImmediate(obj); //This is in theory wrong, but otherwise it breaks a lot

        public static GameObject CreateObjectAndRecord(string name){
            var gameObject = new GameObject(name);
/*             #if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(gameObject, string.Format("Created {0}", name));
            #endif */
            return gameObject;
        }
        

        public static GameObject FindOrCreateObject(string name, Transform parent){
            return FindOrCreateObject(name, parent, out _);
        }

        public static GameObject FindOrCreateObject(string name, Transform parent, out bool justCreated){
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    justCreated = false;
                    return child.gameObject;
                }
            }

            var createdObject = CreateObjectAndRecord(name);
            createdObject.transform.SetParent(parent);
            createdObject.transform.localScale = Vector3.one;
            justCreated = true;
            return createdObject.gameObject;
        }

        private static void GetFrustumCorners(Camera cam, ref List<Vector3> FrustrumCorners, ref Vector3[] frustumCornersNear, ref Vector3[] frustumCornersFar)
        {
            FrustrumCorners.Clear();

            // Near plane corners
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersNear);

            // Near plane corners in world space
            for (int i = 0; i < 4; i++)
            {
                frustumCornersNear[i] = cam.transform.TransformPoint(frustumCornersNear[i]);
            }


            // Far plane corners
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersFar);

            // Far plane corners in world space
            for (int i = 0; i < 4; i++)
            {
                frustumCornersFar[i] = cam.transform.TransformPoint(frustumCornersFar[i]);
            }
            FrustrumCorners.AddRange(frustumCornersNear);
            FrustrumCorners.AddRange(frustumCornersFar);
        }

        public static void GetTriangles(Camera cam, ref List<Vector3> frustumCorners, ref Triangle[] frustrumTriangles, ref Vector3[] frustumCornersNear, ref Vector3[] frustumCornersFar)
        {
            GetFrustumCorners(cam, ref frustumCorners, ref frustumCornersNear, ref frustumCornersFar);
            // Define frustum triangles for each side
            
            // Define frustum triangles for each side
            frustrumTriangles[0] = new Triangle(frustumCorners[0], frustumCorners[1], frustumCorners[5]);
            frustrumTriangles[1] = new Triangle(frustumCorners[0], frustumCorners[5], frustumCorners[4]);
            frustrumTriangles[2] = new Triangle(frustumCorners[2], frustumCorners[3], frustumCorners[7]);
            frustrumTriangles[3] = new Triangle(frustumCorners[2], frustumCorners[7], frustumCorners[6]);
            frustrumTriangles[4] = new Triangle(frustumCorners[1], frustumCorners[2], frustumCorners[6]);
            frustrumTriangles[5] = new Triangle(frustumCorners[1], frustumCorners[6], frustumCorners[5]);
            frustrumTriangles[6] = new Triangle(frustumCorners[0], frustumCorners[3], frustumCorners[7]);
            frustrumTriangles[7] = new Triangle(frustumCorners[0], frustumCorners[7], frustumCorners[4]);
            frustrumTriangles[8] = new Triangle(frustumCorners[0], frustumCorners[1], frustumCorners[2]);
            frustrumTriangles[9] = new Triangle(frustumCorners[2], frustumCorners[3], frustumCorners[0]);
            frustrumTriangles[10] = new Triangle(frustumCorners[4], frustumCorners[5], frustumCorners[6]);
            frustrumTriangles[11] = new Triangle(frustumCorners[6], frustumCorners[7], frustumCorners[4]);
        }
        
        public static Type GetTypeFromInputField(string lookingForField, InfiniteLandsNode fromNode, IGraph graph){
            if(fromNode == null)
                return typeof(object);
            var targetInputField = fromNode.GetType().GetField(lookingForField);
            if(targetInputField == null)
                return typeof(object);

            if (targetInputField.GetSimpleField() == typeof(object))
            {
                var connectionToInput = graph.GetAllEdges().FirstOrDefault(a => a.inputPort.fieldName == lookingForField && a.inputPort.nodeGuid == fromNode.guid);
                if (connectionToInput != null)
                    return GetTypeFromOutputField(connectionToInput.outputPort.fieldName, graph.GetNodeFromGUID(connectionToInput.outputPort.nodeGuid), graph);
                return targetInputField.GetSimpleField();
            }
            else
                return targetInputField.GetSimpleField();
        }
        

        public static Type GetTypeFromOutputField(string lookingForField, InfiniteLandsNode fromNode, IGraph graph){
            if(fromNode == null)
                return typeof(object);
            var targetOutputField = fromNode.GetType().GetField(lookingForField);
            if(targetOutputField == null)
                return typeof(object);
                
            var attribute = targetOutputField.GetCustomAttributes().OfType<IMatchInputType>().FirstOrDefault();
            if(attribute != null && targetOutputField.GetSimpleField() == typeof(object)){
                if(attribute.matchingType != "")
                    return GetTypeFromInputField(attribute.matchingType, fromNode, graph);
                return targetOutputField.GetSimpleField();
            }
            else
                return targetOutputField.GetSimpleField();
        }

        public static FieldInfo[] GetInputOutputFields<T>(this Type classType) where T : PropertyAttribute{
            return classType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(a => a.GetCustomAttribute<T>() != null).ToArray();
        }

        public static Type GetSimpleField(this FieldInfo field){
            Type fieldType = field.FieldType;
            if (fieldType.IsArray)
                return field.FieldType.GetElementType();
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) // Handle Lists
                return field.FieldType.GetGenericArguments()[0];
            else
                return field.FieldType;
        }

        public static IEnumerable<Type> FlattenGenericFields(this FieldInfo[] fields)
        {
            return fields.Select(a => GetSimpleField(a));
        }

        #if UNITY_EDITOR
        public static bool IsGameViewOpenAndFocused()
        {
            var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
            foreach (var window in windows)
            {
                if (window.GetType().FullName != "UnityEditor.GameView")
                {
                    continue;
                }

                if (window.hasFocus)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSceneViewOpenAndFocused() {
            var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
            foreach (var window in windows) {
                if (window.GetType().FullName != "UnityEditor.SceneView") {
                    continue;
                }

                if (window.hasFocus) {
                    return true;
                }
            }

            return false;
        }
        public static void CallOnDisableINTERNAL(Editor editorInstance)
        {
            if (editorInstance == null)
            {
                UnityEngine.Debug.LogError("Editor instance is null.");
                return;
            }

            Type editorType = typeof(Editor);

            // Get the internal method "OnDisableINTERNAL"
            MethodInfo method = editorType.GetMethod("OnDisableINTERNAL", BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                UnityEngine.Debug.LogError("OnDisableINTERNAL method not found.");
                return;
            }

            // Invoke the method on the given Editor instance
            method.Invoke(editorInstance, null);
        }
        #endif
    }
}