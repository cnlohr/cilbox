#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Cilbox
{
	[InitializeOnLoad]
	internal static class CilboxClickableFaults
	{
		static CilboxClickableFaults()
		{
			CilboxFault.InterpretedReporter = Report;
		}

		static void Report( CilboxUnhandledInterpretedException uhe )
		{
			List<CilboxSourceLog.WorldFrame> frames = new List<CilboxSourceLog.WorldFrame>();
			if( uhe.Frames != null )
			{
				foreach( string f in uhe.Frames )
				{
					int a = f.IndexOf( '|' );
					int b = f.LastIndexOf( '|' );
					if( a <= 0 || b <= a ) continue;
					if( !int.TryParse( f.Substring( b + 1 ), out int pc ) ) continue;
					frames.Add( new CilboxSourceLog.WorldFrame( f.Substring( 0, a ), f.Substring( a + 1, b - a - 1 ), pc ) );
				}
			}

			string message = uhe.Throwee is Exception thr ? thr.GetType().FullName + ": " + thr.Message : uhe.Message;
			CilboxSourceLog.LogSourceExceptionLines( message, CilboxSourceLog.ResolveFrameLines( frames ), true );
		}
	}
}
#endif
