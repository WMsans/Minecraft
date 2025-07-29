using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class HeightMapBranch{
        private PathData path;
        private NativeArray<float> Map;

        public void Reuse(PathData path, BranchData branch)
        {
            this.path = path;
            this.Map = branch.GetData<ReturnableBranch>().GetData<float>(path.TotalLength);
        }
        
        public NativeArray<float> GetMap() => Map;
        public int GetMaxResolution() => path.MaxResolution;

        /// <summary>
        /// Returns an array with floats to be set and read, used by all the nodes that will calculate data in that branch
        /// </summary>
        /// <param name="branchID">ID representing the branch</param>
        /// <param name="ID">ID representing the generation call</param>
        public IndexAndResolution GetAllocationSpace(InfiniteLandsNode node, string fieldName, out NativeArray<float> map)
        {
            map = Map;
            return path.GetSpace(node, fieldName);
        }
        
        public IndexAndResolution GetAllocationSpaceVectorized(InfiniteLandsNode node, string fieldName, out NativeSlice<float4> map){
            var targetSpace = path.GetSpace(node, fieldName);
            map = Map.Slice(targetSpace.StartIndex, targetSpace.Length).SliceConvert<float4>();
            return targetSpace;
        }


        public IndexAndResolution GetAllocationSpace(InfiniteLandsNode node, string fieldName)
        {
            return path.GetSpace(node, fieldName);
        }

    }
}