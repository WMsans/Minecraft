using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [CustomPropertyDrawer(typeof(HideAttribute))]
    public class HideAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement hidden = new VisualElement();
            hidden.style.display = DisplayStyle.None;
            return hidden;
        }
    }
}