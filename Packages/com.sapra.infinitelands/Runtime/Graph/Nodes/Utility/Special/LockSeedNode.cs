using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Windows;

namespace sapra.InfiniteLands
{
    [CustomNode("Lock Seed", docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/lockseed")]
    public class LockSeedNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public object Input;
        [Output(match_type_name: nameof(Input))] public object Output;
        public int Seed;
        public bool AbsoluteSeedValue = true;

        private BranchData newBranch;
        private Type targetType;

        public void  ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            targetType = RuntimeTools.GetTypeFromInputField(nameof(Input), this, Graph);
            if (targetType.Equals(typeof(HeightData)))
            {
                currentBranch.AllocateInput(this, nameof(Input), acomulatedResolution);
                currentBranch.AllocateOutputSpace(this, nameof(Output));
            }
        }
        public override bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            var dataCreated = base.TryGetOutputData<object>(branch, out var result, fieldName, listIndex);
            if (dataCreated && result != null)
                data = (T)result;
            else
                data = default;
            return dataCreated;
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return true;
        }
        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                MeshSettings newMeshSettings = new MeshSettings(branch.meshSettings);
                newMeshSettings.Seed = AbsoluteSeedValue ? Seed : newMeshSettings.Seed + Seed;
                newBranch = BranchData.NewMeshSettings(newMeshSettings, branch, GetNodesInInput(nameof(Input)));
                targetType = RuntimeTools.GetTypeFromInputField(nameof(Input), this, Graph);
                state.IncreaseSubState();
            }

            if (state.SubState == 1)
            {
                if (targetType == typeof(HeightData))
                {
                    if (!TryGetInputData<HeightData>(newBranch, out var heightData, nameof(Input)))
                    {
                        Debug.LogError("something went wrong");
                        return true;
                    }

                    HeightMapBranch fromHeightBranch = newBranch.GetData<HeightMapBranch>();
                    var from = fromHeightBranch.GetMap();
                    HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                    var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var to);
                    JobHandle job = CopyToFrom.ScheduleParallel(to, from,
                        targetSpace, heightData.indexData,
                        heightData.jobHandle);

                    Output = new HeightData(job, targetSpace, heightData.minMaxValue);
                }
                else
                {
                    if (!TryGetInputData<object>(newBranch, out var result, nameof(Input)))
                        Debug.Log("sad");
                    Output = result;
                }
                state.IncreaseSubState();
            }

            return state.SubState == 2;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}
