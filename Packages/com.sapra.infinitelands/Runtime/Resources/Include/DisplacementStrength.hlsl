#ifndef DISPLACEMENT_INCLUDED
#define DISPLACEMENT_INCLUDED
sampler2D _DisplaceTexture;
int _DisplacementTextureIsSet;
float3 _DisplaceTexturePosition;
float2 _DisplaceTextureBoundary;
float _DisplaceSize;

void GetDisplacementStrength_float(in float3 grassPosition, in float3 idHash, out float totalStrength, out float2 dv){
    if(_DisplacementTextureIsSet == 0){
        totalStrength = 0;
        dv = 0;
    }else{
        //Apply displacement
        float2 displaceUV = (grassPosition.xz-_DisplaceTexturePosition.xz+(idHash.xy-0.5f)*.2)/(_DisplaceSize*2.f);
        float4 displaceVector = tex2Dlod(_DisplaceTexture, float4(displaceUV+.5f, 0.0, 0.0));
        float displace_strenght = saturate(displaceVector.a+(displaceVector.a*idHash.x*.2f));
        float heightValue = lerp(_DisplaceTextureBoundary.x, _DisplaceTextureBoundary.y,displaceVector.y);
        float height_strenght = saturate(1-(heightValue-grassPosition.y));

        totalStrength = height_strenght*displace_strenght;
        dv = (displaceVector.xz-.5f)*2*saturate(totalStrength);
    }
}

void GetDisplacementStrength_half(in half3 grassPosition, in half3 idHash, out half totalStrength, out half2 dv){
    if(_DisplacementTextureIsSet == 0){
        totalStrength = 0;
        dv = 0;
    }else{
        //Apply displacement
        half2 displaceUV = (grassPosition.xz-_DisplaceTexturePosition.xz+(idHash.xy-0.5f)*.2)/(_DisplaceSize*2.f);
        half4 displaceVector = tex2Dlod(_DisplaceTexture, float4(displaceUV+.5f, 0.0, 0.0));
        half displace_strenght = saturate(displaceVector.a+(displaceVector.a*idHash.x*.2f));
        half heightValue = lerp(_DisplaceTextureBoundary.x, _DisplaceTextureBoundary.y,displaceVector.y);
        half height_strenght = saturate(1-(heightValue-grassPosition.y));

        totalStrength = height_strenght*displace_strenght;
        dv = (displaceVector.xz-.5f)*2*saturate(totalStrength);
    }
}
#endif //MYHLSLINCLUDE_INCLUDED