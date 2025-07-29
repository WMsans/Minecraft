using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static sapra.InfiniteLands.IHoldVegetation;

namespace sapra.InfiniteLands.Editor
{
    [CustomEditor(typeof(VegetationAsset), true)]
    [CanEditMultipleObjects]
    public class VegetationAssetEditor : UnityEditor.Editor
    {
        VegetationAsset asset;
        VisualElement root;
        public override VisualElement CreateInspectorGUI()
        {
            asset = target as VegetationAsset;
            root = new VisualElement();
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.VegetationAssetStyles));
            RedrawView();
            return root;
        }

        public void RedrawView(){
            root.Clear();   
            root.Add(CreateHeader());
            root.Add(CreatePreviewAndMeshConfiguration());
            root.Add(PositionDataConfiguration());
            root.Add(ColorDataConfiguration());
            root.Add(Debugging());
            root.Bind(serializedObject);
        }

        private VisualElement CreateHeader(){
            var element = CreateNormal(nameof(VegetationAsset.spawnMode), RedrawView);
            return CreateNormal(nameof(VegetationAsset.spawnMode));
        }

        private VisualElement CreatePreviewAndMeshConfiguration(){
            var preview = asset.Preview(true);
            if(preview == null)
                preview = new Label("Missing Data");
            var items = CreateMeshItems();
            preview.AddToClassList("rigthSide");
            items.AddToClassList("leftSide");
            return EditorTools.SideBySide(items, preview);
            
        }
        private VisualElement CreateMeshItems(){
            VisualElement container = new VisualElement();
            container.Add(CreateNormal(nameof(VegetationAsset.skipRendering)));
            if(asset.spawnMode == SpawnMode.GPUInstancing){
                var generateCollidersProperty = serializedObject.FindProperty(nameof(VegetationAsset.GenerateColliders));
                container.Add(CreateNormal(generateCollidersProperty, RedrawView));
                if (generateCollidersProperty.boolValue)
                {
                    container.Add(CreateIndented(nameof(VegetationAsset.ColliderObject)));
                    container.Add(CreateIndented(nameof(VegetationAsset.ColliderMode)));
                    container.Add(CreateIndented(nameof(VegetationAsset.CollisionDistance)));
                }

                container.Add(CreateNormal(nameof(VegetationAsset.LodGroups), RedrawView));
                container.Add(CreateNormal(nameof(VegetationAsset.LOD), RedrawView));
                container.Add(CreateNormal(nameof(VegetationAsset.HighLodDistance)));
                container.Add(CreateNormal(nameof(VegetationAsset.CrossFadeLODDithering)));

            }
            else{
                container.Add(CreateNormal(nameof(VegetationAsset.InstanceObject)));
                container.Add(CreateNormal(nameof(VegetationAsset.Materials)));
            }
            return container;
        }

        private VisualElement PositionDataConfiguration(){
            VisualElement container = new VisualElement();
           
            container.Add(CreateNormal(nameof(VegetationAsset.ViewDistance)));
            container.Add(CreateNormal(nameof(VegetationAsset.DistanceBetweenItems)));
            container.Add(CreateNormal(nameof(VegetationAsset.PositionRandomness)));
            container.Add(CreateNormal(nameof(VegetationAsset.VerticalPosition)));
            container.Add(CreateNormal(nameof(VegetationAsset.AlignToGround)));
            
            var heightVariation = serializedObject.FindProperty(nameof(VegetationAsset.HeightVariation));
            container.Add(CreateNormal(heightVariation, RedrawView));
            if(heightVariation.enumValueIndex == (int)HeightVariation.SimplexNoise || heightVariation.enumValueIndex == (int)HeightVariation.Mixed){
                container.Add(CreateIndented(nameof(VegetationAsset.SimplexNoiseSize)));
            }
            
            var densityAffectsHeight = serializedObject.FindProperty(nameof(VegetationAsset.DensityAffectsHeight));
            container.Add(CreateNormal(densityAffectsHeight, RedrawView));
            if(densityAffectsHeight.enumValueIndex == (int)DensityHeightMode.BetweenMinMaxSize){
                container.Add(CreateIndented(nameof(VegetationAsset.MinimumScale)));
            }
            
            container.Add(CreateNormal(nameof(VegetationAsset.MaximumScale)));
            if(asset.spawnMode == SpawnMode.GPUInstancing){
                var halfInstances = serializedObject.FindProperty(nameof(VegetationAsset.HalfInstancesAtDistance));
                container.Add(CreateNormal(halfInstances, RedrawView));
                if(halfInstances.boolValue){
                    container.Add(CreateIndented(nameof(VegetationAsset.HalfInstancesDistance)));
                }
                var castShadows = serializedObject.FindProperty(nameof(VegetationAsset.CastShadows));
                container.Add(CreateNormal(castShadows, RedrawView));
                if(castShadows.boolValue){
                    container.Add(CreateIndented(nameof(VegetationAsset._shadowDistance)));
                    if(HasLods)
                        container.Add(CreateIndented(nameof(VegetationAsset.ShadowsLODOffset)));
                }
            }

            return container;
        }
        private bool HasLods => (asset.LOD != null && asset.LOD.Length > 1) || (asset.LodGroups != null && asset.LodGroups.lodCount > 1);

        private VisualElement ColorDataConfiguration(){
            VisualElement container = new VisualElement();
            container.Add(CreateNormal(nameof(VegetationAsset.HowToSampleColor)));
            container.Add(CreateNormal(nameof(VegetationAsset.SamplingRandomness)));
            container.Add(RemovalList());
            return container;
        }

        public VisualElement RemovalList(){
            SerializedProperty removeAtTexturesProperty = serializedObject.FindProperty(nameof(VegetationAsset.RemoveAtTextures));
            ScrollView listContainer = new ScrollView();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.AssetSearchWindowStyles);
            listContainer.styleSheets.Add(styleSheet);
            listContainer.AddToClassList("textures");
            void RebuildList()
            {
                listContainer.Clear(); // Clear the current UI
                listContainer.Add(new Label("Remove at Textures"));
                AssetSearchWindow.AddAssetGrid(null, asset.RemoveAtTextures, texture => 
                    asset.RemoveAtTextures = asset.RemoveAtTextures.Where(a => a != texture).ToList(), listContainer);

                // Add buttons to add/remove elements
                VisualElement buttonsContainer = new VisualElement();
                var addButton = new Button(() => AssetSearchWindow.OpenAssetSearchWindow(null, a => {                   
                    serializedObject.Update();
                    asset.RemoveAtTextures.Add(a as TextureAsset);
                    asset.RemoveAtTextures = asset.RemoveAtTextures.Where(a => a != null).Distinct().ToList();
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                    RebuildList();
                }, typeof(TextureAsset), allowEmpty: false));
                addButton.text = "Add"; 
                var copyButton = new Button(() => ClipboardReflectionHelper.CallSetSerializedProperty(removeAtTexturesProperty));
                copyButton.text = "Copy";

                var pasteButton = new Button(() => {
                    serializedObject.Update();
                    ClipboardReflectionHelper.CallGetSerializedProperty(removeAtTexturesProperty);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                    RebuildList();
                });
                pasteButton.text = "Paste";
                pasteButton.SetEnabled(ClipboardReflectionHelper.CallHasSerializedProperty());
                buttonsContainer.Add(addButton);
                buttonsContainer.Add(copyButton);
                buttonsContainer.Add(pasteButton);
                buttonsContainer.AddToClassList("bottomButtons");
                listContainer.Add(buttonsContainer);
            }

            // Initial build of the list
            RebuildList();
            return listContainer;
        }

        private VisualElement Debugging(){
            VisualElement container = new VisualElement();
            container.Add(CreateNormal(nameof(VegetationAsset.DrawDistances)));
            container.Add(CreateNormal(nameof(VegetationAsset.DrawLODBoundaries)));
            container.Add(CreateNormal(nameof(VegetationAsset.DrawTransitions)));
            container.Add(CreateNormal(nameof(VegetationAsset.DrawShadowBoundaries)));
            container.Add(CreateNormal(nameof(VegetationAsset.DrawItAsSpheres)));
            container.Add(CreateNormal(nameof(VegetationAsset.DrawSpawnedColliders)));
            container.Add(CreateNormal(nameof(VegetationAsset.drawBoundingBox)));
            return container;
        }

        private VisualElement CreateIndented(string propName){
            var field = CreateNormal(propName);
            field.AddToClassList("indented");
            return field;
        }
        private VisualElement CreateNormal(string propName,Action OnModified = default){
            var prop = serializedObject.FindProperty(propName);
            return CreateNormal(prop, OnModified);
        }

        private VisualElement CreateNormal(SerializedProperty prop,Action OnModified = default){
            var field = new PropertyField(prop);
            if(OnModified != null)
                field.TrackPropertyValue(prop, a => OnModified());
            return field;
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {   
            asset = target as VegetationAsset;
            if(asset == null)
                return null;

            var prev = asset.Preview(false);
            if(prev == null)
                return null;
            Texture2D previewTexture = (asset.Preview(false) as Image).image as Texture2D;
/*             int attempts = 0;
            while (previewTexture == null && attempts <= 10)
            {
                var prev = asset.Preview(false);

                previewTexture = (asset.Preview(false) as Image).image as Texture2D;
                attempts++;
            } */
            if(previewTexture == null)
                return null;
                
            Texture2D tex = new Texture2D (width, height);
            EditorUtility.CopySerialized(previewTexture, tex);
            return tex;
        }
    }
}