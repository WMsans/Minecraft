using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Local Input", docs ="https://ensapra.com/packages/infinite_lands/nodes/utility/local/local", startCollapsed = true, synonims = new string[]{"Portal"})]
    public class LocalInputNode : InfiniteLandsNode
    {
        [Input, Hide] public object Input;
        [Output(match_type_name: nameof(Input))] public object Output;

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
            return TryGetInputData(branch, out data, nameof(Input));
        }
    }
}