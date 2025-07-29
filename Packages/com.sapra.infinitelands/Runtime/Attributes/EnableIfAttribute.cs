namespace sapra.InfiniteLands{
    public class EnableIfAttribute : ConditionalAttribute
    {
        public EnableIfAttribute(string conditionName) : base(conditionName)
        {
        }
    }
}