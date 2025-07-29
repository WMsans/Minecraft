using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Reflection;

namespace sapra.InfiniteLands.Editor
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    class EnableIfAttributeDrawer : ConditionalPropertyDrawer
    {
        protected override void ApplyCondition(VisualElement element, bool value)
        {
            element.SetEnabled(value);
        }
    }
}