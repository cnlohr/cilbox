// MIT/x11 License 2024 <>< Charles Lohr

#include <stdio.h>
#include <stdlib.h>

#define STBI_NO_SIMD
#define STB_IMAGE_WRITE_IMPLEMENTATION
#define STB_IMAGE_IMPLEMENTATION

#include "stb_image.h"
#include "stb_image_write.h"

int main()
{
	#ifdef __TINYC__
	int mkdir( const char * path, int mode );
	#endif
	mkdir( ".temp", 0777 );
	int i;
	const int gw = 32;
	const int gh = 64;
	uint8_t * buff = calloc( gw*gh*16*16, 3 );
	
	for( i = 0; i < 256; i++ )
	{
		char cmdline[PATH_MAX];
//		char glyph[4];
//		if( i == '\'' || i == '\\' || i == '\"' )
//			sprintf( glyph, "\\%c", i );
//		else
//			sprintf( glyph, "%c", i );
		if( i == 9 || i == 10 || i == 13 || i == 0 || i == 32 ) continue;
//		printf( "%d (%c)\n", i, i );
		snprintf( cmdline, sizeof(cmdline)-1, "msdfgen.exe -font AudioLinkConsole-Bold.ttf 0x%02x -translate 0.2 3.3 -size %d %d -scale 3.65 -range 1.6 -o .temp/m%03d.png -testrender .temp/g%03d.png %d %d", i, gw, gh, i, i, gw*16, gh*16 );
		printf( "%s\n", cmdline );
		int r = system( cmdline );
		if( r == 0 )
		{
			int w, h, n;
			char fname[PATH_MAX];
			snprintf( fname, sizeof(fname)-1, ".temp/m%03d.png", i );
			uint8_t *data = stbi_load(fname, &w, &h, &n, 0);
			if( data == 0 )
			{
				fprintf( stderr, "Error: Can't load filename for glyph %d\n", i );
				continue;
			}
			if( n != 3 || w != gw || h != gh )
			{
				fprintf( stderr, "error: werid response for %s\n", fname );
				free( data );
				continue;
			}
			int x, y;
			int cx = (i % 16) * gw;
			int cy = (i / 16) * gh;
			
			// Add a 2px black border
			#if 0
			for( x = 0; x < w; x++ )
			{
				memset( &data[(x+(0)*gw)*3], 0, 3  );
				//memset( &data[(x+(1)*gw)*3], 0, 3  );
				memset( &data[(x+(h-1)*gw)*3], 0, 3  );
				//memset( &data[(x+(h-2)*gw)*3], 0, 3  );
			}
			for( y = 0; y < h; y++ )
			{
				memset( &data[(0+(y)*gw)*3], 0, 3  );
				//memset( &data[(1+(y)*gw)*3], 0, 3  );
				memset( &data[(w-1+(y)*gw)*3], 0, 3  );
				//memset( &data[(w-2+(y)*gw)*3], 0, 3  );
			}
			#endif
			// Copy over pixels
			for( y = 0; y < h; y++ )
			{
				for( x = 0; x < w; x++ )
				{
					memcpy( &buff[((cx + x) + (cy+y) * gw*16)*3], &data[(x+y*gw)*3], 3 );
				}
			}
			free( data );
		}
		else
		{
			// Do nothing here.
		}
	}
	int ago = stbi_write_png( "msdfprintf.png", gw*16, gh*16, 3, buff, gw*16*3 );
	if( ago == 0 )
	{
		fprintf( stderr, "Error: Could not write out glyphs. Error %d\n", ago );
		return -9;
	}
	return 0;
}
