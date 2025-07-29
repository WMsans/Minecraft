using Unity.Collections;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct CoordinateResult
    {
        public NativeArray<CoordinateData> points{get; private set;}
        public MeshSettings meshSettings { get; private set; }
        public TerrainConfiguration terrainConfiguration { get; private set; }
        public Vector2 MinMaxHeight { get; private set; }

        ReturnablePack pack;

        public CoordinateResult(ReturnablePack pack, NativeArray<CoordinateData> points, MeshSettings meshSettings, TerrainConfiguration terrainConfiguration, Vector2 MinMaxHeight)
        {
            this.points = points;
            this.pack = pack;
            this.meshSettings = meshSettings;
            this.terrainConfiguration = terrainConfiguration;
            this.MinMaxHeight = MinMaxHeight;
        }

        public void Return()
        {
            pack.Release();
        }
        public CoordinateData GetCoordinateDataAtGrid(Vector2 position, bool interpolated)
        {
            if (interpolated)
                return CoordinateDataResultInterpolated(points, meshSettings.Resolution, meshSettings.MeshScale, position, terrainConfiguration.Position);
            else
                return CoordinateDataResult(points, meshSettings.Resolution, meshSettings.MeshScale, position, terrainConfiguration.Position);
        }

        private CoordinateData CoordinateDataResult(NativeArray<CoordinateData> points, int resolution, float correctMeshScale, Vector2 simplePos, Vector3 position){
            Vector2 leftCorner = new Vector2(position.x, position.z)-new Vector2(correctMeshScale,correctMeshScale)/2f;
            Vector2 flatUV = (simplePos-leftCorner) / correctMeshScale;
            Vector2Int index = Vector2Int.RoundToInt(flatUV * resolution);
            return SampleData(points, resolution, index);
        }
        
        private CoordinateData CoordinateDataResultInterpolated(NativeArray<CoordinateData> points, int resolution, float correctMeshScale, Vector2 simplePos, Vector3 position){
            Vector2 leftCorner = new Vector2(position.x, position.z)-new Vector2(correctMeshScale,correctMeshScale)/2f;
            Vector2 flatUV = (simplePos-leftCorner)*resolution / correctMeshScale;
            
            Vector2Int indexA = Vector2Int.FloorToInt(flatUV);
            Vector2Int indexB = indexA+new Vector2Int(0, 1);
            Vector2Int indexC = indexA+new Vector2Int(1, 1);
            Vector2Int indexD = indexA+new Vector2Int(1, 0);

            Vector2 t = flatUV-indexA;

            var dataA = SampleData(points, resolution, indexA);
            var dataB = SampleData(points, resolution, indexB);
            var dataC = SampleData(points, resolution, indexC);
            var dataD = SampleData(points, resolution, indexD);

            CoordinateData XBottom = CoordinateData.Lerp(dataA, dataD, t.x);
            CoordinateData XTop = CoordinateData.Lerp(dataB, dataC, t.x);

            return CoordinateData.Lerp(XBottom, XTop, t.y);
        }

        private CoordinateData SampleData(NativeArray<CoordinateData> points, int resolution, Vector2Int index){
            index.x = Mathf.Clamp(index.x, 0, resolution);
            index.y = Mathf.Clamp(index.y, 0, resolution);

            if(index.x + index.y * (resolution + 1) < points.Length)
                return points[index.x + index.y * (resolution + 1)];
            else 
                return CoordinateData.Default;
        }
    }
}