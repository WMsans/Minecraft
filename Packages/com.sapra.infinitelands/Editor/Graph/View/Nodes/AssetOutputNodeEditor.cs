using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using System.Reflection;

namespace sapra.InfiniteLands.Editor
{
    [EditorForClass(typeof(AssetOutputNode))]
    public class AssetOutputNodeEditor : NodeView
    {
        public AssetOutputNodeEditor(InfiniteLandsGraphView view, InfiniteLandsNode node) : base(view, node){}

        protected override void GenerateExtraPreviewButtons()
        {
            AssetOutputNode densityOutput = node as AssetOutputNode;
            PortData assetFakePort = new PortData(node.guid, nameof(densityOutput.Asset));
            OutputPreview preview = GraphViewersFactory.CreateOutputPreview(assetFakePort, node, this);
            GeneratePreviewButton(preview, assetFakePort);
        }
        protected override void GenerateTitle()
        {
            CustomNodeAttribute attribute = node.GetType().GetCustomAttribute<CustomNodeAttribute>();
            AssetOutputNode densityOutput = node as AssetOutputNode;
            if(densityOutput.Asset != null)
                this.title = attribute.name + " : " + densityOutput.Asset.name;
            else
                this.title = attribute.name;
        }

        protected override void DrawProperties(VisualElement propertiesContainer, SerializedProperty _)
        {
            propertiesContainer.Clear();
            AssetOutputNode densityOutput = node as AssetOutputNode;
            var nodeProperty = GetNodeProperty(node);
            if(nodeProperty == null)
                return;
            var objectFieldProperty = nodeProperty.FindPropertyRelative(nameof(densityOutput.Asset));
            // Create the ObjectField UI element
            ObjectField objectField = new ObjectField(nameof(densityOutput.Asset))
            {
                objectType = typeof(InfiniteLandsAsset), // Set to the type of object you want to display
                value = densityOutput.Asset // Set the initial value (if any)
            };
            objectField.bindingPath = objectFieldProperty.propertyPath;
            objectField.TrackPropertyValue(objectFieldProperty, TrackChanges);

            // Hide the object picker button (the circular button)
            var objectFieldSelector = objectField.Q(className: "unity-object-field__selector");
            objectFieldSelector.style.display = DisplayStyle.None;
            
            var selectorCopy = new Button(() => AssetSearchWindow.OpenAssetSearchWindow(densityOutput.Asset, selectedAsset =>
            {
                // Update the ObjectField value with the selected asset
                densityOutput.Asset = selectedAsset as InfiniteLandsAsset;
                objectField.value = densityOutput.Asset;
                //TriggerNodeReload();
            }));

            
            selectorCopy.AddToClassList("custom_picker");

            // Copy all the relevant styles from the original selector button
            var container = objectField.Q(className: "unity-base-field__input");
            container.Add(selectorCopy);
            propertiesContainer.Add(objectField);

            if(objectFieldProperty.NextVisible(false)){
                base.DrawProperties(propertiesContainer, objectFieldProperty);
            }
        }
    }
}
