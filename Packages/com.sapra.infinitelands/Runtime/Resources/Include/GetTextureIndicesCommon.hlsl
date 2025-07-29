#ifndef TEXTURE_INDICES_COMMON
#define TEXTURE_INDICES_COMMON
void insertSortedGlobal(int4 places, inout int4 indices, inout float4 weight, in int newIndice, in float newWeight)
{
    float saturation = saturate(places.z + places.y + places.x);
    float saturation2 = saturate(places.y + places.x);

    indices.w = lerp(lerp(indices.w, newIndice, places.w), indices.z, saturation);
    indices.z = lerp(lerp(indices.z, newIndice, places.z), indices.y, saturation2);
    indices.y = lerp(lerp(indices.y, newIndice, places.y), indices.x, places.x);
    indices.x = lerp(indices.x, newIndice, places.x);

    weight.w = lerp(lerp(weight.w, newWeight, places.w), weight.z, saturation);
    weight.z = lerp(lerp(weight.z, newWeight, places.z), weight.y, saturation2);
    weight.y = lerp(lerp(weight.y, newWeight, places.y), weight.x, places.x);
    weight.x = lerp(weight.x, newWeight, places.x);
}     

void resortByIndex(in int4 indices, in float4 weights, out int4 finalIndices, out float4 finalWeights)
{
    finalIndices = -1;
    finalWeights = 0;

    for(int i = 0; i < 4; i++){
        int4 place = step(finalIndices, indices[i]);
        insertSortedGlobal(place, finalIndices, finalWeights, indices[i], weights[i]);
    }
}  

#ifdef SimpleSample
void SampleIndices(in float2 correctUV, UnityTexture2DArray textureMasks, UnitySamplerState sampler_splatMap, out int4 highestIndices, out float4 highestWeights){  
    int4 indices = 0;
    float4 weights = 0.0001f;
    
    int length = 0;
    int width = 0;
    int height = 0;
    textureMasks.tex.GetDimensions(width,height,length);
    [loop]
    for(int i = 0; i < length; i++)
    {    
        half4 fourTextures = saturate(textureMasks.Sample(sampler_splatMap, float3(correctUV, i)));       
        for(int b = 0; b < 4; b++){
            int index = i*4+b;
            float weight = fourTextures[b];


            if (weight > weights.w) {
                int4 places = step(weights, weight);
                insertSortedGlobal(places, indices, weights, index, weight);
            }
        }
    } 
    //highestIndices = indices;
    //highestWeights = weights;
    resortByIndex(indices, weights, highestIndices, highestWeights);
}
#endif

#ifdef LevelSample
void SampleIndicesLevel(in float2 correctUV, Texture2DArray textureMasks, SamplerState sampler_splatMap, out int4 highestIndices, out float4 highestWeights){  
    highestIndices = 0;
    highestWeights = 0.0001f;
    int length = 0;
    int width = 0;
    int height = 0;
    textureMasks.GetDimensions(width,height,length);
    
    [loop]
    for(int i = 0; i < length; i++)
    {    
        half4 fourTextures = saturate(textureMasks.SampleLevel(sampler_splatMap, float3(correctUV, i),0));       
        for(int b = 0; b < 4; b++){
            int index = i*4+b;
            float weight = fourTextures[b];

            if (weight > highestWeights.w) {
                int4 places = step(highestWeights, weight);
                insertSortedGlobal(places, highestIndices, highestWeights, index, weight);
            }
        }
    } 
}
#endif
#endif