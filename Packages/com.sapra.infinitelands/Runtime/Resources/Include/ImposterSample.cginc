#ifndef XRA_IMPOSTERSAMPLE
#define XRA_IMPOSTERSAMPLE

#include "..\Include\ImposterCommon.cginc"


void ImposterSample_half( in half4 vertex, in half2 uv, in half2 grid, in half4 iframe0, in half4 iframe1, in half4 iframe2, in UnityTexture2D _ImposterWorldNormalDepthTex, in UnityTexture2D _ImposterBaseTex, in bool _ImposterFullSphere, in half _ImposterFrames, in half _ImposterBorderClamp,  out half4 baseTex, out half4 worldNormal )//, out half depth )
{
    half2 fracGrid = frac(grid);
    
    half4 weights = TriangleInterpolate( fracGrid );
      
    half2 gridSnap = floor(grid) / _ImposterFrames.xx;
        
    half2 frame0 = gridSnap;
    half2 frame1 = gridSnap + (lerp(half2(0,1),half2(1,0),weights.w)/_ImposterFrames.xx);
    half2 frame2 = gridSnap + (half2(1,1)/_ImposterFrames.xx);
    
    half2 vp0uv = frame0 + iframe0.xy;
    half2 vp1uv = frame1 + iframe1.xy; 
    half2 vp2uv = frame2 + iframe2.xy;
   
    //clamp out neighboring frames TODO maybe discard instead?
    half2 gridSize = 1.0/_ImposterFrames.xx;
    gridSize *= _ImposterBaseTex_TexelSize.zw;
    gridSize *= _ImposterBaseTex_TexelSize.xy;
    float2 border = _ImposterBaseTex_TexelSize.xy*_ImposterBorderClamp;
    
    //vp0uv = clamp(vp0uv,frame0+border,frame0+gridSize-border);
    //vp1uv = clamp(vp1uv,frame1+border,frame1+gridSize-border);
    //vp2uv = clamp(vp2uv,frame2+border,frame2+gridSize-border);
   
    //for parallax modify
    half4 n0 = SAMPLE_TEXTURE2D_LOD( _ImposterWorldNormalDepthTex, sampler_ImposterWorldNormalDepthTex, vp0uv, 0 );
    half4 n1 = SAMPLE_TEXTURE2D_LOD( _ImposterWorldNormalDepthTex, sampler_ImposterWorldNormalDepthTex, vp1uv, 0 );
    half4 n2 = SAMPLE_TEXTURE2D_LOD( _ImposterWorldNormalDepthTex, sampler_ImposterWorldNormalDepthTex, vp2uv, 0 );
    
    //dx dy
    half2 coords = uv.xy * 0.5;
    float2 dx = ddx(coords.xy);  
    float2 dy = ddy(coords.xy);
    
    half n0s = 0.5-n0.a;    
    half n1s = 0.5-n1.a;
    half n2s = 0.5-n2.a;
    
    half2 n0p = iframe0.zw * n0s;
    half2 n1p = iframe1.zw * n1s;
    half2 n2p = iframe2.zw * n2s;
    
    //add parallax shift 
    vp0uv += n0p;
    vp1uv += n1p;
    vp2uv += n2p;
    
/*     //clamp out neighboring frames TODO maybe discard instead?
    vp0uv = clamp(vp0uv,frame0+border,frame0+gridSize-border);
    vp1uv = clamp(vp1uv,frame1+border,frame1+gridSize-border);
    vp2uv = clamp(vp2uv,frame2+border,frame2+gridSize-border); */

    half2 insideRegion0 = step(frame0+border, vp0uv)*(1-step(frame0+gridSize-border, vp0uv));
    half2 insideRegion1 = step(frame1+border, vp1uv)*(1-step(frame1+gridSize-border, vp1uv));
    half2 insideRegion2 = step(frame2+border, vp2uv)*(1-step(frame2+gridSize-border, vp2uv));

    vp0uv = vp0uv*insideRegion0;
    vp1uv = vp1uv*insideRegion1;
    vp2uv = vp2uv*insideRegion2;
    
    worldNormal = ImposterBlendWeights( _ImposterWorldNormalDepthTex,sampler_ImposterWorldNormalDepthTex, uv, vp0uv, vp1uv, vp2uv, weights, dx, dy );
    baseTex = ImposterBlendWeights( _ImposterBaseTex, sampler_ImposterBaseTex,uv, vp0uv, vp1uv, vp2uv, weights, dx, dy );
        
    //pixel depth offset
    //half pdo = 1-baseTex.a;
    //float3 objectScale = float3(length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
    //                        length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
    //                        length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
    //half2 size = _ImposterSize.xx * 2.0;// * objectScale.xx;  
    //half3 viewWorld = mul( UNITY_MATRIX_VP, float4(0,0,1,0) ).xyz;
    //pdo *= size * abs(dot(normalize(imp.viewDirWorld.xyz),viewWorld));
    //depth = pdo;
}

#endif