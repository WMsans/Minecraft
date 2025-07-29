using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public struct DefaultSettings : ITextureSettings
    {
        [SerializeField] private float Size;
        public float NormalStrength;
        public static DefaultSettings Default => new DefaultSettings
        {
            Size = 10,
            NormalStrength = 1,
        };

        public ComputeBuffer CreateTextureCompute(IEnumerable<ITextureSettings> settingsArray, float MeshScale)
        {
            ComputeBuffer assetSettingsBuffer = new ComputeBuffer(settingsArray.Count(),
                GetObjectByteSize(), ComputeBufferType.Default);
            assetSettingsBuffer.SetData(settingsArray.Cast<DefaultSettings>().Select(a => new DefaultSettings(){
                Size = a.GetTextureSize(MeshScale),
                NormalStrength = a.NormalStrength
            }).ToArray());

            return assetSettingsBuffer;
        }

        public int GetObjectByteSize() => sizeof(float) * 2;

        public float GetTextureSize(float MeshScale)
        {
            var ratio = Mathf.RoundToInt(MeshScale/Size);
            return MeshScale/ratio;
        }
    }
}