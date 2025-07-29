namespace sapra.InfiniteLands{
    public interface IHeightMapConnector
    {
        public void ConnectHeightMap(PathData currentBranch, float ScaleToResolutionRatio, int acomulatedResolution);
    }
}