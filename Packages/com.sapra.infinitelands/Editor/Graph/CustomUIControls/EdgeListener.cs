using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace sapra.InfiniteLands.Editor
{
    public class CustomEdgeConnectorListener : IEdgeConnectorListener
    {
        public InfiniteLandsGraphView View;
        public CustomEdgeConnectorListener(InfiniteLandsGraphView View)
        {
            this.View = View;
        }
        public void OnDrop(GraphView graphView, Edge edge)
        {
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            GraphFactory.ShowNodeWindowAutoConnect(View, position, edge);
        }
        
    }

}