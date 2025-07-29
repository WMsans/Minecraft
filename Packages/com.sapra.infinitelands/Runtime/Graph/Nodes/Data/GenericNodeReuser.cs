namespace sapra.InfiniteLands{
    public struct GenericNodeReuser<T, Z> : IReuseObject<Z> 
        where T : InfiniteLandsNode
        where Z : INodeReusable<T>
    {
        private BranchData branch;
        private T node;
        public GenericNodeReuser(BranchData branch, T node){
            this.branch = branch;
            this.node = node;
        }

        public void Reuse(Z instance)
        {
            instance.Reuse(node, branch);
        }
    }
}