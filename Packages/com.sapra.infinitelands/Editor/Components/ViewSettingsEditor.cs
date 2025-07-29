using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [CustomPropertyDrawer(typeof(ViewSettings))]
    public class ViewSettingsEditor: PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();

            // Create a Foldout for the ViewSettings section
            var foldout = new Foldout{text = "View Settings"};

            // Create the fields
            var force = new PropertyField(property.FindPropertyRelative(nameof(ViewSettings.ForceStayGameMode)));
            var fetCam = new PropertyField(property.FindPropertyRelative(nameof(ViewSettings.AutomaticallyFetchCameras)));
            var fetTran = new PropertyField(property.FindPropertyRelative(nameof(ViewSettings.AutomaticallyFetchTransforms)));

            var baCa = new PropertyField(property.FindPropertyRelative(nameof(ViewSettings.BaseCameras)));
            var baTa = new PropertyField(property.FindPropertyRelative(nameof(ViewSettings.BaseTransforms)));
            var allCa = new PropertyField(property.FindPropertyRelative("AllCameras"));
            var allTra = new PropertyField(property.FindPropertyRelative("AllTransforms"));

            // Disable fields as before
            allCa.SetEnabled(false);
            allTra.SetEnabled(false);

            // Add fields to the foldout (this will indent them)
            foldout.Add(force);
            foldout.Add(fetCam);
            foldout.Add(fetTran);

            foldout.Add(baCa);
            foldout.Add(baTa);
            foldout.Add(allCa);
            foldout.Add(allTra);

            // Add the foldout to the container
            container.Add(foldout);

            return container;
        }
    }
}