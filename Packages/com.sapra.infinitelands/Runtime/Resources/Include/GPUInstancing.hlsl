// https://twitter.com/Cyanilux/status/1396848736022802435?s=20

#ifndef GRASS_INSTANCED_INCLUDED
#define GRASS_INSTANCED_INCLUDED
#define mask 0xFF

#include "..\Include\Quaternion.cginc"
#include "..\Include\DataControl.cginc"

struct InstanceData {
    float3 position;
    uint2 quaternionScale;
    uint normalandTexture;
};

struct SumPack{
    uint startBase;
    uint countBase;

    uint startTrans;
    uint countTrans;
};

StructuredBuffer<InstanceData> _PerInstanceData;
StructuredBuffer<uint> _Indices;
StructuredBuffer<SumPack> _Counters;
StructuredBuffer<uint> _TargetLODs;

int _LODValue;
int _LODCount;
int _ShadowLodOffset;
int _TransitionEnabled;

float4x4 inverse(float4x4 input)
{
#define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
    float4x4 cofactors = float4x4(
        minor(_22_23_24, _32_33_34, _42_43_44),
        -minor(_21_23_24, _31_33_34, _41_43_44),
        minor(_21_22_24, _31_32_34, _41_42_44),
        -minor(_21_22_23, _31_32_33, _41_42_43),
 
        -minor(_12_13_14, _32_33_34, _42_43_44),
        minor(_11_13_14, _31_33_34, _41_43_44),
        -minor(_11_12_14, _31_32_34, _41_42_44),
        minor(_11_12_13, _31_32_33, _41_42_43),
 
        minor(_12_13_14, _22_23_24, _42_43_44),
        -minor(_11_13_14, _21_23_24, _41_43_44),
        minor(_11_12_14, _21_22_24, _41_42_44),
        -minor(_11_12_13, _21_22_23, _41_42_43),
 
        -minor(_12_13_14, _22_23_24, _32_33_34),
        minor(_11_13_14, _21_23_24, _31_33_34),
        -minor(_11_12_14, _21_22_24, _31_32_34),
        minor(_11_12_13, _21_22_23, _31_32_33)
        );
#undef minor
    return transpose(cofactors) / determinant(input);
}

float4x4 MakeTRSMatrix(float3 pos, float4 rotQuat, float3 scale)
{
    float4x4 rotPart = QuatToMatrix(rotQuat);
    float4x4 trPart = float4x4(
        float4(scale.x, 0, 0, 0), 
        float4(0, scale.y, 0, 0), 
        float4(0, 0, scale.z, 0), 
        float4(pos, 1));
    return mul(rotPart, trPart);
}

float UnMask(in uint value){
	uint NormalIndex = value & 0xFF;
	if(_ShadowLodOffset >= 0){
		float ShadowTransition = ((value >> 23) & 0xFF)/255.0f;
		return NormalIndex+_ShadowLodOffset+ShadowTransition;
	}
	else{
		float NormalTransition = ((value >> 8) & 0x3FFF)/16383.0f;
		return NormalIndex+NormalTransition;
	}
	
}

void initializeInfiniteLands(InstanceData data){
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    #ifdef unity_ObjectToWorld
    #undef unity_ObjectToWorld
    #endif

    #ifdef unity_WorldToObject
    #undef unity_WorldToObject
    #endif
   
    float4 rotation;
    float3 scale;
    UnpackRotationScale(data.quaternionScale, rotation, scale);
    float4x4 trs = transpose(MakeTRSMatrix(data.position, rotation, scale));
    #undef unity_ObjectToWorld
    #undef unity_WorldToObject
    unity_ObjectToWorld = trs;
    unity_WorldToObject = inverse(trs);
    
    #if SHADERPASS == SHADERPASS_MOTION_VECTORS && defined(SHADERPASS_CS_HLSL)
        unity_MatrixPreviousM = unity_ObjectToWorld;
        unity_MatrixPreviousMI = unity_WorldToObject;
    #endif
#endif
}

void dummySHCall() {
}

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
InstanceData GetInstanceData(out int instanceIndex){
    instanceIndex = _Indices[_Counters[_LODValue].startBase+unity_InstanceID];
    return _PerInstanceData[instanceIndex];
}
#endif 

void IL_Initialize() {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        int instanceIndex;
        InstanceData data = GetInstanceData(instanceIndex);
        initializeInfiniteLands(data);
    #endif 
}
void TransfromPosition_float(in float3 objectPosition, out float3 position, out float3 worldPosition, out float transition, out uint textureIndex, out float3 groundNormal){
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        int instanceIndex;
        InstanceData data = GetInstanceData(instanceIndex);

        worldPosition = data.position;

        initializeInfiniteLands(data);

        if(_TransitionEnabled > 0){
            float current = UnMask(_TargetLODs[instanceIndex]);
            transition = saturate(abs(_LODValue-current)*2.0-1.0);
        }
        else{
            transition = 0;
        }
        
        UnpackNormal(data.normalandTexture, groundNormal);
        UnpackTextureIndex(data.normalandTexture, textureIndex);
        
    #else
        worldPosition = 0;
        transition = 0;
        textureIndex = 0;
        groundNormal = 0;
    #endif

	position = objectPosition;
}
#endif