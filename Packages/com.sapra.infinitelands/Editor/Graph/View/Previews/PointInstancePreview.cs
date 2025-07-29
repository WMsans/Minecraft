// PointInstancePreview.cs
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    [EditorForClass(typeof(PointInstance))]
    public class PointInstancePreview : OutputPreview
    {
        private readonly PointInstanceVisualizer visualizer;
        private readonly PointInstanceVisualizer previousVisualizer;

        public PointInstancePreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView) 
            : base(targetPort, node, nodeView)
        {
            visualizer = new PointInstanceVisualizer(); // Default to white if no color provided
            previousVisualizer = new PointInstanceVisualizer(Color.red, true);
        }
        public PointInstancePreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView, Color pointColor) 
            : base(targetPort, node, nodeView)
        {
            visualizer = new PointInstanceVisualizer(); // Default to white if no color provided
            previousVisualizer = new PointInstanceVisualizer(Color.red, true);
        }

        public override VisualElement GetPreview(BranchData settings, GraphSettings graphSettings)
        {
            if (!Node.isValid) return null;

            var writeableNode = settings.GetWriteableNode(Node);
            var foundData = writeableNode.TryGetOutputData(settings, out PointInstance pointInstanceData, PortData.fieldName,PortData.listIndex);
            if (!foundData) return null;

            VisualElement container = new VisualElement();
            container.style.width = new StyleLength(Length.Percent(100));
            container.style.height = new StyleLength(Length.Percent(100));
            container.style.position = Position.Relative; 
            //If there's height data as input show it
            var inputHeightData = Node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => new { Field = f, Attribute = f.GetCustomAttribute<InputAttribute>() })
                .Where(x => x.Attribute != null && x.Field.GetCustomAttribute<DisabledAttribute>() == null && x.Field.FieldType == typeof(HeightData))
                .Select(x => x.Field)
                .FirstOrDefault();

            if(inputHeightData != null){
                var inputDataHeight = NodeView.graph.GetAllEdges()
                    .Where(a => a.inputPort.nodeGuid.Equals(Node.guid) && a.inputPort.fieldName.Equals(inputHeightData.Name))
                    .FirstOrDefault();
                if(inputDataHeight != null){
                    var targetNodeHeight = NodeView.graph.GetNodeFromGUID(inputDataHeight.outputPort.nodeGuid);
                    HeightDataPreview heightDataPreview = new HeightDataPreview(inputDataHeight.outputPort, targetNodeHeight, NodeView);
        
                    VisualElement element = heightDataPreview.GetPreview(settings, graphSettings, false);
                    if (element != null)
                        StackVisuals(container, element);
                    
                }
            }
            
            if (pointInstanceData.PreviousInstance != null)
            {
                VisualElement previous = previousVisualizer.CreateVisual(pointInstanceData.PreviousInstance, settings, false, graphSettings.MeshScale);
                StackVisuals(container, previous);

            }

            VisualElement current = visualizer.CreateVisual(pointInstanceData, settings, true, graphSettings.MeshScale);
            StackVisuals(container, current);

            var worldPreview = EditorTools.Generate_WorldPreview(NodeView, PortData);
            StackVisualRight(container, worldPreview);
            // If no previous instance, just return the current visual
            return container;
        }

        public override bool ValidPreview() => Node.isValid;
    }
}