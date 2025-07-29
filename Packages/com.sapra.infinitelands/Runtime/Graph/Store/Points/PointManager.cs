using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class PointManager
    {
        public static Dictionary<(string id, int seed), string> PointInstanceName = new();
        public static string GetPointInstanceKey(string id, int seed)
        {
            if (!PointInstanceName.TryGetValue((id, seed), out var key))
            {
                key = id + seed;
                PointInstanceName.Add((id, seed), key);
            }
            return key;
        }
        public Dictionary<string, PointInstance> PointInstances = new();
        private StringObjectStore<object> globalStore;
        public PointManager(StringObjectStore<object> globalStore){
            this.globalStore = globalStore;
        }
        public bool TryGetValue(IProcessPoints processor, int seed, out PointInstance pointInstance, out string key){
            key = GetPointInstanceKey(processor.processorID, seed);
            return PointInstances.TryGetValue(key, out pointInstance);
        }
        private PointInstance GetPointInstance(IProcessPoints processor, int seed, ref PointInstanceFactory GenerateNewInstance){
            if(!TryGetValue(processor, seed, out var pointInstance, out string key)){
                pointInstance = GenerateNewInstance.Create();
                PointInstances.Add(key, pointInstance);
            }
            return pointInstance;
        }

        public PointInstance GetPointInstance(IProcessPoints processor,
            float gridSize, PointInstance dependancy, int seed, bool newPoints = false)
        {
            var factory = new PointInstanceFactory(processor, seed, gridSize, newPoints, dependancy, this);
            return GetPointInstance(processor, seed, ref factory);
        }

        public HeightAtPoint GetDataAtPoint(InfiniteLandsNode node, string fieldName, 
            Vector3 position, MeshSettings meshSettings)
        {
            
            var heightData = ExtractHeightData(node, fieldName, position, meshSettings, out var newSettings);
            HeightAtPoint extractor = new HeightAtPoint(heightData, newSettings);
            return extractor;
        }

        public HeighDataExtractor ExtractHeightData(InfiniteLandsNode node, string fieldName, Vector3 position, MeshSettings meshSettings, out TreeData newSettings){
            return new HeighDataExtractor(node, fieldName, globalStore, position, meshSettings, out newSettings);
        }

        private struct PointInstanceFactory : IFactory<PointInstance>
        {
            IProcessPoints Processor;
            float gridSize;
            int seed;
            bool newPoints;
            PointInstance PreviousInstance;
            PointManager PointManager;

            public PointInstanceFactory(IProcessPoints Processor, int seed, float gridSize, bool newPoints, PointInstance PreviousInstance, PointManager pointManager){
                this.Processor = Processor;
                this.gridSize = gridSize;
                this.PreviousInstance = PreviousInstance;
                this.newPoints = newPoints;
                this.PointManager = pointManager;
                this.seed = seed;
            }
            public PointInstance Create()
            {
                return new PointInstance(Processor, gridSize, PreviousInstance, PointManager, seed, newPoints);
            }
        }
    }
}