using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper.SerializedCollections;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

    private readonly Dictionary<string, CreateTerrainLayerDelegate> _layerCreators = new();
    private readonly Dictionary<string, float[]> _defaultProperties = new();
    private readonly Dictionary<string, string[]> _propertyNames = new();
#if UNITY_EDITOR
    [VInspector.Button]
#endif
    public void FindAndRegisterLayers()
    {
        var layerTypes = TypeCache.GetTypesDerivedFrom<ITerrainLayer>().Where(t => !t.IsAbstract).ToList();
        foreach (var type in layerTypes)
        {
            var registerMethod = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
            registerMethod.Invoke(null, new object[] { });
        }
        Debug.Log("Successfully registered " + layerTypes.Count + " layer types: ");
    }


    public void Register(string layerTypeName, CreateTerrainLayerDelegate creator, string[] fields, float[] defaultProperties)
    {
        _layerCreators[layerTypeName] = creator;
        _defaultProperties[layerTypeName] = defaultProperties;
        _propertyNames[layerTypeName] = fields;
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

    public string[] GetPropertiesName(string layerTypeName)
    {
        if (_propertyNames.TryGetValue(layerTypeName, out var propertiesNames))
        {
            return propertiesNames;
        }

        return new[] { "" };
    }

    public IEnumerable<string> GetLayerTypeNames()
    {
        return _layerCreators.Keys;
    }
}