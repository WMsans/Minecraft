using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(BiomeNode))]
    public class BiomeNodeEditor : NodeView
    {
        private BiomeTree previousTree;
        public BiomeNodeEditor(InfiniteLandsGraphView view, InfiniteLandsNode node) : base(view, node)
        {
            OnPropertyModified += RedrawPorts;
            RegisterCallback<DetachFromPanelEvent>(DeleteView);
            RegisterCallback<AttachToPanelEvent>(NewlyCreated);
        }
        public void DeleteView(DetachFromPanelEvent e)
        {
            BiomeNode castedNode = (BiomeNode)node;
            if(castedNode.biomeTree != null)
                castedNode.biomeTree.OnValuesChangedAfter -= RedrawPorts;
        }
        public void NewlyCreated(AttachToPanelEvent e)
        {
            EditorApplication.delayCall += RedrawPorts;
        }
        public override void AddSelfToElements(InfiniteLandsGraphView view)
        {
            base.AddSelfToElements(view);
            if (DebugOptions.ShowFullComplexGraph) return;

            var biomeNode = node as BiomeNode;
            var tree = biomeNode.biomeTree;
            if (tree == null) return;
            var ndCopies = biomeNode.GetNodeCopies();

            if (ndCopies == null || ndCopies.Count <= 0) return;
            foreach (var node in ndCopies)
            {
                view.AddElementToDictionary(node.guid, this, true);
            }
        }
                
        public void RedrawPorts() {
            var biomeNode = node as BiomeNode;
            if (previousTree != null)
                previousTree.OnValuesChangedAfter -= RedrawPorts;
            if(biomeNode.biomeTree != null)
                biomeNode.biomeTree.OnValuesChangedAfter += RedrawPorts;
            previousTree = biomeNode.biomeTree;
            treeView?.RedrawNode(this);
        }
        
        protected override List<VisualElement> CreateOutputPorts()
        {
            var createdOutputs = new List<VisualElement>();
            var biomeNode = node as BiomeNode;
            var ndCopies = biomeNode.GetNodeCopies();
            if (ndCopies == null || ndCopies.Count <= 0) return createdOutputs;

            var heightNode = ndCopies.OfType<HeightOutputNode>().FirstOrDefault();
            if (heightNode != null)
            {
                var portsGenerated = this.CreatePorts<OutputAttribute>(heightNode, Direction.Output, graph);
                if (portsGenerated.Count > 1)
                {
                    Debug.Log("not made for this");
                }

                foreach (var prtElement in portsGenerated)
                {
                    Port port = prtElement as Port;
                    port.portName = "Output Height";
                    port.style.display = DisplayStyle.Flex;
                }
                createdOutputs.AddRange(portsGenerated);
            }

            var biomeOutputs = ndCopies.OfType<GlobalOutputNode>();
            foreach (var node in biomeOutputs)
            {
                var portsGenerated = this.CreatePorts<OutputAttribute>(node, Direction.Output, graph);
                if (portsGenerated.Count > 1)
                {
                    Debug.Log("not made for this");
                }

                foreach (var prtElement in portsGenerated)
                {
                    Port port = prtElement as Port;
                    port.portName = node.InputName;
                }
                createdOutputs.AddRange(portsGenerated);
            }
            return createdOutputs;
        }
        protected override List<VisualElement> CreateInputPorts()
        {
            List<VisualElement> elements = new();
            var biomeNode = node as BiomeNode;
            var ndCopies = biomeNode.GetNodeCopies();
            if (ndCopies == null || ndCopies.Count <= 0) return elements;

            var globalInputs = ndCopies.OfType<GlobalInputNode>();
            foreach(var node in globalInputs){
                var portsGenerated = this.CreatePorts<InputAttribute>(node, Direction.Input, graph);
                if(portsGenerated.Count > 1){
                    Debug.Log("not made for this");
                }

                foreach(var prtElement in portsGenerated){
                    Port port = prtElement as Port;
                    port.portName = node.OutputName;
                }
                elements.AddRange(portsGenerated);
            }
            return elements;
        }
    }
}