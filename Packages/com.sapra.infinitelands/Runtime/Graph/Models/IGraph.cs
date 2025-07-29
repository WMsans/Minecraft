using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IGraph
    {
        public string name{get; set;}
        public IEnumerable<InfiniteLandsNode> GetAllNodes();
        public IEnumerable<InfiniteLandsNode> GetValidNodes();

        public IEnumerable<EdgeConnection> GetAllEdges();
        public IEnumerable<EdgeConnection> GetBaseEdges();
        public IEnumerable<InfiniteLandsNode> GetBaseNodes();

        public InfiniteLandsNode GetNodeFromGUID(string guid);
        public IEnumerable<InfiniteLandsNode> GetNodesFromGUID(IEnumerable<string> guids);
        public int GetNodeIndex(InfiniteLandsNode node);
        public HeightOutputNode GetOutputNode();

        public Action OnValuesChangedBefore{get; set;}
        public Action OnValuesChangedAfter { get; set; }
        public bool _autoUpdate{get;}

        public void ValidationCheck();
        public int GetUniqueIndex();

#if UNITY_EDITOR
        public void NotifyValuesChangedBefore();
        public void NotifyValuesChangedAfter();

        public GroupBlock CreateGroup(string name, Vector2 position, List<string> elementsGUIDS);
        public StickyNoteBlock CreateStickyNote(Vector2 position);
        public InfiniteLandsNode CreateNode(Type type, Vector2 position, bool record = true);

        public StickyNoteBlock CreateStickyNoteFromJson(string JsonData, Vector2 position);
        public GroupBlock CreateGroupFromJson(string JsonData, Vector2 position);
        public InfiniteLandsNode CreateNodeFromJson(Type type, string JsonData, Vector2 position);

        public void CopyDataFromTo(InfiniteLandsNode from, InfiniteLandsNode to);

        public void AddElementsToGroup(GroupBlock group, IEnumerable<string> guids);
        public void RecordAction(string action);

        public bool AddConnection(EdgeConnection connection, bool deleteOtherConnections = true);
        public void RemoveConnection(EdgeConnection connection);
#endif
    }
}