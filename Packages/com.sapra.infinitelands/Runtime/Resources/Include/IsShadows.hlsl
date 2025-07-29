#ifndef ISSHADOWS_INCLUDED
#define ISSHADOWS_INCLUDED
int shadow_mode;
void IsShadows_half(out bool IsShadow){  
    IsShadow = shadow_mode == 1;
}
#endif //MYHLSLINCLUDE_INCLUDED
