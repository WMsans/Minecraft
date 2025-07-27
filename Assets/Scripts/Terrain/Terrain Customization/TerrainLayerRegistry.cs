using System;
using System.Collections.Generic;

public static class TerrainLayerRegistry
{
    public delegate TerrainLayer CreateTerrainLayerDelegate(params float[] properties);

    private static readonly Dictionary<string, CreateTerrainLayerDelegate> _layerCreators = new Dictionary<string, CreateTerrainLayerDelegate>();
    private static readonly Dictionary<string, float[]> _defaultProperties = new Dictionary<string, float[]>();

    public static void Register(string layerTypeName, CreateTerrainLayerDelegate creator, float[] defaultProperties)
    {
        _layerCreators[layerTypeName] = creator;
        _defaultProperties[layerTypeName] = defaultProperties;
    }

    public static TerrainLayer CreateLayer(string layerTypeName, params float[] properties)
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
    
    public static float[] GetDefaultProperties(string layerTypeName)
    {
        if (_defaultProperties.TryGetValue(layerTypeName, out var properties))
        {
            return properties;
        }
        return new float[16];
    }

    public static IEnumerable<string> GetLayerTypeNames()
    {
        return _layerCreators.Keys;
    }
}