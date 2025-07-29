using UnityEditor;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    class HideIfAttributeDrawer : ConditionalPropertyDrawer
    {
        protected override void ApplyCondition(VisualElement element, bool value)
        {
            element.style.display = !value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}