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

		public Type GetNativeTypeFromSerializee( Serializee s )
		{
			Dictionary< String, Serializee > ses = s.AsMap();
			String typeName = ses["n"].AsString();
			// Perform security!!



			Serializee g;
			Type [] ga = null;
			if( ses.TryGetValue( "g", out g ) )
			{
				Serializee [] gs = g.AsArray();
				ga = new Type[gs.Length];
				for( int i = 0; i < gs.Length; i++ )
					ga[i] = GetNativeTypeFromSerializee( gs[i] );
				typeName += "`" + gs.Length;
			}


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

			if( ret == null )
			{
				Debug.LogError( $"Could not find type {typeName}" );
				return null;
			}

			if( ga != null )
				ret = ret.MakeGenericType(ga);

			return ret;
		}

		public Type [] TypeNamesToArrayOfNativeTypes( Serializee [] sa )
		{
			Type [] ret = new Type[sa.Length];
			for( int i = 0; i < sa.Length; i++ )
				ret[i] = GetNativeTypeFromSerializee( sa[i] );
			return ret;
		}

		// This does not check any rules, so it can be static.
		static public String GetNativeTypeNameFromSerializee( Serializee s )
		{
			Dictionary< String, Serializee > m = s.AsMap();
			Serializee g;
			if( m.TryGetValue( "g", out g ) )
			{
				String ret = m["n"].AsString() + "`[";
				Serializee [] gs = g.AsArray();
				for( int i = 0; i < gs.Length; i++ )
					ret += (i==0?"":",") + GetNativeTypeNameFromSerializee( gs[i] );
				return ret + "]";
			}
			else
			{
				return m["n"].AsString();
			}
		}

		// This does not check any rules, so it can be static.
		public static Serializee GetSerializeeFromNativeType( Type t )
		{
			Dictionary< String, Serializee > ret = new Dictionary< String, Serializee >();

			if( t.IsGenericType )
			{
				String [] sn = t.FullName.Split( "`" );
				ret["n"] = new Serializee( sn[0] );
				Type [] ta = t.GenericTypeArguments;
				Serializee [] sg = new Serializee[ta.Length];
				for( int i = 0; i < ta.Length; i++ )
					sg[i] = GetSerializeeFromNativeType( ta[i] );
				ret["g"] = new Serializee( sg );
			}
			else
			{
				ret["n"] = new Serializee( t.FullName );
			}
			return new Serializee( ret );
		}

		public MethodBase GetNativeMethodFromTypeAndName( Type declaringType, String name, Type [] parameters, Serializee [] genericArguments, String fullSignature )
		{
			MethodBase m;
			// XXX SECURITY: DECIDE HERE IF A GIVEN METHOD IS OK

			if( typeof(Delegate).IsAssignableFrom(declaringType) )
			{
				int argct = declaringType.GenericTypeArguments.Length;
				Type specific = typeof(CilboxPlatform);
				MethodInfo mi = specific.GetMethod( "ProxyForGeneratingActions" );
				mi = mi.MakeGenericMethod( declaringType );
				return mi;
			}


			// XXX Can we combine Constructor + Method?
			m = declaringType.GetMethod(
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
		// This is called only when creating a new action, not when it's called.
		// T is the delegate, not the arguments of the delegate.
		static public object ProxyForGeneratingActions<T>( CilboxProxy proxy, CilboxMethod method )
		{
			CilboxPlatform.DelegateRepackage rp = new CilboxPlatform.DelegateRepackage();
			rp.meth = method;
			rp.o = proxy;
			MethodInfo mthis = typeof(CilboxPlatform.DelegateRepackage)
				.GetMethod("ActionCallback"+(typeof(T).GenericTypeArguments.Length).ToString());
			if( mthis.IsGenericMethod )
				mthis = mthis.MakeGenericMethod( typeof(T).GenericTypeArguments );
			return Delegate.CreateDelegate( typeof(T), rp, mthis );
		}

		public class DelegateRepackage
		{
			public CilboxMethod meth;
			public CilboxProxy o;
		    public void ActionCallback0( ) { meth.Interpret( o, new object[0] ); }
		    public void ActionCallback1<T0>( T0 o0 ) { meth.Interpret( o, new object[]{o0} ); }
		    public void ActionCallback2<T0,T1>( T0 o0, T1 o1 ) { meth.Interpret( o, new object[]{o0,o1} ); }
		    public void ActionCallback3<T0,T1,T2>( T0 o0, T1 o1, T2 o2 ) { meth.Interpret( o, new object[]{o0,o1,o2} ); }
		    public void ActionCallback4<T0,T1,T2,T3>( T0 o0, T1 o1, T2 o2, T3 o3 ) { meth.Interpret( o, new object[]{o0,o1,o2,o3} ); }
		}
	}
}

