using UnityEngine;

namespace sapra.InfiniteLands{
    public abstract class ConditionalAttribute : PropertyAttribute
    {
        public string conditionName { get; }
        public ConditionalAttribute(string conditionName) => this.conditionName = conditionName;
    }
}