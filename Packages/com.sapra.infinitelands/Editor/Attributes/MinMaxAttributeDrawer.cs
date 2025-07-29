using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands{
[CustomPropertyDrawer(typeof(MinMaxAttribute))]
    public class MinMaxAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            string nameReady = ObjectNames.NicifyVariableName(property.name);
            var attrib = (MinMaxAttribute)attribute;
            var propertyField = new VisualElement();

            var slider = new MinMaxSlider(nameReady, attrib.minValue, attrib.maxValue, attrib.minValue, attrib.maxValue)
            {
                bindingPath = property.propertyPath
            };
            Vector2Field value = new Vector2Field()
            {
                label = "",
                bindingPath = property.propertyPath,
            };

            value.RegisterValueChangedCallback(a=>{
                Vector2 clamped = new Vector2(Mathf.Max(attrib.minValue, a.newValue.x), Mathf.Min(attrib.maxValue, a.newValue.y));
                value.SetValueWithoutNotify(clamped);

            });

            propertyField.Add(slider);
            propertyField.Add(value);

            return propertyField;
        }
        
    }
}