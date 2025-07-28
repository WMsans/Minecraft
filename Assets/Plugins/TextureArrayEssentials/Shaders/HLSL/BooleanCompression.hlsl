#ifndef BOOLEANCOMPRESSION_INCLUDED
#define BOOLEANCOMPRESSION_INCLUDED

int AddCompressedValue(int CompressedValues, bool Value, int Index)
{
    return CompressedValues | (Value ? (int) (1 * pow(2, Index)) : 0);
}

bool GetCompressedValue(int CompressedValues, int Index)
{
    int current = (int) (1 * pow(2, Index));
    return (CompressedValues & current) != 0;
}

void AddCompressedValue_float(int CompressedValues, bool Value, int Index, out int Out)
{
    Out = AddCompressedValue(CompressedValues, Value, Index);
}

void GetCompressedValue_float(int CompressedValues, int Index, out bool Out)
{
    Out = GetCompressedValue(CompressedValues, Index);
}

#endif