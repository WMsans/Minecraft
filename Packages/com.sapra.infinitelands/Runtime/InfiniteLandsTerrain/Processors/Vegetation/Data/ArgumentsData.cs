using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{  
    public class ArgumentsData{
        public readonly List<GraphicsBuffer.IndirectDrawIndexedArgs> Arguments;
        public readonly int MaxSubMeshCount;
        public readonly int LODLength;
        public readonly bool CastShadows;
        public readonly int MaxShadowLOD;
        public readonly int ShadowLODOffset;
        public readonly MeshLOD[] Lods;

        public readonly bool ValidArguments;
        public ArgumentsData(List<GraphicsBuffer.IndirectDrawIndexedArgs> _arguments, MeshLOD[] lods, int lodLenght, 
            int maxSubMeshCount, int maxShadowLOD, int _shadowLODOffset, bool castShadows){
            Arguments = _arguments;
            MaxSubMeshCount = maxSubMeshCount;
            LODLength = lodLenght;
            MaxShadowLOD = maxShadowLOD;
            ShadowLODOffset = _shadowLODOffset;
            CastShadows = castShadows;
            Lods = lods;
            ValidArguments = Lods != null && Arguments.Count > 0;
        }
    }
}