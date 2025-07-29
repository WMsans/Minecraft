using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Reflection;

namespace sapra.InfiniteLands.Editor
{
    public abstract class ConditionalPropertyDrawer : PropertyDrawer
    {
        protected abstract void ApplyCondition(VisualElement element, bool value);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var attr = (ConditionalAttribute)attribute;
            var propField = new PropertyField(property);
            container.Add(propField);

            object target = EditorTools.GetFieldContainer(property, property.serializedObject.targetObject);
            if (target == null)
            {
                container.Add(new Label($"{attr.GetType().Name} Error"));
                return container;
            }

            var field = target.GetType().GetField(attr.conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var prop = field == null ? target.GetType().GetProperty(attr.conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null;

            if (field == null && prop == null)
            {
                container.Add(new Label($"{attr.GetType().Name} Error: {attr.conditionName}"));
                return container;
            }

            bool GetValue(object t) => field != null ? (bool)field.GetValue(t) : (bool)prop.GetValue(t);
            ApplyCondition(propField, GetValue(target));

            var conditionProp = EditorTools.GetConditionSerializedProperty(property, attr.conditionName);
            if (conditionProp != null)
            {
                propField.TrackPropertyValue(conditionProp, _ => {
                    object t = EditorTools.GetFieldContainer(property, property.serializedObject.targetObject);
                    if (t != null) ApplyCondition(propField, GetValue(t));
                });
            }
            else
            {
                propField.TrackSerializedObjectValue(property.serializedObject, so => {
                    object t = EditorTools.GetFieldContainer(property, so.targetObject);
                    if (t != null) ApplyCondition(propField, GetValue(t));
                });
            }

            return container;
        }
    }
}