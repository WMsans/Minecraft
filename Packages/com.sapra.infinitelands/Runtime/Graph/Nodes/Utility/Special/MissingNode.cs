namespace sapra.InfiniteLands
{
    [CustomNode("MISSING", canCreate = false, canDelete = true, docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/missingnode.html", customType = "MISSING")]
    public class MissingNode : InfiniteLandsNode
    {
        public override bool ExtraValidations()
        {
            return false;
        }
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
    }
}