using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public partial class InfiniteLandsNode
    {
        #region Inputs
        public bool TryGetInputData<T>(BranchData branch, out T data, string fieldName)
        {
            IsNodeReadOnly();
#if UNITY_EDITOR
            processTimes.Stop();
#endif
            var portsToInput = GetPortsToInput(fieldName);
            var length = portsToInput.Length;
            if (length != 1)
            {
                data = default;
                if(length > 1)
                    Debug.LogError("Multiple ports to input!");
                
                return true;
            }

            var targetPort = portsToInput[0];
            if (!ProcessDependency(branch, targetPort))
            {
                data = default;
                return false;
            }
#if UNITY_EDITOR
            processTimes.Start();
#endif
            return TryGetInputData(branch, targetPort, out data);
        }

        public bool TryGetInputData<T>(BranchData branch, ref List<T> data, string fieldName)
        {
            IsNodeReadOnly();
#if UNITY_EDITOR
            processTimes.Stop();
#endif
            var ports = GetPortsToInput(fieldName);
            data.Clear();
            for (int i = 0; i < ports.Length; i++)
            {
                data.Add(default);
            }

            for (int i = 0; i < ports.Length; i++)
            {
                if (!ProcessDependency(branch, ports[i])) return false;
                if (TryGetInputData(branch, ports[i], out T result))
                {
                    var index = ports[i].localPort.listIndex;
                    data[index] = result;
                }
                else
                    return false;
            }

#if UNITY_EDITOR
            processTimes.Start();
#endif
            return true;
        }

        private bool TryGetInputData<T>(BranchData branch, CachedPort targetPort, out T data)
        {
            var nodeCopy = branch.GetWriteableNode(targetPort.node);
            return nodeCopy.TryGetOutputData(branch, out data, targetPort.originPort.fieldName, targetPort.originPort.listIndex);
        }

        private bool ProcessDependency(BranchData branch, CachedPort port)
        {
            var writeableNode = branch.GetWriteableNode(port.node);
            return writeableNode.ProcessNodeInternal(branch);
        }

        #endregion

        #region Outputs
        public virtual bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            IsNodeReadOnly();
            if (!state.completed)
            {
                Debug.LogErrorFormat("The node {0} with Guid: {1} is not completed. Stays at {2}, but you are requesting data from it", this.name, this.guid, state.state);
            }

            if (listIndex < 0)
                return store.TryGetData(fieldName, out data);
            else
                return store.TryGetData(fieldName, listIndex, out data);
        }

        protected void CacheOutputValue<T>(T data, string fieldName)
        {
            store.AddData(fieldName, data);
        }

        protected void CacheOutputValue<T>(List<T> data, string fieldName)
        {
            store.AddData<T>(fieldName, data);
        }
        #endregion
    }
}