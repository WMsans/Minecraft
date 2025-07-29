using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class HeightMapManager : IInitializeBranch, ICloseBranch
    {
        private static Dictionary<string, string> NormalsNames = new();
        private Dictionary<(string guid, int resolution, float meshScale), PathData> pathData = new();
        public IGraph graph;

        public HeightMapManager(IGraph graph){
            this.graph = graph;
        }

        public void InitializeBranch(BranchData branch, BranchData previousBranch)
        {
            var meshSettings = branch.meshSettings;
            var path = GetPathData(meshSettings, branch.StartingNodes);
            //int finalResolution = path.MaxResolution;
            //branch.meshSettings = meshSettings.ModifyResolution(finalResolution);
            var allocatedBranch = GenericPoolLight<HeightMapBranch>.Get();
            allocatedBranch.Reuse(path, branch);
            branch.AddData(allocatedBranch);
        }

        
        public void CloseBranch(BranchData branch)
        {
            var allocatedBranch = branch.GetData<HeightMapBranch>();
            GenericPoolLight.Release(allocatedBranch);
        }


        public PathData GetPathData(MeshSettings meshSettings, InfiniteLandsNode[] startingNodes){
            if (startingNodes.Length <= 0)
            {
                Debug.LogError("no starting nodes");
                return default;
            }
            
            var node = startingNodes[0];
            var key = GetPathKey(meshSettings, node);
            if(pathData.TryGetValue(key, out var path)){
                return path;
            }
            else{
                path = new PathData(this, meshSettings.ScaleToResolution, startingNodes);
                pathData[key] = path;
                path.StartNodeApplication(meshSettings.Resolution);
                return path;
            }
        }

        private (string guid, int resolution, float meshScale) GetPathKey(MeshSettings settings, InfiniteLandsNode node)
        {
            return (node.guid, settings.Resolution, settings.MeshScale);
        }

        public static string GetNormalMapName(string fieldName){
            if(!NormalsNames.TryGetValue(fieldName, out var key)){
                key = fieldName+"-normals";
                NormalsNames.Add(fieldName, key);
            }
            return key;
        }
    }
}