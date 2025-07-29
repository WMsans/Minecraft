using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Global Output", typeof(BiomeTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/global/globaloutput", startCollapsed = true)]
    public class GlobalOutputNode : InfiniteLandsNode
    {
        [HideInInspector] public string InputName = "Output";
        [Input(namefield: nameof(InputName))] public object Input;
        [Output(match_type_name: nameof(Input)), Disabled] public object Default;

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
    }
}