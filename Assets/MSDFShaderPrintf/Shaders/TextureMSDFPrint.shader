Shader "Unlit/TextureMSDFPrint"
{
	Properties
	{
		_MSDFTex ("MSDF Texture", 2DArray) = "white" {}
		_CheckTexture ("Check Texture", 2D) = "white" {}
		[ToggleUI] _CombDisplay( "Combined Display", float ) = 0.0
		[ToggleUI] _HexDisplay( "Hex Display", float ) = 0.0
		_SuperLines ("Vertical Pixels", float) = 4.0
		_SuperColumns ("Horizontal Pixels", float) = 8.0
		_OffsetX ("Offset X", float) = 0
		_OffsetY ("Offset Y", float) = 0
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}

		Pass
		{
			//ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "../MSDFShaderPrintf.cginc"
			//#include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
		
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			Texture2D<float4> _CheckTexture;	
			uniform float4 _CheckTexture_TexelSize;
			float _HexDisplay, _CombDisplay;
			float _SuperLines;
			float _SuperColumns;
			float _OffsetX, _OffsetY;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 col = 0.0;
				
				int lines = _SuperLines * 4;
				int columns = 10;
				int supercolumns = _SuperColumns;
				float2 inuv = i.uv;
				inuv.y = 1.0 - inuv.y;
				float2 uv = inuv * float2( supercolumns * columns, lines );
				int2 dig = floor( uv );

				col.a += 0.2;

				
				float2 fielduv = inuv;
				fielduv *= float2( supercolumns, lines );
				
				uint2 dpycoord = floor( fielduv );
				uint2 tc = dpycoord / uint2( 1.0, 4.0 );
				
				uint3 datacoord = uint3( tc.x + uint(_OffsetX), _CheckTexture_TexelSize.w - tc.y - 1 - uint(_OffsetY), 0 );

				float4 v4 = _CheckTexture.Load( datacoord );
				float value = 0.0;
				switch( dpycoord.y&3 )
				{
				case 0: value = v4.x; break;
				case 1: value = v4.y; break;
				case 2: value = v4.z; break;
				case 3: value = v4.w; break;
				}
				float2 nfuv2;

				// Two rows.
				nfuv2 = fielduv * float2( 1.0, 2.0 );
				
				float2 gradval = nfuv2 * float2( 14.0, 1.0 );
				float4 grad = float4( ddx(gradval), ddy(gradval) );
				
				if( _CombDisplay > 0.5 )
				{
					if( frac( fielduv.y ) > 1.0/2.0 )
					{
						uint v = asuint(value);
						if( frac( fielduv.x ) > 6.0/14.0 )
						{
							col = MSDFPrintHex( v, nfuv2, grad, 14, 8, 0 ).xxxy;
						}
						else if( frac( fielduv.x ) < 5 / 14.0 && frac( fielduv.x ) > 1.0 / 14.0 )
						{
							int thiscell = frac( nfuv2.x  ) * 14.0 - 1.0;
							v = (v >> (uint(thiscell)*8)) & 0xff;
							float2 thisuv2 = float2( 14.0, 1.0 ) * nfuv2;
							col = MSDFPrintChar( v, thisuv2, grad ).xxxy;
						}
						else
						{
							float2 thisuv2 = float2( 14.0, 1.0 ) * nfuv2;
							col = MSDFPrintChar( 32, thisuv2, grad ).xxxy;
						}
					}
					else
					{
						col = MSDFPrintNum( value, nfuv2, grad, 14, 6, false, 0 ).xxxy;
					}
					switch (dpycoord.y & 3) { case 0: col.y = 0; col.z = 0; break; case 1: col.x = 0; col.z = 0; break; case 2: col.x = 0; col.y = 0; break; };
					//col.y = nfuv2.y/6.0-1.0;
					//col.a = 1.0;
				}
				else
				{
					if( _HexDisplay > 0.5 )
					{
						col = MSDFPrintHex( asuint(value), nfuv2, grad, 11, 3 ).xxxy;
					}
					else
					{
						col = MSDFPrintNum( value, nfuv2, grad, 11, 5, false, 0 ).xxxy;
					}
					switch (dpycoord.y & 3) { case 0: col.y = 0; col.z = 0; break; case 1: col.x = 0; col.z = 0; break; case 2: col.x = 0; col.y = 0; break; };
				}

//				col.a = lerp( 0, col.a, saturate(2-4 * saturate( length( fwidth( nfuv2 ) ) ) ) );
//				col.a = 1.0;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			
			
			
			
/*
// For splitting into thirds.
					float2 nfuv2 = fielduv * float2( 1.0, 6.0 );
					if( frac( fielduv.y ) > 4.0/6.0 )
					{
						if( frac( fielduv.x ) > 0.5 )
						{
							col += MSDFPrintHex( asuint(value), nfuv2 * float2( 1.0, 1.0/2.0 ) - float2( 1.50/11.0, 0.0 ), 28, 8, 6 ).xxxy;
							col += 0.1;
						}
						else
						{
							
							col = MSDFPrintChar( tv, charUv, smoothUv );
						}
					}
					else
					{
						int oddline = int( fielduv.y ) & 1;
						col += MSDFPrintNum( value, nfuv2 * float2( 1.0, 1.5/6.0 ) - oddline * float2( 0.0, 3.0/6.0), 14, 6, false, 0 ).xxxy;
					}
					switch (dpycoord.y & 3) { case 0: col.y = 0; col.z = 0; break; case 1: col.x = 0; col.z = 0; break; case 2: col.x = 0; col.y = 0; break; };
					//col.y = nfuv2.y/6.0-1.0;
					//col.a = 1.0;
					*/
			ENDCG
		}
	}
}

