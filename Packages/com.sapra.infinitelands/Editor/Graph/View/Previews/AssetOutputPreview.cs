using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(IAsset))]
    public class AssetOutputPreview : OutputPreview
    {
        public AssetOutputPreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView) : base(targetPort, node, nodeView)
        {
        }

        public override VisualElement GetPreview(BranchData settings, GraphSettings graphSettings){
            FieldInfo field = Node.GetType().GetField(PortData.fieldName);
            IAsset asset = (IAsset)field.GetValue(Node);
            if(asset is IHaveAssetPreview previewer)
                return previewer.Preview(true);
            return null;
        }

        public override bool ValidPreview()
        {
            FieldInfo field = Node.GetType().GetField(PortData.fieldName);
            IAsset asset = (IAsset)field.GetValue(Node);
            return asset != null;
        }
    }
}