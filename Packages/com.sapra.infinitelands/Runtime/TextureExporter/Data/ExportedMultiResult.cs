using System.Collections.Generic;
using Unity.Jobs;

namespace sapra.InfiniteLands{
    public readonly struct ExportedMultiResult{
        public readonly IBurstTexturePool Pool;
        public readonly JobHandle job;
        public readonly List<BurstTexture> textures;
        public ExportedMultiResult(List<BurstTexture> textures, IBurstTexturePool pool, JobHandle job){
            this.job = job;
            this.textures = textures;
            this.Pool = pool;
        }
        public void Return(){
            if(textures != null && textures.Count > 0)
                Pool.Return(textures);
        }
    }
}