using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    public interface IAmplifyGraph
    {
        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges);
        public bool Amplified { get; set; }
    }

    public class AmplifyingData
    {
        public List<InfiniteLandsNode> allNodes;
        public List<EdgeConnection> allEdges;

        public IGraph NodeOriginGraph;
        public IGraph AmplifierGraphCaller;
    }
}