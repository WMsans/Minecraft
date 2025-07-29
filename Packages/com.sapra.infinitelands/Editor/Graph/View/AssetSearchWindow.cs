using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    public class AssetSearchWindow : EditorWindow
    {
        private Action<UnityEngine.Object> _onSelectObjectCallback;
        private Dictionary<Type, List<UnityEngine.Object>> _assetsByType = new();
        private ScrollView _scrollView;

        private UnityEngine.Object preselected;

        [MenuItem("Window/Infinite Lands/Asset Search", false, 5011)]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetSearchWindow>();
            window.titleContent = new GUIContent("Asset Search");
            window.Show();
        }

        public static void OpenAssetSearchWindow(UnityEngine.Object current, Action<UnityEngine.Object> callback, Type filterAllow = null, Type avoid = null, bool allowEmpty = true)
        {
            // Open the custom asset search window
            AssetSearchWindow wnd = EditorWindow.GetWindow<AssetSearchWindow>("Asset Search");

            // Ensure the window is shown and focused
            wnd.Show();
            wnd.Focus();

            // Initialize the window with a callback for asset selection
            wnd.Initialize(callback, current, filterAllow, avoid, allowEmpty);
        }

        public void Initialize(Action<UnityEngine.Object> onSelectObjectCallback, UnityEngine.Object preselected, Type filter = null, Type avoid = null, bool allowEmpty = true)
        {
            if(filter == null)
                filter = typeof(InfiniteLandsAsset);
                
            this.preselected = preselected;
            _onSelectObjectCallback = onSelectObjectCallback;

            // Load the UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UIStyles.AssetSearchWindowLayout);
            visualTree.CloneTree(rootVisualElement);

            // Load the USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.AssetSearchWindowStyles);
            rootVisualElement.styleSheets.Add(styleSheet);

            // Assign UI elements
            _scrollView = rootVisualElement.Q<ScrollView>("scrollView");

            // Register search field callback
            var searchField = rootVisualElement.Q<TextField>("searchField");
            searchField.RegisterValueChangedCallback(evt => FilterAssets(evt.newValue, allowEmpty));

            LoadAllAssets(filter.Name, avoid, allowEmpty);
        }

        private void LoadAllAssets(string filterType, Type avoidType, bool allowEmpty)
        {
            _assetsByType.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{filterType}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null) continue;

                var type = asset.GetType();
                if(type==avoidType) continue;

                if (!_assetsByType.ContainsKey(type))
                {
                    _assetsByType[type] = new List<UnityEngine.Object>();
                }
                _assetsByType[type].Add(asset);
            }

            PopulateGrid(allowEmpty);
        }

        private void PopulateGrid(bool allowEmpty)
        {
            _scrollView.Clear();

            if(allowEmpty){
                var assetCard = CreateNullAsset();
                _scrollView.Add(assetCard);
            }

            foreach (var typeGroup in _assetsByType)
            {
                AddAssetHeader(typeGroup.Key);
                AssetGridInternal(typeGroup.Value);
            }
        }

        private void FilterAssets(string searchQuery, bool allowEmpty)
        {
            _scrollView.Clear();

            if(allowEmpty){
                var assetCard = CreateNullAsset();
                _scrollView.Add(assetCard);
            }
            
            foreach (var typeGroup in _assetsByType)
            {
                var filteredAssets = string.IsNullOrWhiteSpace(searchQuery)
                    ? typeGroup.Value
                    : typeGroup.Value.Where(asset =>
                        asset.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                if(filteredAssets.Count > 0){
                    AddAssetHeader(typeGroup.Key);
                    AssetGridInternal(filteredAssets);
                }
            }
        }

        private void AssetGridInternal(List<UnityEngine.Object> assets){
            AddAssetGrid(preselected, assets, asset => {
                    _onSelectObjectCallback?.Invoke(asset);
                    Close();
                }, _scrollView);
        }

        private void AddAssetHeader(Type assetType)
        {
            var headerLabel = new Label(ObjectNames.NicifyVariableName(assetType.Name));
            headerLabel.AddToClassList("asset-type-header");
            _scrollView.Add(headerLabel);
        }

        public static void AddAssetGrid(UnityEngine.Object selected, IEnumerable<UnityEngine.Object> assets, Action<UnityEngine.Object> onSelectObjectCallback, ScrollView _scrollView)
        {
            var gridContainer = new VisualElement { name = "gridContainer" };
            gridContainer.AddToClassList("grid-container");
            if(assets == null)
                return;
            foreach (var asset in assets)
            {
                if(asset == null)
                    continue;
                var assetCard = CreateAssetCard(asset, selected, a => onSelectObjectCallback?.Invoke(asset));
                gridContainer.Add(assetCard);
            }

            _scrollView.Add(gridContainer);
        }

        public static VisualElement CreateAssetCard(UnityEngine.Object asset, UnityEngine.Object preselected, Action<MouseUpEvent> callback)
        {
            var card = new VisualElement();
            card.AddToClassList("asset-card");
            if(asset == null)
                return null;
            if (asset.Equals(preselected))
                card.AddToClassList("pre-selected");

            // Create the preview image
            VisualElement preview = null;
            if(asset is IHaveAssetPreview previewer){
                preview = previewer.Preview(false);
            }

            if(preview == null){
                var previewImage = new Image { name = "preview" };
                previewImage.image = AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset);
                preview = previewImage;
            }

            preview.AddToClassList("preview");
            card.Add(preview);
        

            // Add the asset name
            var nameLabel = new Label(asset.name) { name = "name" };
            nameLabel.AddToClassList("name");
            card.Add(nameLabel);

            // Add click callback
            card.RegisterCallback<MouseUpEvent>(evt => callback?.Invoke(evt));

            return card;
        }

        private VisualElement CreateNullAsset()
        {
            var card = new VisualElement();
            card.AddToClassList("asset-card");
            if (preselected == null)
                card.AddToClassList("pre-selected");

            // Create the preview image
            var previewImage = new Image { name = "preview" };
            previewImage.AddToClassList("preview");
            card.Add(previewImage);
        

            // Add the asset name
            var nameLabel = new Label("Empty") { name = "name" };
            nameLabel.AddToClassList("name");
            card.Add(nameLabel);

            // Add click callback
            card.RegisterCallback<MouseUpEvent>(evt =>
            {
                _onSelectObjectCallback?.Invoke(null);
                Close();
            });

            return card;
        }
    }
}
