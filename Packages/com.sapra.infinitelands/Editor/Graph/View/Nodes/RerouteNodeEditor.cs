using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(RerouteNode))]
    public class RerouteNodeEditor : NodeView
    {
        public override bool TriggerRedrawOnConnection => true;
        public RerouteNodeEditor(InfiniteLandsGraphView view, InfiniteLandsNode node) : base(view, node)
        {
            var title = this.Q<VisualElement>("title");
            var divider = this.Q<VisualElement>("divider");

            title.RemoveFromHierarchy();
            divider.RemoveFromHierarchy();
        }

        protected override List<VisualElement> CreateInputPorts()
        {
            var created = base.CreateInputPorts();
            foreach(var port in created){
                var label = port.Q<Label>("type");
                label.text = "";
            }
            return created;
        }

        protected override List<VisualElement> CreateOutputPorts()
        {            
            var created = base.CreateOutputPorts();
            foreach(var port in created){
                var label = port.Q<Label>("type");
                label.style.display = DisplayStyle.None;
            }
            return created;
        }
    }
}