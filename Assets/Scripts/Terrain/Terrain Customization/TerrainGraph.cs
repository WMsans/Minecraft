using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Terrain Graph", menuName = "Terrain/Terrain Graph")]
public class TerrainGraph : ScriptableObject
{
    public List<NodeData> nodes = new List<NodeData>();
    public List<EdgeData> edges = new List<EdgeData>();

    // You can set a root node to define the start of the generation sequence.
    // This can be extended to be set visually in the graph editor.
    public NodeData rootNode;
}

[System.Serializable]
public class EdgeData
{
    public string outputNodeGuid;
    public string inputNodeGuid;
}