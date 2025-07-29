#ifndef XRA_IMPOSTERCOMMON_CGINC
#define XRA_IMPOSTERCOMMON_CGINC

struct ImposterData
{
    half2 uv;
    half2 grid;
    half4 frame0;
    half4 frame1;
    half4 frame2;
    half4 vertex;
};

struct Ray
{
    half3 Origin;
    half3 Direction;
};

half3 NormalizePerPixelNormal (half3 n)
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return n;
    #else
        return normalize(n);
    #endif
}

half4 ImposterBlendWeights(UnityTexture2D tex, SamplerState samp, half2 uv, half2 frame0, half2 frame1, half2 frame2, half4 weights, half2 dx, half2 dy )
{    
    half4 samp0 = tex.SampleGrad( samp, frame0, dx.xy, dy.xy );
    half4 samp1 = tex.SampleGrad( samp, frame1, dx.xy, dy.xy );
    half4 samp2 = tex.SampleGrad( samp, frame2, dx.xy, dy.xy );

    half4 result = samp0*weights.x + samp1*weights.y + samp2*weights.z;
    
    return result;
}

float Isolate( float c, float w, float x )
{
    return smoothstep(c-w,c,x)-smoothstep(c,c+w,x);
}

float SphereMask( float2 p1, float2 p2, float r, float h )
{
    float d = distance(p1,p2);
    return 1-smoothstep(d,r,h);
}

//for hemisphere
half3 OctaHemiEnc( half2 coord )
{
 	coord = half2( coord.x + coord.y, coord.x - coord.y ) * 0.5;
 	half3 vec = half3( coord.x, 1.0 - dot( half2(1.0,1.0), abs(coord.xy) ), coord.y  );
 	return vec;
}
//for sphere
half3 OctaSphereEnc( half2 coord )
{
    half3 vec = half3( coord.x, 1-dot(1,abs(coord)), coord.y );
    if ( vec.y < 0 )
    {
        half2 flip = vec.xz >= 0 ? half2(1,1) : half2(-1,-1);
        vec.xz = (1-abs(vec.zx)) * flip;
    }
    return vec;
}

half3 GridToVector( half2 coord , bool _ImposterFullSphere)
{
    half3 vec;
    if ( _ImposterFullSphere )
    {
        vec = OctaSphereEnc(coord);
    }
    else
    {
        vec = OctaHemiEnc(coord);
    }
    return vec;
}

//for hemisphere
half2 VecToHemiOct( half3 vec )
{
	vec.xz /= dot( 1.0, abs(vec) );
	return half2( vec.x + vec.z, vec.x - vec.z );
}

half2 VecToSphereOct( half3 vec )
{
    vec.xz /= dot( 1,  abs(vec) );
    if ( vec.y <= 0 )
    {
        half2 flip = vec.xz >= 0 ? half2(1,1) : half2(-1,-1);
        vec.xz = (1-abs(vec.zx)) * flip;
    }
    return vec.xz;
}
	
half2 VectorToGrid( half3 vec , in bool _ImposterFullSphere)
{
    half2 coord;

    if ( _ImposterFullSphere )
    {
        coord = VecToSphereOct( vec );
    }
    else
    {
        vec.y = max(0.001,vec.y);
        vec = normalize(vec);
        coord = VecToHemiOct( vec );
    }
    return coord;
}

half4 TriangleInterpolate( half2 uv )
{
    uv = frac(uv);

    half2 omuv = half2(1.0,1.0) - uv.xy;
    
    half4 res = half4(0,0,0,0);
    //frame 0
    res.x = min(omuv.x,omuv.y); 
    //frame 1
    res.y = abs( dot( uv, half2(1.0,-1.0) ) );
    //frame 2
    res.z = min(uv.x,uv.y); 
    //mask
    res.w = saturate(ceil(uv.x-uv.y));
    
    return res;
}

//frame and framecout, returns 
half3 FrameXYToRay( half2 frame, half2 frameCountMinusOne, bool _ImposterFullSphere )
{
    //divide frame x y by framecount minus one to get 0-1
    half2 f = frame.xy / frameCountMinusOne;
    //bias and scale to -1 to 1
    f = (f-0.5)*2.0; 
    //convert to vector, either full sphere or hemi sphere
    half3 vec = GridToVector( f , _ImposterFullSphere);
    vec = normalize(vec);
    return vec;
}

half3 ITBasis( half3 vec, half3 basedX, half3 basedY, half3 basedZ )
{
    return half3( dot(basedX,vec), dot(basedY,vec), dot(basedZ,vec) );
}
 
half3 FrameTransform( half3 projRay, half3 frameRay, out half3 worldX, out half3 worldZ  )
{
    //TODO something might be wrong here
    worldX = normalize( half3(-frameRay.z, 0, frameRay.x) );
    worldZ = normalize( cross(worldX, frameRay ) ); 
    
    projRay *= -1.0; 
    
    half3 local = normalize( ITBasis( projRay, worldX, frameRay, worldZ ) );
    return local;
}

half2 VirtualPlaneUV( half3 planeNormal, half3 planeX, half3 planeZ, half3 center, half2 uvScale, Ray rayLocal )
{
    half normalDotOrigin = dot(planeNormal,rayLocal.Origin);
    half normalDotCenter = dot(planeNormal,center);
    half normalDotRay = dot(planeNormal,rayLocal.Direction);
    
    half planeDistance = normalDotOrigin-normalDotCenter;
    planeDistance *= -1.0;
    
    half intersect = planeDistance / normalDotRay;
    
    half3 intersection = ((rayLocal.Direction * intersect) + rayLocal.Origin) - center;
    
    half dx = dot(planeX,intersection);
    half dz = dot(planeZ,intersection);
    
    half2 uv = half2(0,0);
    
    if ( intersect > 0 )
    {
        uv = half2(dx,dz);
    }
    else
    {
        uv = half2(0,0);
    }
    
    uv /= uvScale;
    uv += half2(0.5,0.5);
    return uv;
}


half3 SpriteProjection( half3 pivotToCameraRayLocal, half frames, half2 size, half2 coord )
{
    half3 gridVec = pivotToCameraRayLocal;
    
    //octahedron vector, pivot to camera
    half3 y = normalize(gridVec);
    
    half3 x = normalize( cross( y, half3(0.0, 1.0, 0.0) ) );
    half3 z = normalize( cross( x, y ) );

    half2 uv = ((coord*frames)-0.5) * 2.0; //-1 to 1 

    half3 newX = x * uv.x;
    half3 newZ = z * uv.y;
    
    half2 halfSize = size*0.5;
    
    newX *= halfSize.x;
    newZ *= halfSize.y;
    
    half3 res = newX + newZ;  
     
    return res;
}


#endif //XRA_IMPOSTERCOMMON_CGINC