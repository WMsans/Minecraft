using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    public class SerializedEdge : IRenderSerializableGraph
    {
        public EdgeConnection connection;
        public SerializedEdge(EdgeConnection edge)
        {
            connection = edge;
        }
        public object GetDataToSerialize() => connection;
        public string GetGUID()=>connection.GetHashCode().ToString();
        public Vector2 GetPosition() => Vector2.zero;
    }
}