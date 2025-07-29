#ifndef TEXTURESETTINGS_INCLUDED
#define TEXTURESETTINGS_INCLUDED
struct TextureSettings{
    float Size;
    float NormalStrength;
};
StructuredBuffer<TextureSettings> _texture_settings;
#endif

//UNITY_SHADER_NO_UPGRADE
#ifndef GETSIZESSINGLE_INCLUDED
#define GETSIZESSINGLE_INCLUDED
void GetSizesSingle_float(in int indice, out float Size){
    Size = _texture_settings[indice].Size;
}
void GetSizesSingle_half(in int indice, out half Size){
    Size = _texture_settings[indice].Size;
}
#endif //MYHLSLINCLUDE_INCLUDED

//UNITY_SHADER_NO_UPGRADE
#ifndef GETNORMALSINGLE_INCLUDED
#define GETNORMALSINGLE_INCLUDED
void GetNormalStrength_float(in int indice, out float NormalStrength){
    NormalStrength = _texture_settings[indice].NormalStrength;
}
void GetNormalStrength_half(in int indice, out half NormalStrength){
    NormalStrength = _texture_settings[indice].NormalStrength;
}
#endif //MYHLSLINCLUDE_INCLUDED