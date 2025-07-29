namespace sapra.InfiniteLands{  
    public struct InfiniteSettings{
        public int LODLevels;
        public int VisibleChunks;
        public InfiniteSettings(int lodlevels, int visibleChunks){
            this.LODLevels = lodlevels;
            this.VisibleChunks = visibleChunks;
        }
    }
}