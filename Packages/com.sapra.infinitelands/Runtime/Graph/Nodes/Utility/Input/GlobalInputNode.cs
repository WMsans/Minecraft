using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Global Input", typeof(BiomeTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/global/globalinput", startCollapsed = true)]
    public class GlobalInputNode : InfiniteLandsNode/* , IAmplifyGraph */
    {
        [HideInInspector] public string OutputName = "Input";
        [Input] public object Default;
        [Output(match_type_name: nameof(Default), namefield: nameof(OutputName))] public object Output;

        public bool Amplified { get; set; }

        protected override bool Process(BranchData branch)
        {
            return true;
        }
        protected override void CacheOutputValues()
        {
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return true;
        }
        public override bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            return TryGetInputData(branch, out data, nameof(Default));
        }

/*         public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            bool isFromThisGraph = Graph.GetBaseNodes().Contains(this);
            if (!isFromThisGraph)
            {
                bool connectionsToThisInput = Graph.GetBaseEdges().Where(a => a.inputPort.nodeGuid.Contains(guid) && a.inputPort.fieldName == nameof(Default)).Any();
                if (connectionsToThisInput)
                {
                    allEdges.RemoveAll(a => a.inputPort.nodeGuid == guid && a.inputPort.fieldName == nameof(Default));
                }
            }
        } */
    }
}