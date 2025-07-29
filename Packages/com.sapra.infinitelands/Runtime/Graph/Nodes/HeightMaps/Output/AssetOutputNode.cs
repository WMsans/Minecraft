using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Asset Output", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/output/assetoutput", synonims = new string[]{"Vegetation", "Textures","Pack"})]
    public class AssetOutputNode : InfiniteLandsNode, ILoadAsset, ISetAsset
    {
        public InfiniteLandsAsset Asset;

        [Input] public HeightData Density;

        [field: SerializeField] public ILoadAsset.Operation action{get; private set;}

        public string OutputVariableName => nameof(Density);
        public IEnumerable<IAsset> GetAssets(){
            if(Asset is IHoldManyAssets manyAssets){
                return manyAssets.GetAssets();
            }else{
                return new[]{Asset};
            }
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Density, nameof(Density));
        }

        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                if (!branch.ForcedOrFinished(Density.jobHandle)) return false;
                state.IncreaseSubState();
            }
            return state.SubState == 1;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Density, nameof(Density));
        }

        public bool AssetExists(IAsset asset) => GetAssets().Contains(asset);
        public void SetAsset(InfiniteLandsAsset asset) => Asset = asset;
    }
}