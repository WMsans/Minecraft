
//UNITY_SHADER_NO_UPGRADE
#ifndef USEFULTEXTURES_INCLUDED
#define USEFULTEXTURES_INCLUDED
#define SimpleSample
#include "GetTextureIndicesCommon.hlsl"

void GetIndices_float(in float2 correctUV, UnityTexture2DArray textureMasks, UnitySamplerState sampler_splatMap, out int4 highestIndices, out float4 highestWeights){  
    SampleIndices(correctUV, textureMasks, sampler_splatMap, highestIndices, highestWeights);
}
#endif //MYHLSLINCLUDE_INCLUDED
