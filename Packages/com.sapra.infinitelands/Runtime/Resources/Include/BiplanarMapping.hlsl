
//UNITY_SHADER_NO_UPGRADE
#ifndef BIPLANAR_INCLUDED
#define BIPLANAR_INCLUDED
void Biplanar_float(in float3 p, in float3 n, in float k, out float2 weights, out float2 uv1, out float2 ddx1, out float2 ddy1,out float2 uv2, out float2 ddx2,out float2 ddy2)
{
    // grab coord derivatives for texturing
    float3 dpdx = ddx(p);
    float3 dpdy = ddy(p);
    n = abs(n);

    // determine major axis (in x; yz are following axis)
    int3 ma = (n.x>n.y && n.x>n.z) ? int3(0,1,2) :
               (n.y>n.z)            ? int3(1,2,0) :
                                      int3(2,0,1) ;
    // determine minor axis (in x; yz are following axis)
    int3 mi = (n.x<n.y && n.x<n.z) ? int3(0,1,2) :
               (n.y<n.z)            ? int3(1,2,0) :
                                      int3(2,0,1) ;
    // determine median axis (in x;  yz are following axis)
    int3 me = int3(3,3,3) - mi - ma;
    
    // blend factors
    float2 w = float2(n[ma.x],n[me.x]);
    // make local support
    w = clamp( (w-0.5773)/(1.0-0.5773), 0.0, 1.0 );
    // shape transition
    w = pow( w, float2(k/8.0, k/8.0) );

    weights = w;
    uv1 = float2(p[ma.y],   p[ma.z]);
    uv2 = float2(   p[me.y],   p[me.z]);

    ddx1 = float2(dpdx[ma.y],dpdx[ma.z]);
    ddx2 = float2(dpdx[me.y],dpdx[me.z]);
    ddy1 = float2(dpdy[ma.y],dpdy[ma.z]);
    ddy2 = float2(dpdy[me.y],dpdy[me.z]);


    // blend and return
    //return (x*w.x + y*w.y) / (w.x + w.y);
}
#endif //MYHLSLINCLUDE_INCLUDED
