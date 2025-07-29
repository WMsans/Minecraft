using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [CustomEditor(typeof(InfiniteLandsTerrain))]
    public class InfiniteLandsTerrainEditor : UnityEditor.Editor
    {
        private InfiniteLandsTerrain infiniteLands;
        private VisualElement currentComponents;
        public Action OnPropertyModified;
        public override VisualElement CreateInspectorGUI()
        {
            infiniteLands = target as InfiniteLandsTerrain;
            currentComponents = new();
            VisualElement root = new VisualElement();
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.InfiniteLandsEditorStyles));
            root.AddToClassList("root"); 

            root.Add(TopConent());
            root.Add(BasicProperties());
            root.Add(currentComponents);

            ReloadComponents();

            root.Bind(serializedObject);
            return root;
        }

        private void AddNewComponent()
        {
            var types = GetAssemblyRoutines<ChunkProcessor>();
            var menu = new GenericMenu();
            foreach (var type in types)
            {
                string name = type.Name;
                bool exists = infiniteLands.EnabledComponents.Any(c => c?.GetType() == type);
                menu.AddItem(new GUIContent(name), false, exists ? null : () =>
                {
                    infiniteLands.AddComponent(type);
                    serializedObject.ApplyModifiedProperties();
                    ReloadComponents();
                });
            }
            menu.ShowAsContext();
        }

        private List<Type> GetAssemblyRoutines<T>() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes().Where(t => t.IsSubclassOf(typeof(T)) && !t.IsGenericType))
            .ToList();
        
        void ReloadComponents(){
            currentComponents.Clear();
            serializedObject.Update();

            bool canModify = infiniteLands.ProcessorTemplate == LandsTemplate.Custom;
            VisualElement topName = new VisualElement();
            topName.AddToClassList("componentTitle");

            SerializedProperty templateProperty = serializedObject.FindProperty(nameof(infiniteLands.ProcessorTemplate));
            VisualElement templateContainer = new VisualElement();
            PropertyField templateField = new PropertyField(templateProperty);

            EditorApplication.delayCall += () => {
                templateField.RegisterValueChangeCallback(evt => {
                    infiniteLands.ChangeTemplate((LandsTemplate)evt.changedProperty.enumValueIndex);
                    ReloadComponents();
                });
            };
            Label templateLabel = new Label("Template:");
            templateContainer.Add(templateLabel);
            templateContainer.Add(templateField);
            templateContainer.AddToClassList("templateField");

            Button addButton = new Button(AddNewComponent){
                text = "Add Processor"
            };
            Button clearButton = new Button(ClearComponents){
                text = "Clear"
            };
            clearButton.SetEnabled(canModify);
            addButton.SetEnabled(canModify);

            VisualElement rightSide = new VisualElement();
            rightSide.style.flexDirection = FlexDirection.Row;

            rightSide.Add(templateContainer);
            rightSide.Add(addButton);
            rightSide.Add(clearButton);


            Label label = new Label("Processors");
            label.AddToClassList("title");
            topName.Add(label);
            topName.Add(rightSide);

            currentComponents.Add(topName);
            var components = serializedObject.FindProperty(nameof(infiniteLands.EnabledComponents));
            if(components != null){
                for(int i = 0; i < components.arraySize; i++)
                {
                    SerializedProperty item = components.GetArrayElementAtIndex(i);
                    var result = LoadAbstractRoutine(item, infiniteLands.EnabledComponents[i].GetType());
                    currentComponents.Add(result);
                }
            }

            currentComponents.Bind(serializedObject);
        }

        private void ClearComponents(){
            infiniteLands.ClearComponents();
            ReloadComponents();
        }
        private VisualElement LoadAbstractRoutine(SerializedProperty property, Type itemType)
        {
            // Create the root VisualElement container
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            // Create header element
            VisualElement header = CreateAbstractRoutineHeader(property, itemType);
            container.Add(header);

            bool isExpanded = property.FindPropertyRelative(nameof(InfiniteLandsComponent.expanded)).boolValue;
            // Create expandable content
            if (isExpanded)
            {
                VisualElement propertyContainer = new VisualElement();
                propertyContainer.AddToClassList("property-container");

                SerializedProperty propertyCopy = property.Copy();
                var depth = propertyCopy.depth;
                bool available = propertyCopy.NextVisible(true); //Skip item header/dropdown
                bool moreProperties = available && propertyCopy.depth > depth;
                if(moreProperties){
                    var currentDepth = propertyCopy.depth;
                    do{
                        var prop = new PropertyField(propertyCopy);
                        EditorApplication.delayCall += () => {
                            prop.RegisterValueChangeCallback(evt => {
                                OnPropertyModified?.Invoke();
                            });
                        };
                        propertyContainer.Add(prop);
                    }
                    while (propertyCopy.NextVisible(false) && propertyCopy.depth >= currentDepth );               
                }
                else{
                    Label noPropertiesLabel = new Label("No editable properties available");
                    noPropertiesLabel.AddToClassList("no-properties-label");
                    propertyContainer.Add(noPropertiesLabel);
                }
                container.Add(propertyContainer);

            }

            return container;
        }

        private VisualElement CreateAbstractRoutineHeader(SerializedProperty property, Type itemType)
        {
            bool canModify = infiniteLands.ProcessorTemplate == LandsTemplate.Custom;
            string displayName = ObjectNames.NicifyVariableName(itemType.Name);

            var header = new VisualElement();
            header.AddToClassList("header-container");

            var labelButton = new Button(() => ToggleExpansion(property)){
                text = displayName
            };
            labelButton.AddToClassList("label-button");

            var removeButton = new Button(() => RemoveComponent(itemType)){
                text = "×",
                tooltip = "Remove component"
            };
            removeButton.AddToClassList("remove-button");
            SetupContextMenu(labelButton, itemType.Name);

            removeButton.SetEnabled(canModify);
            if (!canModify){
                removeButton.tooltip = "Cannot remove: Component is read-only";
            }

            header.Add(labelButton);
            header.Add(removeButton);
            return header;
        }

        private void ToggleExpansion(SerializedProperty property)
        {
            var expandedProp = property.FindPropertyRelative(nameof(InfiniteLandsComponent.expanded));
            expandedProp.boolValue = !expandedProp.boolValue;
            serializedObject.ApplyModifiedProperties();
            ReloadComponents();
        }

        private void RemoveComponent(Type itemType)
        {
            infiniteLands.RemoveComponent(itemType);
            ReloadComponents();
        }

        private void SetupContextMenu(Button button, string componentName)
        {
            button.RegisterCallback<ContextClickEvent>(evt =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Edit Script"), false, () => OpenFile(componentName));
                menu.AddItem(new GUIContent("Copy Component Name"), false, () => 
                    EditorGUIUtility.systemCopyBuffer = componentName);
                menu.ShowAsContext();
            });
        }

        private void OpenFile(string type)
        {
            var guid = AssetDatabase.FindAssets($"{type} t:script")
                .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == type);
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        }

        private VisualElement BasicProperties(){
            VisualElement propertiesContainer = new VisualElement();
            Label label = new Label("Configuration");
            label.AddToClassList("title");
            propertiesContainer.Add(label);
            SerializedProperty property = serializedObject.FindProperty(nameof(infiniteLands.terrainGenerator));
            var currentDepth = property.depth;
            do{
                var prop = new PropertyField(property);
                EditorApplication.delayCall += () => {
                    prop.RegisterValueChangeCallback(evt => {
                        OnPropertyModified?.Invoke();
                    });
                };
                propertiesContainer.Add(prop);
            }
            while (property.NextVisible(false) && property.depth >= currentDepth && property.name != nameof(infiniteLands.EnabledComponents));
            return propertiesContainer;
        }

        private VisualElement TopConent(){
            VisualElement container = new VisualElement();
            Button regeneration = new Button(() => infiniteLands.Initialize());
            regeneration.text = "Sync & Regenerate";
            regeneration.SetEnabled(infiniteLands.graph != null);
            OnPropertyModified += () => regeneration.SetEnabled(infiniteLands.graph != null);

            VisualElement buttons = GenerateVisualizationToggles();
            container.Add(regeneration);
            container.Add(buttons);
            return container;
        }

        private VisualElement GenerateVisualizationToggles(){
            VisualElement container = new VisualElement();
            container.AddToClassList("GenerationButtons");
            Button leftButton = new Button(() => infiniteLands.ChangeVisualizer(true, true));
            leftButton.text = "Infinite Visualizer";

            Button rightButton = new Button(() => infiniteLands.ChangeVisualizer(false, true));
            rightButton.text = "Single Visualizer";

            UpdateButtonStates(leftButton, rightButton, infiniteLands.infiniteMode);

            // Optional: Add a callback to update states when SwitchVisualizer is called
            leftButton.clicked += () => UpdateButtonStates(leftButton, rightButton, true);
            rightButton.clicked += () => UpdateButtonStates(leftButton, rightButton, false);
            Undo.undoRedoPerformed += () => UpdateButtonStates(leftButton, rightButton, infiniteLands.infiniteMode);

            container.Add(leftButton);
            container.Add(rightButton);
            return container;
        }


        private void UpdateButtonStates(Button infiniteButton, Button singleButton, bool isInfinite)
        {
            string addInfinite = isInfinite ? "enabled" : "disabled";
            string addSingle = isInfinite ? "disabled" : "enabled";

            infiniteButton.RemoveFromClassList(addSingle);
            infiniteButton.AddToClassList(addInfinite);

            singleButton.RemoveFromClassList(addInfinite);
            singleButton.AddToClassList(addSingle);
        }
    }
}