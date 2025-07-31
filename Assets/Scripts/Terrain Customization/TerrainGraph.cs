using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "New Terrain Graph", menuName = "Terrain/Terrain Graph")]
public unsafe class TerrainGraph : ScriptableObject
{
    public List<NodeData> nodes = new List<NodeData>();
    public List<EdgeData> edges = new List<EdgeData>();

    public NodeData rootNode;

    public List<string> layerTypeNames = new List<string>();

    public T CreateLayer<T>(string layerTypeName, params float[] properties) where T: struct
    {
        var type = Type.GetType(layerTypeName);
        if (type != null)
        {
            var createMethod = type.GetMethod("Create", new[] { typeof(float[]) });
            if (createMethod != null)
            {
                return (T)createMethod.Invoke(null, new object[] { properties });
            }
        }
        throw new Exception($"Unknown terrain layer type: {layerTypeName}");
    }
    
    public float[] GetDefaultProperties(string layerTypeName)
    {
        var type = Type.GetType(layerTypeName);
        if (type != null)
        {
            var createMethod = type.GetMethod("Create", new Type[0]);
            if (createMethod != null)
            {
                var layer = (TerrainLayer)createMethod.Invoke(null, null);
                var props = new float[16];
                for (int i = 0; i < 16; i++)
                {
                    props[i] = layer.properties[i];
                }
                return props;
            }
        }
        return new float[16];
    }

    public string[] GetPropertyNames(string layerTypeName)
    {
        var type = Type.GetType(layerTypeName);
        if (type != null)
        {
            var fieldsMethod = type.GetMethod("Fields");
            if (fieldsMethod != null)
            {
                return (string[])fieldsMethod.Invoke(null, null);
            }
        }
        return new[] { "" };
    }

    public IEnumerable<string> GetLayerTypeNames()
    {
        return layerTypeNames;
    }

#if UNITY_EDITOR
    public void FindAndRegisterLayers()
    {
        layerTypeNames.Clear();
        var terrainLayerTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<ITerrainLayer>().Where(t => !t.IsAbstract && !t.IsGenericType);
        var heightmapLayerTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<IHeightmapLayer>().Where(t => !t.IsAbstract && !t.IsGenericType);
        
        foreach (var type in terrainLayerTypes.Concat(heightmapLayerTypes))
        {
            layerTypeNames.Add(type.AssemblyQualifiedName);
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class EdgeData
{
    public string outputNodeGuid;
    public string inputNodeGuid;
}