Shader "Unlit/TestMSDFPrint"
{
	Properties
	{
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
			#include "/Assets/MSDFShaderPrintf/MSDFShaderPrintf.cginc"
			//#include "com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
		
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
				
				int lines = 21;
				int columns = 21;
				float2 inuv = i.uv;
				inuv.y = 1.0 - inuv.y;
				float2 uv = inuv * float2( columns, lines );
				int2 dig = floor( uv );

				float2 gradval = uv;
				float4 grad = float4( ddx(gradval), ddy(gradval) );

				if( uv.x < 10 && uv.y <= 13 )
				{
					const uint sendarr[130] = { 
						'I', 'n', 's', 't', 'a', 'n', 'c', 'e', ' ', ' ',
						'W', 'a', 'l', 'l', 'c', 'l', 'o', 'c', 'k', ' ',
						'N', 'e', 't', 'w', 'o', 'r', 'k', ' ', ' ', ' ',
						'U', 'T', 'C', ' ', 'D', 'a', 'y', 's', ' ', ' ',
						'U', 'T', 'C', ' ', 'S', 'e', 'c', 'o', 'n', 'd',
						'A', 'u', 't', 'o', ' ', 'g', 'a', 'i', 'n', ' ',
						'P', 'e', 'a', 'k', ' ', 'v', 'a', 'l', 'u', 'e',
						'R', 'M', 'S', ' ', 'v', 'a', 'l', 'u', 'e', ' ',
						'F', 'P', 'S', ' ', 'T', '/', 'A', 'L', ' ', ' ',
						'P', 'l', 'a', 'y', 'e', 'r', 'I', 'n', 'f', 'o',
						'D', 'e', 'b', 'u', 'g', ' ', '1', ' ', ' ', ' ',
						'D', 'e', 'b', 'u', 'g', ' ', '2', ' ', ' ', ' ',
						'D', 'e', 'b', 'u', 'g', ' ', '3', ' ', ' ', ' ',
					};

					dig = floor( uv );
					int val = sendarr[dig.x + dig.y * 10];
					col = MSDFPrintChar( val, uv,  grad ).xxxy;
				}
				else
				{
					float value = 0;
					int xoffset = 5;
					bool leadingzero = false;
					
					// If below halfway part we are all one big number soup.
					if( dig.y < 10 )
						dig.x -= 10;
					switch( dig.y )
					{
					case 0:
					case 1:
						// 2: Time since level start in milliseconds.
						// 3: Time of day.
						value = AudioLinkDecodeDataAsSeconds( dig.y?ALPASS_GENERALVU_LOCAL_TIME:ALPASS_GENERALVU_INSTANCE_TIME );
						float seconds = glsl_mod(value, 60);
						int minutes = (value/60) % 60;
						int hours = (value/3600);
						value = hours * 10000 + minutes * 100 + seconds;
						
						if( dig.x < 3 )
						{
							value = hours;
							xoffset = 2;
							leadingzero = 1;
						}
						else if( dig.x < 6 )
						{
							value = minutes;
							xoffset = 5;
							leadingzero = 1;
						}
						else if( dig.x > 5)
						{
							value = seconds;
							xoffset = 8;
							leadingzero = 1;
						}
						break;
					case 2:
						if( dig.x < 8 )
						{
							value = AudioLinkDecodeDataAsUInt( ALPASS_GENERALVU_NETWORK_TIME )/1000;
							xoffset = 7;
						}
						else
						{
							value = AudioLinkDecodeDataAsUInt( ALPASS_GENERALVU_NETWORK_TIME )%1000;
							xoffset = 11;
							leadingzero = 1;
						}
						break;
					case 3:
						value = AudioLinkDecodeDataAsUInt( ALPASS_GENERALVU_UNIX_DAYS );
						xoffset = 11;
						break;
					case 4:
						if( dig.x < 8 )
						{
							value = AudioLinkDecodeDataAsUInt( ALPASS_GENERALVU_UNIX_SECONDS )/1000;
							xoffset = 7;
						}
						else
						{
							value = AudioLinkDecodeDataAsUInt( ALPASS_GENERALVU_UNIX_SECONDS )%1000;
							xoffset = 11;
							leadingzero = 1;
						}
						break;
					case 5:
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2( 11, 0 ) ) ); //Autogain Debug
						break;
					case 6:
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2( 8, 0 ) ) ).y; //Peak
						break;
					case 7:
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2( 8, 0 ) ) ).x; //RMS
						break;

					case 8:
						if( dig.x < 7 )
						{
							value = AudioLinkData( int2( ALPASS_GENERALVU + int2( 0, 0 ) ) ).b; //True FPS
							xoffset = 7;
						}
						else
						{
							value = AudioLinkData( int2( ALPASS_GENERALVU + int2( 1, 0 ) ) ).b; //AudioLink FPS
							xoffset = 11;
						}
						break;

					case 9:
						if( dig.x < 3 )
						{
							value = AudioLinkData( int2( ALPASS_GENERALVU_PLAYERINFO ) ).r;
							xoffset = 3;
						}
						else if( dig.x < 9 )
						{
							value = AudioLinkData( int2( ALPASS_GENERALVU_PLAYERINFO ) ).g;
							xoffset = 9;
						}
						else
						{
							value = AudioLinkData( int2( ALPASS_GENERALVU_PLAYERINFO ) ).b;
							xoffset = 11;
						}
						break;
					case 10:
						//GENERAL DEBUG VALUE 1
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2(7, 0 ) ) ).x;
						break;
					case 11:
						//GENERAL DEBUG VALUE 2
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2(7, 0 ) ) ).y;
						break;
					case 12:
						//GENERAL DEBUG VALUE 3
						value = AudioLinkData( int2( ALPASS_GENERALVU + int2(7, 0 ) ) ).z;
						break;
					default:
						if( dig.x < 5 )
						{
							// CC Note
							value = AudioLinkData( ALPASS_CCINTERNAL + int2( 1 + dig.y - 13, 0 ) ).x;
							xoffset = 3;
						}
						else if( dig.x < 10 )
						{
							//CC Note Number
							value = AudioLinkData( ALPASS_CCINTERNAL + int2( 1 + dig.y - 13, 1 ) ).x;
							xoffset = 11;
						}
						else if( dig.x < 15 )
						{
							//Time Existed
							value = AudioLinkData( ALPASS_CCINTERNAL + int2( 1 + dig.y - 13, 1 ) ).y;
							xoffset = 3;
						}
						else if( dig.x < 20 )
						{
							//Intensity
							xoffset = 8;
							value = AudioLinkData( ALPASS_CCINTERNAL + int2( 1 + dig.y - 13, 0 ) ).a;
						}
						break;
					}
					//value = 2.00000;
					
					// Get fielduv back into the 0..1 over the whole text field.
					float2 fielduv = ( uv + float2(-10., 0.0 ) ) * float2( 1.0/11.0, 1.0 );
					
					col += MSDFPrintNum( value, fielduv, grad, 11.0, 10, leadingzero, -xoffset ).xxxy;
					//col = float4( fielduv, 0.0, 1. );
				}
				
				
		
				float4 amplitudemon = AudioLinkData( ALPASS_WAVEFORM + int2( inuv.x*128, 0 ) );
				float2 uvin = ( inuv.xy*float2(1., 11./3.)-float2( 0., 8./3.) );
				float r = amplitudemon.r + amplitudemon.a;
				float l = amplitudemon.r - amplitudemon.a;
				float comp = uvin.y * 2. - 1.;
				float ramp = saturate( (.05 - abs( r - comp )) * 40. );
				float lamp = saturate( (.05 - abs( l - comp )) * 40. );
				col.xyz += float3( 1., 0.2, 0.2 ) * ramp + float3( .2, .2, 1. ) * lamp * (1.0 - col.a);
				col.a += saturate(ramp + lamp);
						
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
