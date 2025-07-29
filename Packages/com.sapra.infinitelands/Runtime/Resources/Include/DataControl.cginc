#include "..\Include\Random.cginc"

inline float2 VegetationUVFromIndex(uint2 index, 
    uint _chunkInstancesRow,
    float3 _chunkPosition, float _chunkSize,
    int _itemIndex, float _distanceBetween, float _positionRandomness,
    float _meshScale, out float3 idHash)
{ 
    uint x = index.x;
    uint y = index.y;

    float2 UV = _chunkSize*(index/(float)(_chunkInstancesRow))/_meshScale;
    UV += (_chunkPosition.xz-_chunkSize/2.0f)/_meshScale;

    idHash = randValue(uint3(x,y,_itemIndex));
    float indexOffset = _distanceBetween/_meshScale;
    float angle = 6.283185307 * idHash.z; 
    float radius = _positionRandomness * (0.5 + 0.5 * idHash.x); 
    float2 addition = float2(cos(angle), sin(angle)) * radius * indexOffset;

    UV.x += addition.x;
    UV.y += addition.y;

    return UV;
}

inline float2 ScaleForSamplers(float2 uv, int _resolution){
    return 1.0 / ((_resolution + 1) * 2) + uv * (1.0f - 1.0f / (_resolution + 1));
}

void UnMask(in uint value, out uint LodValue, out float NormalTransition, out uint VisibleNormal, out float ShadowTransition, out uint VisibleShadow){
    LodValue = value & 0xFF;
    NormalTransition = ((value >> 8) & 0x3FFF);
    VisibleNormal = (value >> 22) & 1;
    ShadowTransition = ((value >> 23) & 0xFF);
    VisibleShadow = (value >> 31) & 1;
}


uint2 CompactRotationScale(float4 rotQuat, float scale){
    float4 qn = normalize(rotQuat);
    float3 q = (qn.xyz+1.0)/2.0f;

    uint3 qs = q*0xFFFF;
    
    uint qxqy = (qs.x) | (qs.y << 16);
    uint qzs = (qs.z) | (f32tof16(scale) << 16);
    return uint2(qxqy, qzs);
}

uint CompactNormalTextureIndex(float3 normal, uint textureIndex, bool isValid){
    float3 nn = normalize(normal);
    float2 n = (nn.xz+1.0)/2.0f;

    uint2 ns = (uint2)(n*0xFFF);
    uint flagBit = isValid ? 1 : 0; // Convert bool to uint (1 or 0)

    uint clampedTextureIndex = (uint)(clamp(textureIndex,0, 0x7F)); // 0x7F = 127

    return (ns.x) | (ns.y << 12) | (clampedTextureIndex << 24) | (flagBit << 31);
}

void UnpackTextureIndex(in uint normal_index, out uint textureIndex){
	textureIndex = ((normal_index >> 24) & 0x7F);
}

void UnpackNormal(in uint normal_index, out float3 normal){
	uint nx = normal_index & 0xFFF;
	uint nz = (normal_index >> 12) & 0xFFF;
	float2 nxz = (float2(nx,nz)/0xFFF)*2.0f-1.0f;
	float ny = 1.0-(nxz.x*nxz.x+nxz.y*nxz.y);
	normal = normalize(float3(nxz.x, ny > 0.0 ? sqrt(ny):0.0, nxz.y));
}
void UnpackIsValid(in uint normal_index, out bool isValid){
	isValid = (normal_index >> 31) != 0;
}

void UnpackRotationScale(in uint2 quat_scale, out float4 quaternion, out float3 scale){
	uint qx = quat_scale.x & 0xFFFF;
	uint qy = (quat_scale.x >> 16) & 0xFFFF;
	uint qz = quat_scale.y & 0xFFFF;

	float3 qxyz = (float3(qx,qy,qz)/0xFFFF)*2.0f-1.0f;
	float qw = 1-(qxyz.x*qxyz.x+qxyz.y*qxyz.y+qxyz.z*qxyz.z);
	
	quaternion = float4(qxyz, qw > 0.0 ? sqrt(qw):0.0);

	float s = f16tof32((quat_scale.y >> 16) & 0xFFFF);
	scale = float3(s,s,s);
}