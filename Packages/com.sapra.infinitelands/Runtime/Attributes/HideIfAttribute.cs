using UnityEngine;

namespace sapra.InfiniteLands{
    public class HideIfAttribute : ConditionalAttribute
    {
        public HideIfAttribute(string conditionName) : base(conditionName)
        {
        }
    }
}