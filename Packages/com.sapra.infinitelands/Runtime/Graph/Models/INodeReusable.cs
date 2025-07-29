namespace sapra.InfiniteLands{
    public interface INodeReusable<N> where N : InfiniteLandsNode{
        public void Reuse(N node, BranchData branch);    
    }
}