using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public static class AmplifyExtension
    {

        public static bool AmplifyGraphSafe(this IAmplifyGraph amplifyGraph, List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            if (!amplifyGraph.Amplified)
            {
                amplifyGraph.Amplified = true;
                amplifyGraph.AmplifyGraph(allNodes, allEdges);
                return true;
            }
            return false;
        }
    }
}