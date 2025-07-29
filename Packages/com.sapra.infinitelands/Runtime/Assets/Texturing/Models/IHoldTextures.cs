using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{  
    // Contains the necessary textures used for texturing
    public interface IHoldTextures{
        public string name{get;}
        public List<TextureData> GetTextures();
        public ITextureSettings GetSettings(); // Return the settings for the textures
    }
}