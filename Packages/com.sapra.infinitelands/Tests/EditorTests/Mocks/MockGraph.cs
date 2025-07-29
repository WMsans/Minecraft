using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands.Tests{
    public class MockGraph : IGraph
    {
        int current_count = 0;

        public string name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action OnValuesChangedBefore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action OnValuesChangedAfter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool _autoUpdate => throw new NotImplementedException();

        public bool AddConnection(EdgeConnection connection, bool deleteOtherConnections = true)
        {
            throw new NotImplementedException();
        }

        public void AddElementsToGroup(GroupBlock group, IEnumerable<string> guids)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFromTo(InfiniteLandsNode from, InfiniteLandsNode to)
        {
            throw new NotImplementedException();
        }

        public GroupBlock CreateGroup(string name, Vector2 position, List<string> elementsGUIDS)
        {
            throw new NotImplementedException();
        }

        public GroupBlock CreateGroupFromJson(string JsonData, Vector2 position)
        {
            throw new NotImplementedException();
        }

        public InfiniteLandsNode CreateNode(Type type, Vector2 position, bool record = true)
        {
            throw new NotImplementedException();
        }

        public InfiniteLandsNode CreateNodeFromJson(Type type, string JsonData, Vector2 position)
        {
            throw new NotImplementedException();
        }

        public StickyNoteBlock CreateStickyNote(Vector2 position)
        {
            throw new NotImplementedException();
        }

        public StickyNoteBlock CreateStickyNoteFromJson(string JsonData, Vector2 position)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<EdgeConnection> GetAllEdges()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InfiniteLandsNode> GetAllNodes()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<EdgeConnection> GetBaseEdges()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InfiniteLandsNode> GetBaseNodes()
        {
            throw new NotImplementedException();
        }

        public InfiniteLandsNode GetNodeFromGUID(string guid)
        {
            throw new NotImplementedException();
        }

        public int GetNodeIndex(InfiniteLandsNode node)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InfiniteLandsNode> GetNodesFromGUID(IEnumerable<string> guids)
        {
            throw new NotImplementedException();
        }

        public HeightOutputNode GetOutputNode()
        {
            throw new NotImplementedException();
        }

        public int GetUniqueIndex()
        {
            var tmp = current_count;
            current_count++;
            return tmp;
        }

        public IEnumerable<InfiniteLandsNode> GetValidNodes()
        {
            throw new NotImplementedException();
        }

        public void NotifyValuesChangedAfter()
        {
            throw new NotImplementedException();
        }

        public void NotifyValuesChangedBefore()
        {
            throw new NotImplementedException();
        }

        public void RecordAction(string action)
        {
            throw new NotImplementedException();
        }

        public void RemoveConnection(EdgeConnection connection)
        {
            throw new NotImplementedException();
        }

        public void ValidationCheck()
        {
            throw new NotImplementedException();
        }
    }
}