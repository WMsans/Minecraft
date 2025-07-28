using System;
using UnityEngine;

[System.Serializable]
public class NodeData
{
    public string guid;
    public string layerType;
    public Vector2 position;
    public float[] properties;

    public NodeData(string guid, string layerType, Vector2 position, int propertyCount)
    {
        this.guid = guid;
        this.layerType = layerType;
        this.position = position;
        this.properties = new float[propertyCount];
    }
}