using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    class LayerAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // One line of  oxygen free code.
            property.intValue = EditorGUI.LayerField(position, label,  property.intValue);
        }
    }
}