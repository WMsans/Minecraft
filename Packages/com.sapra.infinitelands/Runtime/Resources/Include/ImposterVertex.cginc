#ifndef XRA_IMPOSTERVERTEX
#define XRA_IMPOSTERVERTEX

#include "..\Include\ImposterCommon.cginc"
void ImposterVertex_half(in half4 vertex, in half2 texcoord, in bool _ImposterFullSphere, in half3 _ImposterOffset, in half _ImposterFrames, in half2 _ImposterSize,  out half4 ivertex, out half2 iuv, out half2 igrid, out half4 iframe0,out half4 iframe1,out half4 iframe2)
{

    //camera in object space
    half3 objectSpaceCameraPos = mul( unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz,1) ).xyz;
    float4x4 objectToWorld = unity_ObjectToWorld;

    half3 imposterPivotOffset = _ImposterOffset.xyz;
    half framesMinusOne = _ImposterFrames-1;
    
    float3 objectScale = float3(length(float3(objectToWorld[0].x, objectToWorld[1].x, objectToWorld[2].x)),
                                length(float3(objectToWorld[0].y, objectToWorld[1].y, objectToWorld[2].y)),
                                length(float3(objectToWorld[0].z, objectToWorld[1].z, objectToWorld[2].z)));
             
    //pivot to camera ray
    float3 pivotToCameraRay = normalize(objectSpaceCameraPos.xyz-imposterPivotOffset.xyz);

    //scale uv to single frame
    texcoord = half2(texcoord.x,texcoord.y)*(1.0/_ImposterFrames.x);  
    
    //radius * 2 * unity scaling
    half2 size = _ImposterSize.xx * 2.0; // * objectScale.xx; //unity_BillboardSize.xy                 
    
    half3 projected = SpriteProjection( pivotToCameraRay, _ImposterFrames, size, texcoord.xy );

    //this creates the proper offset for vertices to camera facing billboard
    half3 vertexOffset = projected + imposterPivotOffset;
    //subtract from camera pos 
    vertexOffset = normalize(objectSpaceCameraPos-vertexOffset);
    //then add the original projected world
    vertexOffset += projected;
    //remove position of vertex
    vertexOffset -= vertex.xyz;
    //add pivot
    vertexOffset += imposterPivotOffset;

    //camera to projection vector
    half3 rayDirectionLocal = (imposterPivotOffset + projected) - objectSpaceCameraPos;
                 
    //projected position to camera ray
    half3 projInterpolated = normalize( objectSpaceCameraPos - (projected + imposterPivotOffset) ); 
    
    Ray rayLocal;
    rayLocal.Origin = objectSpaceCameraPos-imposterPivotOffset; 
    rayLocal.Direction = rayDirectionLocal; 
    
    half2 grid = VectorToGrid( pivotToCameraRay , _ImposterFullSphere);
    half2 gridRaw = grid;
    grid = saturate((grid+1.0)*0.5); //bias and scale to 0 to 1 
    grid *= framesMinusOne;
    
    half2 gridFrac = frac(grid);
    
    half2 gridFloor = floor(grid);
    
    half4 weights = TriangleInterpolate( gridFrac ); 
    
    //3 nearest frames
    half2 frame0 = gridFloor;
    half2 frame1 = gridFloor + lerp(half2(0,1),half2(1,0),weights.w);
    half2 frame2 = gridFloor + half2(1,1);
    
    //convert frame coordinate to octahedron direction
    half3 frame0ray = FrameXYToRay(frame0, framesMinusOne.xx, _ImposterFullSphere);
    half3 frame1ray = FrameXYToRay(frame1, framesMinusOne.xx, _ImposterFullSphere);
    half3 frame2ray = FrameXYToRay(frame2, framesMinusOne.xx, _ImposterFullSphere);
    
    half3 planeCenter = half3(0,0,0);
    
    half3 plane0x;
    half3 plane0normal = frame0ray;
    half3 plane0z;
    half3 frame0local = FrameTransform( projInterpolated, frame0ray, plane0x, plane0z );
    frame0local.xz = frame0local.xz/_ImposterFrames.xx; //for displacement
    
    //virtual plane UV coordinates
    half2 vUv0 = VirtualPlaneUV( plane0normal, plane0x, plane0z, planeCenter, size, rayLocal );
    vUv0 /= _ImposterFrames.xx;   
    
    half3 plane1x; 
    half3 plane1normal = frame1ray;
    half3 plane1z;
    half3 frame1local = FrameTransform( projInterpolated, frame1ray, plane1x, plane1z);
    frame1local.xz = frame1local.xz/_ImposterFrames.xx; //for displacement
    
    //virtual plane UV coordinates
    half2 vUv1 = VirtualPlaneUV( plane1normal, plane1x, plane1z, planeCenter, size, rayLocal );
    vUv1 /= _ImposterFrames.xx;
    
    half3 plane2x;
    half3 plane2normal = frame2ray;
    half3 plane2z;
    half3 frame2local = FrameTransform( projInterpolated, frame2ray, plane2x, plane2z );
    frame2local.xz = frame2local.xz/_ImposterFrames.xx; //for displacement
    
    //virtual plane UV coordinates
    half2 vUv2 = VirtualPlaneUV( plane2normal, plane2x, plane2z, planeCenter, size, rayLocal );
    vUv2 /= _ImposterFrames.xx;
    
    //add offset here
    ivertex = vertex;
    ivertex.xyz += vertexOffset;

    //overwrite others
    iuv = texcoord;
    igrid = grid;
    iframe0 = half4(vUv0.xy,frame0local.xz);
    iframe1 = half4(vUv1.xy,frame1local.xz);
    iframe2 = half4(vUv2.xy,frame2local.xz);
}

#endif