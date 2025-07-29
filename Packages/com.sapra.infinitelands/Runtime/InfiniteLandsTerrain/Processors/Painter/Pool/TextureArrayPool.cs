using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class TextureArrayPool
    {
        readonly int sizeX;
        readonly int sizeY;
        readonly int mipCount;
        readonly int texturesLength;
        readonly bool linearMode;
        readonly bool mightVarying;
        private UnityEngine.Pool.ObjectPool<Texture2DArray> arrayPool;

        public void Dispose(){
            if(arrayPool.CountActive > 0)
                Debug.LogErrorFormat("Not all texture arrays have been released: {0}", arrayPool.CountActive);
            arrayPool.Dispose();
        }

        public TextureArrayPool(int height, int width, int mipmapCount, int textureLength, bool mightVary, Action<Texture2DArray> OnDestroy, bool _linearMode){
            sizeX = height;
            sizeY = width;
            mipCount = mipmapCount;
            texturesLength = textureLength;
            linearMode = _linearMode;
            mightVarying = mightVary;

            arrayPool = new UnityEngine.Pool.ObjectPool<Texture2DArray>(CreateNewTexture2DArray, actionOnDestroy: OnDestroy);
        }
        
        
        public Texture2DArray GetConfiguredArray(string name, List<Texture2D> textures){
            Texture2DArray array = arrayPool.Get();
            if(mightVarying)
                return GenerateTextureArrayVarying(array, name, textures);
            else
                return GenerateTextureArrayFixed(array, name, textures);
        }

        public void Release(Texture2DArray array){
            if(array != null)
                arrayPool.Release(array);
        }
        public Texture2DArray CreateNewTexture2DArray(){
            Texture2DArray current;
            #if UNITY_2022_1_OR_NEWER
            current = new Texture2DArray(sizeX, sizeY, texturesLength, TextureFormat.RGBA32, mipCount, linearMode, true);
            #else
            current = new Texture2DArray(sizeX, sizeY, texturesLength, TextureFormat.RGBA32, mipCount, linearMode);
            #endif
            return current;
        }

        private Texture2DArray GenerateTextureArrayVarying(Texture2DArray current, string name, List<Texture2D> textures)
        {
            int Length = textures.Count();
            if (Length != texturesLength){
                Debug.LogErrorFormat("Trying to create an array with {0} textures when it was prepared for {1}", Length, texturesLength);
                return null;
            }                
            current.name = name;
            current.filterMode = FilterMode.Bilinear;

            CommandBuffer commandBuffer = new CommandBuffer { name = "Generate Mips" };
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(sizeX, sizeY){
                useMipMap = true,
                autoGenerateMips = false,
                mipCount = mipCount, 
                sRGB = !linearMode
            };
            RenderTargetIdentifier identifier = new RenderTargetIdentifier(0);

            commandBuffer.GetTemporaryRT(0,descriptor);
            int index = 0;
            foreach(var texture in textures){
                commandBuffer.Blit(texture, identifier);
                commandBuffer.GenerateMips(identifier);
                commandBuffer.CopyTexture(identifier, 0, current, index);
                index++;
            }
            commandBuffer.ReleaseTemporaryRT(0);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            return current;
        }

        private Texture2DArray GenerateTextureArrayFixed(Texture2DArray current, string name, List<Texture2D> textures)
        {
            int Length = textures.Count;
            if (Length != texturesLength){
                Debug.LogErrorFormat("Trying to create an array with {0} textures when it was prepared for {1}", Length, texturesLength);
                return null;
            }                
            current.name = name;

            int index = 0;
            foreach(var texture in textures){
                Graphics.CopyTexture(texture, 0, current, index);
                index++;
            }

            current.filterMode = FilterMode.Bilinear;
            return current;
        }
    }
}