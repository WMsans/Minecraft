using UnityEditor.Searcher;

namespace sapra.InfiniteLands.Editor{
    public class Adapter : SearcherAdapter
    {
        public Adapter(string title) : base(title){}
        public override bool HasDetailsPanel => false;
        public override float InitialSplitterDetailRatio => 0;
    }
}