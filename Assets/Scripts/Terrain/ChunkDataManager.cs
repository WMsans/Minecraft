using System; 
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChunkDataManager : IDisposable
{
    private Dictionary<int, ChunkData> activeChunkData = new Dictionary<int, ChunkData>();
    private string savePath;

    public ChunkDataManager(string worldName)
    {
        savePath = Path.Combine(Application.persistentDataPath, worldName);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }

    public ChunkData GetChunkData(int nodeIndex)
    {
        if (activeChunkData.TryGetValue(nodeIndex, out var data))
        {
            return data;
        }

        return LoadChunkData(nodeIndex);
    }

    private ChunkData LoadChunkData(int nodeIndex)
    {
        string filePath = GetChunkDataPath(nodeIndex);
        if (File.Exists(filePath))
        {
            // In a real implementation, you would deserialize the data from the file.
        }

        var newChunkData = new ChunkData();
        newChunkData.Allocate();
        activeChunkData[nodeIndex] = newChunkData;
        return newChunkData;
    }

    public void SaveChunkData(int nodeIndex)
    {
        if (activeChunkData.TryGetValue(nodeIndex, out var data))
        {
            string filePath = GetChunkDataPath(nodeIndex);
            // In a real implementation, you would serialize the data and save it to the file.
        }
    }

    public void UnloadChunkData(int nodeIndex)
    {
        if (activeChunkData.TryGetValue(nodeIndex, out var data))
        {
            data.Dispose();
            activeChunkData.Remove(nodeIndex);
        }
    }

    private string GetChunkDataPath(int nodeIndex)
    {
        return Path.Combine(savePath, $"chunk_{nodeIndex}.dat");
    }

    public void Dispose()
    {
        foreach (var chunkData in activeChunkData.Values)
        {
            chunkData.Dispose();
        }
        activeChunkData.Clear();
    }
}