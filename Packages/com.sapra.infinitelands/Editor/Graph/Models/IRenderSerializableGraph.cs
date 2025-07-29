using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    /// <summary>
    /// Allows the serialization of data to be copy-pasted
    /// </summary>
    public interface IRenderSerializableGraph{

        /// <summary>
        /// Return the data that can be serialized by JSON
        /// </summary>
        public object GetDataToSerialize();

        /// <summary>
        /// Gets the node position
        /// </summary>        
        public Vector2 GetPosition();

        /// <summary>
        /// Gets the original GUID of the node
        /// </summary>
        public string GetGUID();
    }
}