using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public static class AssetDataHelper
    {
        public static AssetDataCompact GetCompactAssetData<T>(ChunkData chunkData, List<AssetWithLoaders<T>> assetHolders)
        {
            int assetCount = assetHolders.Count;
            var tree = chunkData.GetVariantTree();
            var originalSettings = tree.OriginalMeshSettings;

            var dataToManage = ListPoolLight<NativeArray<DataToManage>>.Get();
            var jobs = new NativeArray<JobHandle>(assetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            BranchData settings = tree.GetTrunk();
            ReturnableManager manager = settings.GetGlobalData<ReturnableManager>();

            for (int ah = 0; ah < assetCount; ah++)
            {
                AssetWithLoaders<T> assetHolder = assetHolders[ah];
                ILoadAsset[] loaders = assetHolder.loaders;
                int loadersLength = loaders.Length;
                NativeArray<JobHandle> internalJobs = new NativeArray<JobHandle>(loadersLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<DataToManage> internalIndices = new NativeArray<DataToManage>(loadersLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < loadersLength; i++)
                {
                    var loader = loaders[i];
                    var node = (InfiniteLandsNode)loader;
                    var writableNode = settings.GetWriteableNode(node);
                    if (!writableNode.TryGetOutputData<HeightData>(settings, out var output, loader.OutputVariableName))
                        Debug.LogError("Couldn't get output data");
                    internalJobs[i] = output.jobHandle;
                    internalIndices[i] = new DataToManage(output.indexData, loader.action);
                }

                jobs[ah] = JobHandle.CombineDependencies(internalJobs);
                internalJobs.Dispose();

                dataToManage.Add(internalIndices);
            }
            
            JobHandle afterVegetationCreated = JobHandle.CombineDependencies(jobs);
            jobs.Dispose();

            int dataLength = dataToManage.Count;
            var mapLength = MapTools.LengthFromResolution(originalSettings.Resolution);

            ReturnablePack returnablePack = GenericPoolLight<ReturnablePack>.Get();
            NativeArray<float> finalTargetMap = manager.GetData<float>(returnablePack, assetCount * mapLength);
            NativeArray<JobHandle> CombineJobs = new NativeArray<JobHandle>(dataLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var heightBranch = settings.GetData<HeightMapBranch>();
            var map = heightBranch.GetMap();
            List<IndexAndResolution> indexAndResolutions = new();
            for (int i = 0; i < assetCount; i++)
            {
                IndexAndResolution targetSpace = new IndexAndResolution(i*mapLength, originalSettings.Resolution, mapLength);
                indexAndResolutions.Add(targetSpace);
                CombineJobs[i] = MJDensityCombine.ScheduleParallel(map, finalTargetMap, dataToManage[i], targetSpace, afterVegetationCreated);
            }

            JobHandle afterCombined = JobHandle.CombineDependencies(CombineJobs);
            afterCombined.Complete();
            CombineJobs.Dispose();

            for (int i = 0; i < dataLength; i++)
            {
                dataToManage[i].Dispose(afterCombined);
            }
            ListPoolLight<NativeArray<DataToManage>>.Release(dataToManage);

            return new AssetDataCompact(finalTargetMap, assetCount, mapLength, assetHolders.Select(a => a.asset), chunkData, returnablePack, afterCombined);
        }

        public static int MaxOrInvalid(int i, int max)
        {
            if (i >= max)
                return -1;
            return i;
        }
        public static IEnumerable<T> GetAssets<T>(IGraph graph)
        {
            return graph.GetAllNodes().
                Where(a => a != null && a.isValid).
                OfType<ILoadAsset>().
                SelectMany(a => a.GetAssets().Where(a => a != null))
                .OfType<T>()
                .Distinct();
        }
        
        public static List<AssetWithLoaders<T>> GetAssetsWithHolder<T>(IGraph graph)
        {
            var assets = graph.GetAllNodes().
                Where(a => a != null && a.isValid).
                OfType<ILoadAsset>().
                SelectMany(a => a.GetAssets().Where(a => a != null))
                .Distinct();

            List<AssetWithLoaders<T>> resultingData = new();
            foreach (var asset in assets)
            {
                if (asset is T casted) {
                    resultingData.Add(new AssetWithLoaders<T>()
                    {
                        asset = asset,
                        casted = casted,
                        loaders = GetAsetLoaderOf(asset, graph).ToArray()
                    });
                }
            }
            return resultingData;
        }
        
        public static IEnumerable<ILoadAsset> GetAsetLoaderOf<M>(M asset, IGraph graph) where M : IAsset => graph.GetValidNodes().OfType<ILoadAsset>().Where(a => a.AssetExists(asset));

    }
}