using UnityEditor;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(LocalOutputNode))]
    public class LocalOutputNodeEditor : NodeView
    {
        public override bool TriggerRedrawOnConnection => true;

        public LocalOutputNodeEditor(InfiniteLandsGraphView view, InfiniteLandsNode node) : base(view, node)
        {
        }
    }
}