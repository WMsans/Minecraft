using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class TerrainLayerRegistry : ScriptableObject
{
    private static TerrainLayerRegistry instance;

    public static TerrainLayerRegistry Instance
    {
        get
        {
            if (instance) return instance;
            instance = Resources.Load<TerrainLayerRegistry>("TerrainLayerRegistry");
            if (!instance)
            {
                Debug.LogError("Cannot find TerrainLayerRegistry in resources folder");
                return null;
            }

            return instance;
        }
    }

    public delegate TerrainLayer CreateTerrainLayerDelegate(params float[] properties);

    private readonly Dictionary<string, CreateTerrainLayerDelegate> _layerCreators = new Dictionary<string, CreateTerrainLayerDelegate>();
    private readonly Dictionary<string, float[]> _defaultProperties = new Dictionary<string, float[]>();

    [VInspector.Button]
    public void FindAndRegisterLayers()
    {
        var layerTypes = TypeCache.GetTypesDerivedFrom<ITerrainLayer>().Where(t => !t.IsAbstract);
        foreach (var type in layerTypes)
        {
            var registerMethod = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
            registerMethod.Invoke(null, new object[] { });
        }
        Debug.Log("Successfully registered " + layerTypes.Count() + " layer types: ");
    }

    public void Register(string layerTypeName, CreateTerrainLayerDelegate creator, float[] defaultProperties)
    {
        _layerCreators[layerTypeName] = creator;
        _defaultProperties[layerTypeName] = defaultProperties;
    }

    public TerrainLayer CreateLayer(string layerTypeName, params float[] properties)
    {
        if (_layerCreators.TryGetValue(layerTypeName, out var creator))
        {
            if (properties == null || properties.Length == 0)
            {
                properties = GetDefaultProperties(layerTypeName);
            }
            return creator(properties);
        }
        throw new Exception($"Unknown terrain layer type: {layerTypeName}");
    }
    
    public float[] GetDefaultProperties(string layerTypeName)
    {
        if (_defaultProperties.TryGetValue(layerTypeName, out var properties))
        {
            return properties;
        }
        return new float[16];
    }

    public IEnumerable<string> GetLayerTypeNames()
    {
        return _layerCreators.Keys;
    }
}