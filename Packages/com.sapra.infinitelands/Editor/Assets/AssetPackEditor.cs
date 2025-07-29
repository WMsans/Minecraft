using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    [CustomEditor(typeof(AssetPack), true)]
    [CanEditMultipleObjects]
    public class AssetPackEditor : UnityEditor.Editor
    {
        AssetPack asset;
        VisualElement root;
        public override VisualElement CreateInspectorGUI()
        {
            asset = target as AssetPack;
            root = new VisualElement();
            RedrawView();
            root.TrackSerializedObjectValue(serializedObject, a => RedrawView());
            return root;
        }

        public void RedrawView(){
            root.Clear();   
            root.Add(new Label("Selected Assets"));
            root.Add(RemovalList());
            root.Bind(serializedObject);
        }

        public VisualElement RemovalList(){
            SerializedProperty removeAtTexturesProperty = serializedObject.FindProperty(nameof(VegetationAsset.RemoveAtTextures));
            ScrollView listContainer = new ScrollView();
            listContainer.AddToClassList("textures");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.AssetSearchWindowStyles);
            listContainer.styleSheets.Add(styleSheet);
            void RebuildList()
            {
                listContainer.Clear(); // Clear the current UI
                AssetSearchWindow.AddAssetGrid(null, asset.Assets, texture => 
                    asset.Assets = asset.Assets.Where(a => a != texture).ToList(), listContainer);

                // Add buttons to add/remove elements
                VisualElement buttonsContainer = new VisualElement();
                var addButton = new Button(() => AssetSearchWindow.OpenAssetSearchWindow(null, a => {                   
                    serializedObject.Update();
                    asset.Assets.Add(a as InfiniteLandsAsset);
                    asset.Assets = asset.Assets.Where(a => a != null).Distinct().ToList();                   
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                    RebuildList();
                }, avoid: typeof(AssetPack), allowEmpty: false));
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

        /* public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {   
            asset = target as VegetationAsset;
            if(asset == null)
                return null;

            Texture2D previewTexture = null;
            int attempts = 0;
            while (previewTexture == null && attempts <= 10)
            {
                previewTexture = (asset.Preview(false) as Image).image as Texture2D;
                attempts++;
            }
            if(previewTexture == null)
                return null;
                
            Texture2D tex = new Texture2D (width, height);
            EditorUtility.CopySerialized(previewTexture, tex);
            return tex;
        } */
    }
}