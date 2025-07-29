// PointInstanceVisualizer.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    public class PointInstanceVisualizer
    {
        private readonly Color pointColor;
        private readonly bool lowOpacity;

        private const float ContainerSize = 200f;

        public PointInstanceVisualizer(Color pointColor = default, bool lowOpactiy = false)
        {
            this.pointColor = pointColor == default ? Color.magenta : pointColor;
            this.lowOpacity = lowOpactiy;
        }

        public VisualElement CreateVisual(PointInstance data, BranchData settings, bool grid, float meshScale)
        {
            var container = new VisualElement() { style = { width = ContainerSize, height = ContainerSize } };
            if (data == null) return container;

            if(grid)
                DrawGrid(container, data, settings, meshScale);
            DrawPoints(container, data, settings, meshScale);
            return container;
        }

        private void DrawGrid(VisualElement container, PointInstance data, BranchData settings, float meshScale)
        {
            float gridSize = data.GridSize;
            Vector3 terrainPos = settings.terrain.Position;
            int subdivisions = 2; // Number of subdivisions per cell
            float mainLineOpacity = 1.0f; // Full opacity for main lines
            float internalLineOpacity = 0.3f; // Reduced opacity for internal lines

            if (gridSize <= 0 || meshScale <= 0) return;

            float cellSize = (gridSize / meshScale) * ContainerSize;
            float halfCellSize = cellSize / 2f;
            float center = ContainerSize / 2f;
            int lineCount = Mathf.CeilToInt(center / cellSize);
            float subCellSize = cellSize / subdivisions;

            Vector2 normalizedOrigin = new Vector2(terrainPos.x, -terrainPos.z) / meshScale;
            float offsetX = (normalizedOrigin.x % (gridSize / meshScale)) * ContainerSize;
            float offsetY = (normalizedOrigin.y % (gridSize / meshScale)) * ContainerSize;

            for (int i = -lineCount; i <= lineCount; i++)
            {
                // Main grid lines with full opacity
                float xPos = center + (i * cellSize) - offsetX + halfCellSize;
                float yPos = center + (i * cellSize) - offsetY + halfCellSize;
                AddLine(container, xPos, true, mainLineOpacity);    // Vertical main lines
                AddLine(container, yPos, false, mainLineOpacity);   // Horizontal main lines

                // Internal subdivision lines with reduced opacity
                for (int j = 1; j < subdivisions; j++)
                {
                    float subXPos = xPos + (j * subCellSize) - cellSize;
                    float subYPos = yPos + (j * subCellSize) - cellSize;
                    
                    AddLine(container, subXPos, true, internalLineOpacity);   // Vertical internal lines
                    AddLine(container, subYPos, false, internalLineOpacity);  // Horizontal internal lines
                }
            }
        }
        private void DrawPoints(VisualElement container, PointInstance data, BranchData settings, float meshScale)
        {
            if (!data.GetAllPointsInMesh(settings, out var allPoints)) return;

            Vector3 terrainPos = settings.terrain.Position;

            foreach (var point in allPoints)
            {
                var pointElement = GetPoint(point.Scale, point.YRotation, ContainerSize/meshScale);
                Vector3 centered = (Vector3)point.Position - terrainPos;
                Vector2 normalizedPos = new Vector2(centered.x, centered.z) / meshScale + Vector2.one * 0.5f;

                pointElement.style.position = Position.Absolute;
                pointElement.style.left = normalizedPos.x * ContainerSize - pointElement.style.width.value.value / 2;
                pointElement.style.top = (1 - normalizedPos.y) * ContainerSize - pointElement.style.height.value.value / 2;
                
                container.Add(pointElement);
            }
        }

        private void AddLine(VisualElement container, float position, bool isVertical, float opacity = 1.0f)
        {
            // Assuming you're using Unity's UIElements or similar
            var line = new VisualElement();
            line.style.opacity = opacity;
            
            if (isVertical)
            {
                line.style.position = Position.Absolute;
                line.style.left = position;
                line.style.top = 0;
                line.style.width = 1;  // Line thickness
                line.style.height = ContainerSize;
            }
            else
            {
                line.style.position = Position.Absolute;
                line.style.top = position;
                line.style.left = 0;
                line.style.height = 1;  // Line thickness
                line.style.width = ContainerSize;
            }
            
            line.style.backgroundColor = Color.gray; // Adjust color as needed
            container.Add(line);
        }

        private VisualElement GetPoint(float scale, float yRotation, float factor)
        {
            float size = Mathf.Max(scale*factor, 8);
            VisualElement point = CreateContainer(size);
            point.Add(CreateChevron(size));
            point.Add(CreateDot(size));
            point.style.rotate = new StyleRotate(new Rotate(yRotation));
            
            return point;
        }

        private VisualElement CreateContainer(float size)
        {
            return new VisualElement
            {
                style = {
                    width = size,
                    height = size,
                    opacity = lowOpacity ? 0.3f : 1f
                }
            };
        }

        private VisualElement CreateDot(float size)
        {
            float dotSize = size * 0.5f;
            float radius = dotSize / 2;
            float centerOffset = (size - dotSize) / 2;

            return new VisualElement
            {
                style = {
                    width = dotSize,
                    height = dotSize,
                    backgroundColor = pointColor,
                    borderTopLeftRadius = radius,
                    borderTopRightRadius = radius,
                    borderBottomLeftRadius = radius,
                    borderBottomRightRadius = radius,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = Color.black,
                    borderRightColor = Color.black,
                    borderBottomColor = Color.black,
                    borderLeftColor = Color.black,
                    position = Position.Absolute,
                    left = centerOffset,
                    top = centerOffset
                }
            };
        }

        private VisualElement CreateChevron(float size)
        {
            float chevronSize = size * 0.8f;
            float centerOffset = (size - chevronSize) / 2;
            
            VisualElement chevron = new VisualElement
            {
                style = {
                    width = chevronSize,
                    height = chevronSize,
                    position = Position.Absolute,
                    left = centerOffset,
                    top = -2
                }
            };

            chevron.Add(CreateWing(chevronSize, -45f, 0));
            chevron.Add(CreateWing(chevronSize, 45f, chevronSize * 0.3f));
            return chevron;
        }

        private VisualElement CreateWing(float chevronSize, float angle, float horizontalOffset)
        {
            float wingWidth = chevronSize * 0.7f;
            return new VisualElement
            {
                style = {
                    width = wingWidth,
                    height = 2,
                    backgroundColor = pointColor,
                    position = Position.Absolute,
                    left = horizontalOffset,
                    top = chevronSize * 0.5f - 1,
                    rotate = new StyleRotate(new Rotate(angle))
                }
            };
        }
    }
}