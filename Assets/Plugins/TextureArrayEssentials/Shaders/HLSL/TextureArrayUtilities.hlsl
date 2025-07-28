#ifndef TEXTUREARRAYUTILITIES_INCLUDED
#define TEXTUREARRAYUTILITIES_INCLUDED

#include "BooleanCompression.hlsl"

void GetIndexInArray_float(int TexturesAssignedCompressed, int Index, out int Out)
{
    // Get the index of the texture in the array
    float arrayIndex = 0;
    for (int i = 0; i < Index; i++)
    {
        bool assigned = GetCompressedValue(TexturesAssignedCompressed, i);
        
        if (assigned)
        {
            arrayIndex++;
        }
    }

    Out = arrayIndex;
}

void SampleArrayAtConstantIndex_float(UnityTexture2DArray TextureArray, int TexturesAssignedCompressed, int Index, float2 UV, float4 UnassignedColor, SamplerState SS, out float4 Out)
{
    // Only output texture if it is assigned in inspector
    bool indexExists = false;
    GetCompressedValue_float(TexturesAssignedCompressed, Index, indexExists);
    if (!indexExists) {
        Out = UnassignedColor;
        return;
    }
    
    // Get the index of the texture in the array
    float arrayIndex = 0;
    GetIndexInArray_float(TexturesAssignedCompressed, Index, arrayIndex);
    
    // Sample the array at the index found previously
    Out = SAMPLE_TEXTURE2D_ARRAY(TextureArray, SS, UV, arrayIndex);
}

#endif