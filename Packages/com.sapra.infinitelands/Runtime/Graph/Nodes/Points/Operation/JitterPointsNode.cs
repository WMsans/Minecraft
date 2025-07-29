using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace sapra.InfiniteLands
{
    [CustomNode("Jitter Points", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/operations/jitter", synonims = new string[]{"Adjust", "Move", "Scale", "Rotate"})]
    public class JitterPointsNode : InfiniteLandsNode, IProcessPoints
    {
        [Input] public PointInstance Points;
        [Input,Disabled] public HeightData Position;
        [Input,Disabled] public HeightData Rotation;
        [Input,Disabled] public HeightData Scale;

        [Output] public PointInstance Output;

        public string processorID => guid;

        private PointManager manager;
        private MeshSettings meshSettings;
        private PointInstance previousPoints;
        private AwaitableData<HeightData> awaitableData;
        private HeightData finalHeight;
        private TreeData newSettings;

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out previousPoints, nameof(Points));
        }

        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                manager = branch.GetGlobalData<PointManager>();
                meshSettings = new MeshSettings()
                {
                    Resolution = 1,
                    MeshScale = 100,
                    Seed = branch.meshSettings.Seed
                };
                if (manager.TryGetValue(this, branch.meshSettings.Seed, out Output, out _))
                    state.SetSubState(30);
                else
                    state.IncreaseSubState();
            }

            if (state.SubState == 1)
            {
                if (!IsAssigned(nameof(Position)))
                {
                    Output = manager.GetPointInstance(this, previousPoints.GridSize, previousPoints, branch.meshSettings.Seed);
                    state.SetSubState(30);
                }
                else
                {
                    awaitableData = manager.ExtractHeightData(this, nameof(Position), Vector3.zero, meshSettings, out newSettings);
                    state.IncreaseSubState();
                }
            }

            if (state.SubState == 2)
            {
                if (!awaitableData.ProcessData()) return false;
                finalHeight = awaitableData.Result;
                state.IncreaseSubState();
            }

            if (state.SubState == 3)
            {
                if (!branch.ForcedOrFinished(finalHeight.jobHandle)) return false;

                float MaxDistance = Mathf.Max(finalHeight.minMaxValue.y - finalHeight.minMaxValue.x, previousPoints.GridSize);
                Output = manager.GetPointInstance(this, MaxDistance, previousPoints, meshSettings.Seed);
                newSettings.CloseTree();
                state.SetSubState(30);
            }
            return state.SubState == 30;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
        
        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            var updater = GenericPoolLight<UpdateManyPoints>.Get();
            updater.Reuse(this, currentPoints, pointSettings);
            return updater;
        }

        private class UpdateManyPoints : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result{get; private set;}
            int SubState = 0;

            private List<AwaitableData<PointTransform>> RunningProcesses = new();
            private PointGenerationSettings pointSettings;
            private PointInstance previousPoints;
            private PointInstance currentPoints;
            private bool tweakPosition;
            private bool tweakRotation;
            private bool tweakScale;

            private JitterPointsNode node;
            public UpdateManyPoints(){
                Result = new();
                RunningProcesses = new();
            }
            public void Reuse(JitterPointsNode node, PointInstance currentPoints, PointGenerationSettings pointSettings){
                tweakPosition = node.IsAssigned(nameof(Position));
                tweakRotation = node.IsAssigned(nameof(Rotation));
                tweakScale = node.IsAssigned(nameof(Scale));
                this.node = node;
                this.previousPoints = currentPoints.PreviousInstance;
                this.currentPoints = currentPoints;
                this.pointSettings = pointSettings;

                SubState = 0;
                Result.Clear();
                RunningProcesses.Clear();
            }
            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!previousPoints.GetAllPoints(pointSettings, out var foundPoints)) return false;
                    foreach(var point in foundPoints){
                        var waiter = GenericPoolLight<SinglePointUpdate>.Get();
                        waiter.Reuse(point, node, currentPoints, tweakPosition, tweakRotation, tweakScale);
                        RunningProcesses.Add(waiter);
                    }
                    SubState++;
                }

                if(SubState == 1){
                    if(AwaitableTools.CompactData(RunningProcesses, Result)){
                        GenericPoolLight.Release(this);
                        SubState++;
                    }
                }

                return SubState == 2;
            }
        }
        private class SinglePointUpdate : AwaitableData<PointTransform>
        {
            public PointTransform Result{get; set;}
            private PointTransform initialPoint;
            private JitterPointsNode node;
            private PointInstance currentPoints;

            private HeightAtPoint AwaiterPosX;
            private HeightAtPoint AwaiterPosY;

            private HeightAtPoint AwaiterRotation;
            private HeightAtPoint AwaiterScale;

            private bool tweakPosition;
            private bool tweakRotation;
            private bool tweakScale;

            private bool posDone;
            private bool rotDone;
            private bool scaDone;

            private bool posSet;
            private bool rotSet;
            private bool scaSet;

            private int Seed;

            public void Reuse(PointTransform point, JitterPointsNode node, PointInstance currentPoints,
                bool tweakPosition, bool tweakRotation, bool tweakScale)
            {

                initialPoint = point;
                this.currentPoints = currentPoints;
                Result = initialPoint;
                this.node = node;
                this.Seed = currentPoints.Seed;
                this.tweakPosition = tweakPosition;
                this.tweakRotation = tweakRotation;
                this.tweakScale = tweakScale;

                posDone = false;
                rotDone = false;
                scaDone = false;

                posSet = false;
                rotSet = false;
                scaSet = false;
            }

            public bool ProcessData()
            {               
                posDone = !tweakPosition || (tweakPosition && TweakPosition(Result));
                rotDone = !tweakRotation || (tweakRotation && TweakRotation(Result));
                scaDone = !tweakScale || (tweakScale && TweakScale(Result));
                bool completed = posDone && rotDone && scaDone;
                if(completed)
                    GenericPoolLight.Release(this);
                return completed;
            }

            private bool TweakPosition(PointTransform original){
                if(posDone)
                    return true;
                if (!posSet)
                {
                    MeshSettings meshSettingsX = new MeshSettings()
                    {
                        Resolution = 1,
                        MeshScale = 100,
                        Seed = Seed,
                    };
                    MeshSettings meshSettingsY = new MeshSettings()
                    {
                        Resolution = 1,
                        MeshScale = 100,
                        Seed = Seed + 1,
                    };
                    AwaiterPosX = currentPoints.GetDataAtPoint(node, nameof(Position), initialPoint.Position, meshSettingsX);
                    AwaiterPosY = currentPoints.GetDataAtPoint(node, nameof(Position), initialPoint.Position, meshSettingsY);
                    posSet = true;
                }

                if(!AwaiterPosX.ProcessData()) return false;
                if(!AwaiterPosY.ProcessData()) return false;
                
                float normalixedX = Mathf.InverseLerp(AwaiterPosX.MinMaxRange.x, AwaiterPosX.MinMaxRange.y, AwaiterPosX.Result);
                float angle = 6.283185307f * normalixedX; 
                
                float3 jittered = new float3(Mathf.Cos(angle), 0, Mathf.Sin(angle))*AwaiterPosY.Result;
                Result = original.UpdatePosition(jittered+initialPoint.Position);
                return true;
                
            }

            private bool TweakRotation(PointTransform original){
                if(rotDone)
                    return true;
                if(!rotSet)
                {
                    MeshSettings meshSettings = new MeshSettings(){
                        Resolution = 1,
                        MeshScale = 100,
                        Seed = Seed,
                    };
                    AwaiterRotation = currentPoints.GetDataAtPoint(node, nameof(Rotation), initialPoint.Position, meshSettings);
                    rotSet = true;
                }

                if(!AwaiterRotation.ProcessData()) return false;
                Result = original.UpdateRotation(AwaiterRotation.Result+initialPoint.YRotation);
                return true;
                
            }
            private bool TweakScale(PointTransform original){
                if(scaDone)
                    return true;
                if (!scaSet)
                {
                    MeshSettings meshSettings = new MeshSettings()
                    {
                        Resolution = 1,
                        MeshScale = 100,
                        Seed = Seed,
                    };
                    AwaiterScale = currentPoints.GetDataAtPoint(node, nameof(Scale), initialPoint.Position, meshSettings);
                    scaSet = true;
                }

                if(!AwaiterScale.ProcessData()) return false;
                Result = original.UpdateScale(AwaiterScale.Result);
                return true;
            }

        }
    }
}