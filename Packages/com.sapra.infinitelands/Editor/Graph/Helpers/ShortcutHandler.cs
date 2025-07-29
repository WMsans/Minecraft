using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    public static class ShortcutHandler
    {
        public static void BuildContextualMenu(NodeView nodeView, InfiniteLandsGraphView view, ContextualMenuPopulateEvent evt)
        {
            if (nodeView != null)
            {
                evt.menu.AppendAction("Open Documentation _F1", a => nodeView.OpenDocumentation());
                evt.menu.AppendAction("Copy Node GUID _F2", a => CopyGUID(nodeView));
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("View/Expand Nodes", a => ExpandNodes(view, true));
            evt.menu.AppendAction("View/Collapse Nodes", a => ExpandNodes(view, false));
            evt.menu.AppendSeparator("View/");
            evt.menu.AppendAction("View/Expand Preview", a => ShowPreviews(view));
            evt.menu.AppendAction("View/Collapse Preview", a => HidePreviews(view));
            evt.menu.AppendSeparator("View/");
            evt.menu.AppendAction("View/Fit All _A", a => view.FrameAll());
            evt.menu.AppendAction("View/Fit Selection _Q", a => view.FrameSelection());

/*             if (view.selection.Count > 0) //Still not working as desired
            {
                evt.menu.AppendAction("Reorganize Nodes", a => ReorganizeSelection(view.selection, view.targetGraph));            
            } */
        }

        
        public static void OnShortcutPressed(KeyDownEvent evt, InfiniteLandsGraphView view, List<ISelectable> selection)
        {
            if (evt.keyCode == KeyCode.N && !evt.ctrlKey)
            {
                GraphFactory.ShowNodeCreatorWindow(view, view.MousePosition);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.S && !evt.ctrlKey)
            {
                GraphFactory.CreateStickyNote(view, view.MousePosition);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.G && !evt.ctrlKey)
            {
                GraphFactory.CreateGroup(view, view.MousePosition);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.F1 && selection.Count == 1)
            {
                var selectedElement = selection[0];
                if (selectedElement is NodeView selectedNode)
                {
                    selectedNode.OpenDocumentation();
                }
            }
            else if (evt.keyCode == KeyCode.F2 && selection.Count == 1)
            {
                var selectedElement = selection[0];
                if (selectedElement is NodeView selectedNode)
                {
                    CopyGUID(selectedNode);
                }
            }
            else if (evt.keyCode == KeyCode.Q && selection.Count > 0)
            {
                view.FrameSelection();
            }
        }

        private static void ReorganizeSelection(List<ISelectable> selection, IGraph graph)
        {
            var nodes = selection.OfType<NodeView>();
            var guids = nodes.Select(a => a.GetGUID());
            if (nodes.Count() == 0) return;

            //Find the top most selected nodes
            var edges = graph.GetBaseEdges();
            List<(string, int)> NodesToOrder = new();
            float maxLeftOffset = float.MinValue;
            foreach (var node in nodes)
            {
                var guid = node.GetGUID();
                maxLeftOffset = Mathf.Max(maxLeftOffset, node.GetPosition().x);
                if (edges.Where(e => e.outputPort.nodeGuid == guid)
                        .All(e => !guids.Contains(e.inputPort.nodeGuid)))
                    NodesToOrder.Add((guid, 0));
            }

            //Start getting the furthers level of the incoming nodes
            Dictionary<string, int> nodeAndLevel = new();
            int maxLevel = 0;
            for (int i = 0; i < NodesToOrder.Count; i++)
            {
                var (guid, position) = NodesToOrder[i];
                maxLevel = Mathf.Max(maxLevel, position);
                if (!nodeAndLevel.TryGetValue(guid, out var currentStoredOrder) || currentStoredOrder < position)
                {
                    nodeAndLevel[guid] = position;
                    int targetLevel = position + 1;
                    NodesToOrder.AddRange(edges
                        .Where(a => a.inputPort.nodeGuid == guid && guids.Contains(a.outputPort.nodeGuid))
                        .Select(a => (a.outputPort.nodeGuid, targetLevel)));
                }
            }

            List<string>[] LevelWithNodes = new List<string>[maxLevel + 1];
            nodeAndLevel.GroupBy(nl => nl.Value, nl => nl.Key)
                .ToList()
                .ForEach(g => 
                {
                    LevelWithNodes[g.Key] = g.ToList();
                });

            Dictionary<string, NodeView> nodeViews = new();
            float currentMaxLength = 0;
            float averageY = 0;
            int currentMaxLevel = 0;

            //Find nodes and highest length
            for (int l = 0; l < LevelWithNodes.Length; l++)
            {
                var horizontalPosition = -l * 250 + maxLeftOffset;
                var nodesInLevel = LevelWithNodes[l];
                float totalOffset = 0;
                float currentAverageY = 0;

                foreach (var nodeId in LevelWithNodes[l])
                {
                    var targetNodeView = nodes.FirstOrDefault(a => a.GetGUID() == nodeId);
                    nodeViews.Add(nodeId, targetNodeView);
                    totalOffset += targetNodeView.GetPosition().size.y;
                    currentAverageY += targetNodeView.GetPosition().y;
                }

                if (totalOffset > currentMaxLength)
                {
                    currentMaxLength = totalOffset;
                    currentMaxLevel = l;
                    averageY = currentAverageY;
                }

            }

            averageY /= LevelWithNodes[currentMaxLevel].Count;

            //Align longest
            float currentAmount = -currentMaxLength / 2.0f;
            foreach (var node in LevelWithNodes[currentMaxLevel])
            {
                var nodeView = nodeViews[node];
                nodeView.SetPosition(new Rect(new Vector2(-currentMaxLevel * 250 + maxLeftOffset, currentAmount+averageY), nodeView.GetPosition().size));
                currentAmount += nodeView.GetPosition().size.y;
            }

            //Align items
            AlignItemsThroughConnections(currentMaxLevel + 1, true, maxLeftOffset, LevelWithNodes,graph, nodeViews, edges);
            AlignItemsThroughConnections(currentMaxLevel - 1, false, maxLeftOffset, LevelWithNodes, graph, nodeViews, edges);
        }

        private static void AlignItemsThroughConnections(int start, bool toRight,
            float maxLeftOffset, List<string>[] LevelWithNodes, IGraph graph,
            Dictionary<string, NodeView> nodeViews, IEnumerable<EdgeConnection> edges)
        {
            start = Mathf.Clamp(start, 0, LevelWithNodes.Length-1);
            float end = toRight ? LevelWithNodes.Length : 0;
            HashSet<string> checkedNodes = new();
            for (int l = start; toRight ? l < end : l >= end; l += toRight?1:-1)
            {
                var horizontalPosition = -l * 250 + maxLeftOffset;
                foreach (var node in LevelWithNodes[l])
                {
                    checkedNodes.Clear();
                    var inputConnections = edges.Where(a => GetProperConnection(a, node, toRight));
                    float averagePosition = 0;
                    foreach (var connection in inputConnections)
                    {
                        var port = toRight ? connection.inputPort.nodeGuid : connection.outputPort.nodeGuid;
                        if (checkedNodes.Add(port))
                        {
                            averagePosition += graph.GetNodeFromGUID(port).position.y;
                        }
                    }
                    averagePosition /= checkedNodes.Count;

                    var targetNodeView = nodeViews[node];
                    targetNodeView.SetPosition(new Rect(new Vector2(horizontalPosition, averagePosition), targetNodeView.GetPosition().size));

                }
            }
        }
        private static bool GetProperConnection(EdgeConnection connection, string guid, bool getOutput)
        {
            if (getOutput)
                return connection.outputPort.nodeGuid == guid;
            else
                return connection.inputPort.nodeGuid == guid;
        }

        private static void CopyGUID(NodeView view)
        {
            EditorGUIUtility.systemCopyBuffer = view.GetGUID();
        }

        private static void ExpandNodes(InfiniteLandsGraphView view, bool state)
        {
            if (!state)
                HidePreviews(view);

            var tree = view.targetGraph;
            var baseNodes = tree.GetBaseNodes();
            foreach (var node in baseNodes)
            {
                NodeView nodeView = view.FindNodeView(node.guid);
                nodeView.TriggerExpandedState(state);
            }
        }

        private static void ShowPreviews(InfiniteLandsGraphView view)
        {
            ExpandNodes(view, true);

            foreach (var editorNode in view.nodes)
            {
                var nodeView = editorNode as NodeView;
                var targetPort = nodeView.GeneratedToggles.FirstOrDefault();
                if (targetPort != null)
                {
                    OutputPreview prev = (OutputPreview)targetPort.userData;
                    if (prev != null)
                        nodeView.ChangePreview(prev.PortData);
                }
            }
        }


        private static void HidePreviews(InfiniteLandsGraphView view)
        {
            foreach (var node in view.nodes)
            {
                var nodeView = node as NodeView;
                nodeView.ChangePreview(default);
            }
        }
    }
}