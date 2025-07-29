using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System.Linq;

namespace sapra.InfiniteLands
{
    [CustomNode("Texture", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/texture", synonims = new string[]{"Import"})]
    public class TextureNode : InfiniteLandsNode
    {
        public enum Channel{R,G,B,A}
        [Output] public HeightData Output;
        public Texture2D texture;
        public Vector2 MinMaxHeight = new Vector2(0,1);
        public Vector2 Origin = Vector2.zero;

        public float Size = 200;
        public Channel TextureChannel = Channel.A;
        public override bool ExtraValidations()
        {
            return texture != null;
        }

        [Input, Hide] public GridData Grid;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Grid, nameof(Grid));
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            var factory = new TextureDataFactory(this);
            ArrawyWrap<float> textureAlphaData = branch.GetOrCreateGlobalData<ArrawyWrap<float>, TextureDataFactory>(this.guid, ref factory);
            int resolution = texture.width;
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var targetMap);
            Vector3 origin = new Vector3(Origin.x, 0, Origin.y);
            JobHandle job = TextureJob.ScheduleParallel(Grid.meshGrid, Grid.Resolution, targetMap,
                textureAlphaData.values, resolution,
                branch.terrain.Position - origin, MinMaxHeight, targetSpace, Size,
                Grid.jobHandle);

            Output = new HeightData(job, targetSpace, MinMaxHeight);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }

        private struct TextureDataFactory : IFactory<ArrawyWrap<float>>
        {
            private TextureNode textureNode;
            private int width;
            private int height;
            private Channel TextureChannel;
            public TextureDataFactory(TextureNode textureNode){
                this.textureNode = textureNode;
                width = textureNode.texture.width;
                height = textureNode.texture.height;
                TextureChannel = textureNode.TextureChannel;
            }
            
            public ArrawyWrap<float> Create()
            {
                RenderTexture renderTex = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

                Graphics.Blit(textureNode.texture, renderTex);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;
                Texture2D readableText = new Texture2D(width, height);
                readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableText.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);

                var pixels = readableText.GetPixels(0);
                var texChan = TextureChannel;
                RuntimeTools.AdaptiveDestroy(readableText);
                var PixelData = pixels.Select(a => {
                    switch(texChan){
                        case Channel.R:
                            return a.r;
                        case Channel.G:
                            return a.g;
                        case Channel.B:
                            return a.b;
                        default:
                            return a.a;
                    }
                }).ToArray();
                return new ArrawyWrap<float>(new NativeArray<float>(PixelData, Allocator.Persistent));
            }
        }
    }
}