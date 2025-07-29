using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{  
    public interface ITextureSettings
    {
        public float GetTextureSize(float MeshScale);
        public int GetObjectByteSize();
        public ComputeBuffer CreateTextureCompute(IEnumerable<ITextureSettings> settingsArray, float MeshScale);
    }
}