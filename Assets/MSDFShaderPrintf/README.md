# MSDFShaderPrintf

Using https://github.com/Chlumsky/msdfgen

To build font table, from .source, run genall.exe

To build genall.exe, install tinycc, then run: `tcc .source/genall.c` then run `./genall.exe`

I.e. if you want to change properties of the MSDF Fonts.

In here is also `MSDFShaderPrintf.cginc`, which provides:


```c
// For directly evaluating a given glyph index based on uv, smooth uv, will return central hit and shadow opacities.
float2 MSDFEval( float2 texCoord, int index, float2 screenPxRange, float2 suv )

// For printing a char with an outline.
float2 MSDFPrintChar( int charNum, float2 charUv, float2 smoothUv )

// For printing a number
float2 MSDFPrintNum( float value, float2 texCoord, int numDigits = 10, int numFractDigits = 4, bool leadZero = false, int offset = 0 )

```