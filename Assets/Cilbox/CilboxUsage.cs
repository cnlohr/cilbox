using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

#if UNITY_EDITOR
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.IO;
#endif

namespace Cilbox
{
	public class CilboxUsage
	{
		private Cilbox box;

		public CilboxUsage( Cilbox b ) { box = b; }

		public Type GetNativeTypeFromName( String typeName )
		{
//Debug.Log( $"TN INPUT: {typeName}" );
//			typeName = typeName.Replace("CilboxPlatform", "CilboxPlatformActual");
//Debug.Log( $"TN OUTPUT: {typeName}" );

			// XXX SECURITY: DECIDE HERE IF THIS TYPE IS OKAY FOR THE CLIENT TO HAVE.
			if (typeName.Equals(typeof(System.Runtime.CompilerServices.RuntimeHelpers).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() class name.
				// This probably should move somewhere else if we add sandboxing.
				typeName = typeof(CilboxPublicUtils).FullName;
			}
			if (typeName.Equals(typeof(System.RuntimeFieldHandle).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() second argument.
				// This probably should move somewhere else if we add sandboxing.
				typeName = typeof(byte[]).FullName;
			}
			Type ret = Type.GetType( typeName );
			if( ret == null )
			{
				System.Reflection.Assembly [] assys = AppDomain.CurrentDomain.GetAssemblies();
				foreach( System.Reflection.Assembly a in assys )
				{
					ret = a.GetType( typeName );
					if( ret != null ) break;
				}
			}

#if false
			bool isGeneric = false;
			int numGenerics = 0;
Debug.Log( "INPUT TYPENAME: " + typeName );
			String [] parts = typeName.Split("`");

			String [] preGenericsArray = null;

			if( parts.Length > 1 )
			{
				preGenericsArray = parts[1].Split( "[" );
				if( Int32.TryParse( preGenericsArray[0], out numGenerics ) )
				{
					Debug.Log( "GENERIC" + parts[0] );
					isGeneric = true;
					typeName = parts[0] + "`" + numGenerics;
				}
				else
				{
					Debug.LogError( $"Could not parse type {typeName}" );
					return null;
				}
			}
Debug.Log( "TYPEPRE: " + typeName + " " + parts.Length );
			Type ret = Type.GetType( typeName );
			if( ret == null )
			{
				System.Reflection.Assembly [] assys = AppDomain.CurrentDomain.GetAssemblies();
				foreach( System.Reflection.Assembly a in assys )
				{
					Debug.Log( "TYPENAME: " + typeName );
					ret = a.GetType( typeName );
					if( ret != null ) break;
				}
			}
Debug.Log( "Is Generic: " + isGeneric );
			if( isGeneric )
			{
				int i;
Debug.Log( "REXXXXX" + preGenericsArray[1] );
				String[] args = preGenericsArray[1].Split( "]" )[0].Split(",");
				Type[] types = new Type[args.Length];
				Debug.Log( "ARRRR: " + preGenericsArray[1].Split( "]" )[0] );
				for( i = 0; i < args.Length; i++ )
				{
					Debug.Log( "Extracting From:" + args[i] );
					types[i] = GetNativeTypeFromName( args[i] );
				}
				ret = ret.MakeGenericType( types );
			}
#endif
Debug.Log( "GOT OUT: " + ret + " from " + typeName );
			return ret;
		}

		public Type[] TypeNamesToArrayOfNativeTypes( String [] parameterNames )
		{
			// XXX SECURITY: DECIDE HERE IF A GIVEN NATIVE TYPE GROUP

			if( parameterNames == null ) return null;
			Type[] ret = new Type[parameterNames.Length];
			for( int i = 0; i < parameterNames.Length; i++ )
			{
				Type pt = ret[i] = GetNativeTypeFromName(  parameterNames[i]  );
					//GetNativeTypeFromName( assemblyAndTypeName[0], assemblyAndTypeName[1] );
			}
			return ret;
		}

		public MethodBase GetNativeMethodFromTypeAndName( Type declaringType, String name, Type [] parameters, String [] genericArguments, String fullSignature )
		{
			// XXX SECURITY: DECIDE HERE IF A GIVEN METHOD IS OK

//Debug.Log( $"MT INPUT: {declaringType}" );
//			fullSignature = fullSignature.Replace("CilboxPlatform", "CilboxPlatformActual");
//Debug.Log( $"MT OUTPUT: {declaringType}" );



			// XXX Can we combine Constructor + Method?
			MethodBase m = declaringType.GetMethod(
				name,
				genericArguments.Length,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
				null,
				CallingConventions.Any,
				parameters,
				null ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."
/* Can't seem to do this??
			if( m == null )
			{
				m = declaringType.GetConstructor(
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
					null,
					CallingConventions.Any,
					parameters,
					modifiers ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."
			}
*/
			if( m == null )
			{
				// Can't use GetConstructor, because somethings have .ctor or .cctor
				ConstructorInfo[] cts = declaringType.GetConstructors(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
				int ck;
				for( ck = 0; ck < cts.Length; ck++ )
				{
					//Debug.Log( cts[ck] );
					if( fullSignature == cts[ck].ToString() )
					{
						m = cts[ck];
						break;
					}
				}
			}


			if( m != null && m is MethodInfo && genericArguments.Length > 0 )
			{
		    	m = ((MethodInfo)m).MakeGenericMethod( TypeNamesToArrayOfNativeTypes( genericArguments ) );
			}

			return m;
		}
	}


	public class CilboxPlatform
	{

		public class DelegateRepackage
		{
			public CilboxMethod meth;
			public CilboxProxy o;
		    public void ActionCallback( object[] parameters )
			{
				meth.Interpret( o, parameters );
			}
		}
	}
#if false
	public class CilboxPlatform
	{
		static public UnityEngine.Events.UnityAction GenerateButtonClickedEvent( UnityEngine.Events.UnityAction o )
		{
			return (UnityEngine.Events.UnityAction)o;
		}
	}

	public class CilboxPlatformActual
	{
		static public UnityEngine.Events.UnityAction GenerateButtonClickedEvent( DelegateRepackage o )
		{
			DelegateRepackage octx = o;
			return octx.ActionCallback;
		}

		public class DelegateRepackage
		{
			public CilboxMethod meth;
			public CilboxProxy o;
		    public void ActionCallback()
			{
				Debug.Log( "ActionCallback" );
				meth.Interpret( o, new object[0] );
			}
		}
	}
#endif

}

