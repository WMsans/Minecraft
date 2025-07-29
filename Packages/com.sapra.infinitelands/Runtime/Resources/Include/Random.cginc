//Inigo quilez https://www.shadertoy.com/view/XlXcW4
float3 randValue(uint3 x) // iq version
{
    const uint k = 1103515245U; // GLIB C
    x = ((x >> 8U) ^ x.yzx) * k;
    x = ((x >> 8U) ^ x.yzx) * k;
    x = ((x >> 8U) ^ x.yzx) * k;

    return float3(x) / float(0xffffffffU);
}

float3 randValueIndex(uint index) // iq version
{
    index += 1;
    return randValue(uint3(index, index * 30, index * 10));
}
