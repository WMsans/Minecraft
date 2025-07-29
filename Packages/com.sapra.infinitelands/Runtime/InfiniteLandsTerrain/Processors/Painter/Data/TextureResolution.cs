namespace sapra.InfiniteLands{
    [System.Serializable]
    public struct TextureResolution{
        public bool UseMaximumResolution;
        [HideIf(nameof(UseMaximumResolution))] public int Width;
        [HideIf(nameof(UseMaximumResolution))] public int Height;
        [HideIf(nameof(UseMaximumResolution))] public int MipCount;
        public static TextureResolution Default => new TextureResolution(){
            UseMaximumResolution = true,
            Width = 256,
            Height = 256,
            MipCount = 10,
        };
    }
}