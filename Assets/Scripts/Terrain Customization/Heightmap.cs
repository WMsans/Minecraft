using Unity.Collections;
using Unity.Mathematics;

public struct Heightmap
{
    public NativeArray<float> heights;
    public int2 size;

    public Heightmap(int2 size, Allocator allocator)
    {
        this.size = size;
        heights = new NativeArray<float>(size.x * size.y, allocator);
    }

    public void Dispose()
    {
        if (heights.IsCreated)
        {
            heights.Dispose();
        }
    }
}