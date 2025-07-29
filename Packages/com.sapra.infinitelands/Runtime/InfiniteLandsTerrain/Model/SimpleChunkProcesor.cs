using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IProcessChunk
    {
        public Vector3Int GetID();
        public bool IsCompleted();
    }

    public class SimpleChunkProcesor<R> where R : struct, IProcessChunk
    {
        private List<R> InProgress = new();
        private List<Vector3Int> Invalids = new();

        private IControlTerrain infiniteLands;
        public Action<R, bool> CompleteAction;
        public Func<ChunkData, (R process, bool couldCreate)> CreateAction;

        public SimpleChunkProcesor(IControlTerrain infiniteLands, Action<R, bool> completeAction, Func<ChunkData, (R, bool)> createAction)
        {
            this.infiniteLands = infiniteLands;
            CompleteAction = completeAction;
            CreateAction = createAction;

            InProgress = new();
            Invalids = new();
        }

        public void UpdateProcesses() => UpdateInProgressItems(false);
        public void DisableProcessor()
        {
            foreach (var process in InProgress)
            {
                CompleteAction(process, true);

            }
            InProgress.Clear();
            Invalids.Clear();
        }
        public void OnProcessAdded(ChunkData chunk)
        {
            var result = CreateAction(chunk);
            if (result.couldCreate)
            {
                InProgress.Add(result.process);
            }
            if (infiniteLands.InstantProcessors)
                UpdateInProgressItems(true);
        }
        public void OnProcessRemoved(ChunkData chunk)
        {
            foreach (var process in InProgress)
            {
                if (process.GetID().Equals(chunk.ID))
                {
                    Invalids.Add(chunk.ID);
                    break;
                }
            }
        }

        protected void UpdateInProgressItems(bool instantGeneration)
        {
            if (InProgress.Count <= 0)
                return;

            for (int i = InProgress.Count - 1; i >= 0; i--)
            {
                var process = InProgress[i];
                if (process.IsCompleted() || instantGeneration)
                {
                    CompleteAction(process, Invalids.Contains(process.GetID()));
                    InProgress.RemoveAt(i);
                }
            }
        }
    }
}