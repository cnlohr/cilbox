// Temporary defines for testing only
#define __0             48
#define __1             49
#define __2             50
#define __3             51
#define __4             52
#define __5             53
#define __6             54
#define __7             55
#define __8             56
#define __9             57
#define __SPACE         32
#define __EXCLAMATION   33
#define __QUOTE         34
#define __HASH          35
#define __DOLLAR        36
#define __AMP           38
#define __APOSTROPHE    39
#define __PAREN_OPEN    40
#define __PAREN_CLOSED  41
#define __MULT          42
#define __PLUS          43
#define __COMMA         44
#define __DASH          45
#define __PERIOD        46
#define __FWD_SLASH     47
#define __COLON         58
#define __SEMICOLON     59
#define __LESSTHAN      60
#define __EQUAL         61
#define __GREATERTHAN   62
#define __QUESTION      63
#define __CARROT        94
#define __A             65
#define __B             66
#define __C             67
#define __D             68
#define __E             69
#define __F             70
#define __G             71
#define __H             72
#define __I             73
#define __J             74
#define __K             75
#define __L             76
#define __M             77
#define __N             78
#define __O             79
#define __P             80
#define __Q             81
#define __R             82
#define __S             83
#define __T             84
#define __U             85
#define __V             86
#define __W             87
#define __X             88
#define __Y             89
#define __Z             90
#define __a             97
#define __b             98
#define __c             99
#define __d             100
#define __e             101
#define __f             102
#define __g             103
#define __h             104
#define __i             105
#define __j             106
#define __k             107
#define __l             108
#define __m             109
#define __n             110
#define __o             111
#define __p             112
#define __q             113
#define __r             114
#define __s             115
#define __t             116
#define __u             117
#define __v             118
#define __w             119
#define __x             120
#define __y             121
#define __z             122
#define __UNDERSCORE    95


UNITY_DECLARE_TEX2DARRAY( _UdonMSDFPrintf );
uniform float4 _UdonMSDFPrintf_TexelSize;

float2 MSDFCalcScreenPxRange( float4 suv )
{
	float2 screenTexSize = (1.0)/length(suv);
	const float2 pxRange = 8.0;  // Was 4.
	float2 unitRange = (pxRange)/float2(_UdonMSDFPrintf_TexelSize.zw);
	float2 screenPxRangeRaw = 0.5*dot(unitRange, screenTexSize);
	return max(screenPxRangeRaw, 1.0);
}

float2 MSDFEval( float2 texCoord, int index, float2 screenPxRange, float4 grad )
{
	texCoord.y = 1.0 - texCoord.y;
	float3 msd = _UdonMSDFPrintf.SampleGrad(sampler_UdonMSDFPrintf, float3( texCoord, index ), grad.xy, grad.zw );

	float sd = max(min(msd.r, msd.g), min(max(msd.r, msd.g), msd.b)); // sd = median
	float sdShadow = sd + (sd*4.0-0.3) * ( ( 14 * pow(length(grad),0.5)) );
	float2 screenPxDistance = screenPxRange*( float2( sd, sdShadow ) - 0.5)*float2( 1.0, 0.3 );
	float2 opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
	return opacity;
}

float2 MSDFPrintChar( int charNum, float2 charUv, float4 smoothUv )
{
	float2 screenPxRange = MSDFCalcScreenPxRange( smoothUv );
	charUv = frac(charUv);
	return MSDFEval(charUv, charNum, screenPxRange, smoothUv);
}

// Print a number on a line
//
// value            (float) Number value to display
// charUV           (float2) coordinates on the character to render
// softness
// numDigits        (uint) Digit in number to render
// digitOffset      (uint) Shift digits to the right
// numFractDigits   (uint) Number of digits to round to after the decimal
//
// TODO: Please, someone figure out how to improve the rounding here.
float2 MSDFPrintNum( float value, float2 texCoord, float4 grad, int numDigits = 10, int numFractDigits = 4, bool leadZero = false, int offset = 0 )
{
	int digitOffset = numDigits - numFractDigits - 1;
	int digit = floor( frac( texCoord ) * numDigits ) + offset;
	float2 charUv = frac( texCoord * float2( numDigits, 1.0 ) );
	
    uint charNum;
    uint leadingdash = (value<0)?('-'-'0'):(' '-'0');
    value = abs(value);

	if ( ( asuint(value) & 0x7fffffff) > 0x7f800000 )
	{
		charNum = __N;
	}
	else if (digit == digitOffset)
    {
        charNum = __PERIOD;
    }
    else
    {
        value += 0.4999999 * pow( 0.1, numFractDigits );
        int dmfd = (int)digit - (int)digitOffset;
        if (dmfd > 0)
        {
            //fractional part.
            uint fpart = round(frac(value) * pow(10, numFractDigits));
            uint l10 = pow(10.0, numFractDigits - dmfd);
            charNum = ((uint)(fpart / l10)) % 10;
        }
        else
        {
            float l10 = pow(10.0, (float)(dmfd + 1));
            float vnum = value * l10;
            charNum = (uint)(vnum);

            //Disable leading 0's?
            //if (!leadZero && dmfd != -1 && charNum == 0 && dmfd < 0.5)
            //    charNum = ' '-'0'; // space

            if( dmfd < -1 && charNum == 0 )
            {
                
                if( leadZero )
                    charNum %= (uint)10;
                else
                    charNum = leadingdash;
            }
            else
                charNum %= (uint)10;
        }
        charNum += '0';
    }

	return MSDFPrintChar( charNum, charUv, grad );
}

float2 MSDFPrintHex( uint value, float2 texCoord, float4 grad, int numDigitFields = 10, int numDigitShow = 8, int offset = 0 )
{
	float2 charUv = frac( texCoord * float2( numDigitFields, 1.0 ) );
	int digit =  -1-offset+numDigitFields-floor( frac( texCoord ) * numDigitFields );
	uint tv;
	if( digit < 0 || digit >= numDigitShow ) 
	{
		tv = __SPACE;
	}
	else
	{
		tv = (value>>(digit*4)) & 0xf;
		if( tv > 9 )
			tv = tv + __a - 10;
		else
			tv = tv + __0;
	}
	return MSDFPrintChar( tv, charUv, grad );
}

