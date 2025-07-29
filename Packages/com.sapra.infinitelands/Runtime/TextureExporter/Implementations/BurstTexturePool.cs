using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class BurstTexturePool : IBurstTexturePool{
        private Dictionary<TextureFormat, List<BurstTexture>> _availableTextures = new Dictionary<TextureFormat, List<BurstTexture>>();

        private List<BurstTexture> _allTextures = new List<BurstTexture>();
        private int _textureResolution;
        public int GetTextureResolution() => _textureResolution;
        public BurstTexturePool(int resolution){
            _textureResolution = resolution;

        }
        public BurstTexture GetUnpooledTexture(string name, FilterMode filter, TextureFormat format= TextureFormat.RGBA32)
        {
            if(!_availableTextures.TryGetValue(format, out List<BurstTexture> textures)){
                textures = new List<BurstTexture>();
                _availableTextures.Add(format, textures);
            }

            BurstTexture newTexture;

            if (textures.Count > 0)
            {
                newTexture = textures[0];
                newTexture.ReuseTexture(name);
                textures.RemoveAt(0);
            }
            else{
                newTexture = new BurstTexture(_textureResolution, name, filter, format);
                _allTextures.Add(newTexture);
            }
            return newTexture;
        }

        public List<BurstTexture> GetTexture(string name, FilterMode filter, TextureFormat format)
        {
            List<BurstTexture> newTextures = ListPoolLight<BurstTexture>.Get();
            newTextures.Add(GetUnpooledTexture(name,filter, format));
            return newTextures;
        }

        public List<BurstTexture> GetTexture(string[] names, FilterMode filter, TextureFormat format)
        {
            List<BurstTexture> newTextures = ListPoolLight<BurstTexture>.Get();
            for (int i = 0; i < names.Length; i++)
            {
                newTextures.Add(GetUnpooledTexture(names[i],filter, format));
            }

            return newTextures;
        }

        private void Return(BurstTexture tex)
        {
            if(!_availableTextures.TryGetValue(tex.format, out List<BurstTexture> textures)){
                textures = new List<BurstTexture>();
                _availableTextures.Add(tex.format, textures);
            }

            textures.Add(tex);
        }

        public void Return(List<BurstTexture> texs)
        {
            if(texs == null)    
                return;
            foreach(BurstTexture texture in texs){
                Return(texture);
            }

            ListPoolLight<BurstTexture>.Release(texs);
        }

        public void DestroyBurstTextures(Action<UnityEngine.Object> Destroy)
        {
/*             foreach(KeyValuePair<TextureFormat, List<BurstTexture>> pair in _availableTextures){
                foreach(BurstTexture tex in pair.Value){
                    Destroy(tex.ApplyTexture());
                }
            }
 */
            foreach(BurstTexture texture in _allTextures){
                Destroy(texture.ApplyTexture());
            }
            _allTextures.Clear();
            _availableTextures.Clear();
        }

    }
}