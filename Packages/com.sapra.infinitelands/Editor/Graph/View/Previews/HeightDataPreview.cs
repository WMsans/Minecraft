using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    [EditorForClass(typeof(HeightData))]
    public class HeightDataPreview : OutputPreview
    {
        private BurstTexture texture;
        private readonly PointInstanceVisualizer previousVisualizer;
        private bool LocalMode;

        public HeightDataPreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView) : base(targetPort, node, nodeView)
        {
            previousVisualizer = new PointInstanceVisualizer(Color.red, true);
        }

        public override VisualElement GetPreview(BranchData settings, GraphSettings graphSettings) => GetPreview(settings, graphSettings, true);

        public VisualElement GetPreview(BranchData settings, GraphSettings graphSettings, bool button)
        {
            if (!Node.isValid)
                return null;

            // Fetch data and resources
            var writeableNode = settings.GetWriteableNode(Node);
            if (!writeableNode.ProcessNode(settings))
                return null;
            
            if (!writeableNode.TryGetOutputData(settings, out HeightData data, PortData.fieldName, PortData.listIndex))
                return null;
            HeightMapBranch heightBranch = settings.GetData<HeightMapBranch>();
            IBurstTexturePool texturePool = settings.GetGlobalData<IBurstTexturePool>();
            texture = texturePool.GetUnpooledTexture(PortData.fieldName, FilterMode.Point);
            NativeArray<Color32> raw = texture.GetRawData<Color32>();

            // Global min/max from HeightData
            var globalMinMax = new[] { data.minMaxValue.y, data.minMaxValue.x };

            // Determine normalization range based on mode
            NativeArray<float> normalizationRange;
            JobHandle jobHandle = data.jobHandle;
            if(LocalMode){
                    normalizationRange = new NativeArray<float>(globalMinMax, Allocator.TempJob);
                    jobHandle = GetMapBoundaries.ScheduleParallel(
                        normalizationRange, heightBranch.GetMap(),
                        data.indexData, raw.Length, texturePool.GetTextureResolution(), data.jobHandle
                    );
            }
            else{
                normalizationRange = new NativeArray<float>(new[] { data.minMaxValue.x, data.minMaxValue.y }, Allocator.TempJob);
            }

            JobHandle textureJob = MTJGeneral.ScheduleParallel(
                raw, normalizationRange, heightBranch.GetMap(),
                data.indexData, texturePool.GetTextureResolution(), jobHandle
            );
            textureJob.Complete();

            var container = new VisualElement
            {
                style = { width = Length.Percent(100), height = Length.Percent(100) }
            };

            // Image display
            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                image = texture.ApplyTexture(),
                style = { width = Length.Percent(100), height = Length.Percent(100) }
            };

            texture.ApplyTexture();
            StackVisuals(container, image);

            // Handle point data visualization
            var inputPointData = Node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => new { Field = f, Attribute = f.GetCustomAttribute<InputAttribute>() })
                .Where(x => x.Attribute != null && x.Field.GetCustomAttribute<DisabledAttribute>() == null && x.Field.FieldType == typeof(PointInstance))
                .Select(x => x.Field)
                .FirstOrDefault();
          
            if (inputPointData != null)
            {
                container.style.position = Position.Relative;
                if (writeableNode.TryGetInputData<PointInstance>(settings, out var pointData, inputPointData.Name))
                {
                    VisualElement pointVisuals = previousVisualizer.CreateVisual(pointData, settings, true, graphSettings.MeshScale);
                    StackVisuals(container, pointVisuals);
                }
            }

            if (button)
            {
                var minmaxContainer = Generate_MinMaxContainer(normalizationRange);
                StackVisuals(container, minmaxContainer);

                var modeToggle = Generate_LocalModeToggle();
                var worldPreview = EditorTools.Generate_WorldPreview(NodeView, PortData);
                VisualElement rightContainer = new VisualElement();
                rightContainer.Add(modeToggle);
                rightContainer.Add(worldPreview);
                StackVisualRight(container, rightContainer);
            }

            normalizationRange.Dispose(textureJob);
            return container;
        }

        private Toggle Generate_LocalModeToggle()
        {
            var modeToggle = new Toggle()
            {
                value = LocalMode // Initial state
            };
            modeToggle.AddToClassList("preview-toggle");
            modeToggle.AddToClassList("map-toggle");
            modeToggle.style.width = Length.Percent(100);
            modeToggle.RegisterValueChangedCallback(evt =>
            {
                LocalMode = evt.newValue;
                NodeView.ChangePreview(PortData);
                EditorTools.UpdateToggleState(modeToggle, evt.newValue);
            });
            EditorTools.UpdateToggleState(modeToggle, LocalMode);
            return modeToggle;
        }
        private VisualElement Generate_MinMaxContainer(NativeArray<float> normalizationRange)
        {
            var minmaxContainer = new VisualElement();
            minmaxContainer.AddToClassList("minMaxContainer");
            var minLabel = new Label("Min: " + normalizationRange[0].ToString("F2"));
            var maxLabel = new Label("Max: " + normalizationRange[1].ToString("F2"));
            minmaxContainer.Add(maxLabel);
            minmaxContainer.Add(minLabel);
            return minmaxContainer;
        }
        public override bool ValidPreview()
        {
            return Node.isValid;
        }        
    }
}