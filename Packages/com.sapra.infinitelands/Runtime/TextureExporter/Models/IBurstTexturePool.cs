using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IBurstTexturePool{
        public List<BurstTexture> GetTexture(string[] names, FilterMode filter, TextureFormat format = TextureFormat.RGBA32);
        public List<BurstTexture> GetTexture(string name, FilterMode filter, TextureFormat format = TextureFormat.RGBA32);
        public BurstTexture GetUnpooledTexture(string name, FilterMode filter, TextureFormat format = TextureFormat.RGBA32);

        public void Return(List<BurstTexture> texs);
        public void DestroyBurstTextures(Action<UnityEngine.Object> Destroy);

        public int GetTextureResolution();
    }
}