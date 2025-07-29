namespace sapra.InfiniteLands{
    public struct DataToManage{
        public IndexAndResolution indexData;
        public ILoadAsset.Operation action;
        public DataToManage(IndexAndResolution indexAndResolution, ILoadAsset.Operation action){
            indexData = indexAndResolution;
            this.action = action;
        }
    }
}