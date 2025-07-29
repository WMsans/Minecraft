using System.Reflection;
using sapra.InfiniteLands.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands{
    public abstract class OutputPreview
    {
        public PortData PortData{get; protected set;}
        public InfiniteLandsNode Node;
        public NodeView NodeView;
        public OutputPreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView){
            PortData = targetPort;
            Node = node;
            NodeView = nodeView;
        }
        /// <summary>
        /// Generated VisualElement representing the preview
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public abstract VisualElement GetPreview(BranchData settings, GraphSettings graphSettings);
        /// <summary>
        /// Is the preview valid. If not true, it will not be selectable
        /// </summary>
        /// <returns></returns>
        public abstract bool ValidPreview();

        /// <summary>
        /// Allows to stack multiple visuals on top of each other
        /// </summary>
        /// <param name="container"></param>
        /// <param name="visualElement"></param>
        /// <returns></returns>                                
        protected VisualElement StackVisuals(VisualElement container, VisualElement visualElement)
        {
            // Style each preview to fill the container completely
            visualElement.style.position = Position.Absolute;
            visualElement.style.top = 0;
            visualElement.style.left = 0;
            visualElement.style.width = new StyleLength(Length.Percent(100));
            visualElement.style.height = new StyleLength(Length.Percent(100));

            // Add the preview to the container (later ones appear on top)
            container.Add(visualElement);
            return container;
        }
        /// <summary>
        /// Allows to stack multiple visuals from top to bottom in the left corner of the preview
        /// </summary>
        /// <param name="container"></param>
        /// <param name="visualElement"></param>
        /// <returns></returns>
        protected VisualElement StackVisualRight(VisualElement container, VisualElement visualElement)
        {
            // Style each preview to fill the container completely
            visualElement.style.position = Position.Absolute;
            visualElement.style.top = 0;
            visualElement.style.right = 0;
            visualElement.style.width = new StyleLength(StyleKeyword.Auto);
            visualElement.style.height = new StyleLength(StyleKeyword.Auto);
            // Add the preview to the container (later ones appear on top)
            container.Add(visualElement);
            return container;
        }
    }
}