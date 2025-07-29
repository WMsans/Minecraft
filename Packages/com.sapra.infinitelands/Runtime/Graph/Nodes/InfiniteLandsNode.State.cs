using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public partial class InfiniteLandsNode
    {
        private bool IsReadonly = true;
        public NodeState state;
        public NodeStore store;

        public InfiniteLandsNode GetCopy(bool writeable)
        {
            if (!IsReadonly && writeable)
                Debug.LogError("You are not copying the root node!");
            var copy = (InfiniteLandsNode)MemberwiseClone();
            copy.ResetState();
            copy.IsReadonly = !writeable;
            copy.store = GenericPoolLight<NodeStore>.Get();

            if (!writeable)
            {
                PortsToInputWithNode = new();
                NodesToInput = new();
                NodesInOutput = new();
                PortsToInputList = new();
                InputMode = new();
                Dependencies = new();
            }
            return copy;
        }

        public void Return()
        {
            IsNodeReadOnly();
            store.Release();
            GenericPoolLight<NodeStore>.Release(store);
        }

        public void ResetState()
        {
            state = NodeState.Default;
        }
        private bool IsNodeReadOnly(bool logError = true)
        {
            if (IsReadonly && logError)
                Debug.LogErrorFormat("Working on readonly node {0}:{1}!", this.name, this.guid);
            return IsReadonly;
        }
    }
}