using Unity.Collections;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct BurstTexture
    {
        private int res;
        private Texture2D finalTexture;
        private bool applied;
        public string name => finalTexture.name;
        public TextureFormat format{get; private set;}
        public BurstTexture(int resolution, string name, FilterMode filter, TextureFormat format = TextureFormat.RGBA32)
        {
            res = resolution + 1;
            this.format = format;
            finalTexture = new Texture2D(res, res, format, false, linear: true)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            applied = false;
        }

        public Texture2D ApplyTexture()
        {
            if(!applied && finalTexture != null){
                finalTexture.Apply();
                applied = true;
            }
            return finalTexture;
        }

        public void ReuseTexture(string newName)
        {
            this.finalTexture.name = newName;
            this.applied = false;
        }

        public NativeArray<T> GetRawData<T>() where T : struct
        {
            return finalTexture.GetRawTextureData<T>();
        }
    }
}