using UnityEngine;

namespace sapra.InfiniteLands{
    public class ShowIfAttribute : ConditionalAttribute
    {
        public ShowIfAttribute(string conditionName) : base(conditionName)
        {
        }
    }
}