using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [CustomPropertyDrawer(typeof(DisabledAttribute))]
    public class DisabledAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement prop = new PropertyField(property);
            prop.SetEnabled(false);
            return prop;
        }
    }
}