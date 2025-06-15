//
// CilboxUsage.cs - this file contains:
//  * Security
//  * Any types/methods that get swapped out at load time.
//
// Security Checking:
//   For Methods:
//     1. HandleEarlyMethodRewrite() -- This can rewrite declaring type + method.
//     2. GetNativeTypeNameFromSerializee on declaring type
//     3. GetNativeMethodFromTypeAndName - Sometimes swap-out stuff
//        a. If rewritten, overrides all further security and fast-paths.
//     4. InternalGetNativeMethodFromTypeAndNameNoSecurity
//     5. Parameters and Arguments are checked with CheckTypeSecurityRecursive
//     6. CheckTypeSecurityRecursive calls CheckTypeSecurity
//     7. CheckTypeSecurity checks with the specific Cilbox if a method is
//        allowed via CheckMethodAllowed.
//     8. If disallowed, abort.  If allowed, and overridden, bypass any further checks.
//
//
//   GetNativeTypeFromSerializee (can only be used on non-templated types)
//     1. Checks to see if it's a type from within this cilbox.  If so GO!
//     2. CheckReplaceTypeNotRecursive ON BASE TYPE ONLY
//     3. Recursively check all template types through GetNativeTypeFromSerializee
//     4. Type.GetType
//     5. MakeGenericType
//
//   GetNativeTypeNameFromSerializee mimics GetNativeTypeFromSerializee
//
//   CheckTypeSecurity calls CheckTypeAllowed on the specific cilbox.
//    If it is allowed, continue normal checks.  If it is disallowed, abort.
//
// TODO: Check UnityAction / UnityEvent
//

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Cilbox
{
	public class CilboxUsage
	{
		private Cilbox box;
		public CilboxUsage( Cilbox b ) { box = b; }

		// This is after the type has been fully de-arrayed and de-templated.
		String CheckTypeSecurity( String sType )
		{
			if( box.CheckTypeAllowed( sType ) == true ) return sType;

			Debug.LogError( $"TYPE FAILED CHECK: {sType}" );
			return null;
		}

		public MethodBase GetNativeMethodFromTypeAndName( Type declaringType, String name, Serializee [] parametersIn, Serializee [] genericArgumentsIn, String fullSignature )
		{
			MethodInfo mi = null;
			bool bDisallowed = box.CheckMethodAllowed( out mi, declaringType, name, parametersIn, genericArgumentsIn, fullSignature );
			if( !bDisallowed ) goto disallowed;
			if( mi != null ) return mi;

			// Replace any delegate creations with their proxies.
			if( typeof(Delegate).IsAssignableFrom(declaringType) )
			{
				int argct = declaringType.GenericTypeArguments.Length;
				Type specific = typeof(CilboxPlatform);
				mi = specific.GetMethod( "ProxyForGeneratingActions" );
				mi = mi.MakeGenericMethod( declaringType );
				return mi;
			}

			Type[] parameters = TypeNamesToArrayOfNativeTypes( parametersIn );
			Type[] genericArguments = TypeNamesToArrayOfNativeTypes( genericArgumentsIn );

			MethodBase m = InternalGetNativeMethodFromTypeAndNameNoSecurity( declaringType, name, parameters, genericArguments, fullSignature );

			// Check all parameters for type safety.
			foreach( Type t in parameters )
				if( CheckTypeSecurityRecursive( t ) == null ) goto disallowed;
			foreach( Type t in genericArguments )
				if( CheckTypeSecurityRecursive( t ) == null ) goto disallowed;
			if( m is MethodInfo && CheckTypeSecurityRecursive( ((MethodInfo)m).ReturnType ) == null ) goto disallowed;

			return m;
		disallowed:
			Debug.LogError( $"Privelege failed {declaringType}.{name}" );
			return null;
		}



		////////////////////////////////////////////////////////////////////////////////////
		// REWRITERS ///////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////

		public (String, Serializee) HandleEarlyMethodRewrite( String name, Serializee declaringType )
		{
			Dictionary< String, Serializee > ses = declaringType.AsMap();
			String typeName = ses["n"].AsString();
			if( typeName == "<PrivateImplementationDetails>" && name == "ComputeStringHash" )
			{
				Dictionary< String, Serializee > exportType = new Dictionary< String, Serializee >();
				exportType["n"] = new Serializee( "Cilbox.CilboxPublicUtils" );
				return ( "ComputeStringHashProxy", new Serializee( exportType ) );
			}
			return ( name, declaringType );
		}

		// WARNING: This DOES NOT appropriately handle templated types.
		// TODO: IF YOU WANT THIS TO HANDLE TEMPLATE TYPES, YOU MUST DO SO RECURSIVELY.
		String CheckReplaceTypeNotRecursive( String typeName )
		{
			if (typeName.Equals(typeof(System.Runtime.CompilerServices.RuntimeHelpers).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() class name.
				typeName = typeof(CilboxPublicUtils).FullName;
			}
			if (typeName.Equals(typeof(System.RuntimeFieldHandle).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() second argument.
				typeName = typeof(byte[]).FullName;
			}

			// Perform check without array[]
			//  i.e.  System.byte[][] ===> System.byte  /  [][]
			String [] vTypeNameNoArray = typeName.Split( "[" );
			String typeNameNoArray = ( vTypeNameNoArray.Length > 0 ) ? vTypeNameNoArray[0] : typeName;
			String arrayEnding = typeName.Substring( typeNameNoArray.Length );
			typeNameNoArray = CheckTypeSecurity( typeNameNoArray  );
			if( typeNameNoArray == null ) return null;
			return typeNameNoArray + arrayEnding;
		}

		Type CheckTypeSecurityRecursive( Type t )
		{
			TypeInfo typeInfo = t.GetTypeInfo();
			if( typeInfo == null ) return null;
			String typeName = typeInfo.ToString();
			String [] vTypeNameNoArray = typeName.Split( "[" );
			typeName = ( vTypeNameNoArray.Length > 0 ) ? vTypeNameNoArray[0] : typeName;
			String [] vTypeNameNoGenerics = typeName.Split( "`" );
			typeName = ( vTypeNameNoGenerics.Length > 0 ) ? vTypeNameNoGenerics[0] : typeName;

			if( CheckTypeSecurity( typeName ) == null ) return null;
			foreach( Type tt in typeInfo.GenericTypeArguments )
			{
				if( CheckTypeSecurityRecursive( tt ) == null ) return null;
			}
			return t;
		}

		////////////////////////////////////////////////////////////////////////////////////
		// INTERNAL CHECKING ///////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////

		public Type GetNativeTypeFromSerializee( Serializee s )
		{
			Dictionary< String, Serializee > ses = s.AsMap();
			String typeName = ses["n"].AsString();
			if( box.classes.ContainsKey( typeName ) ) return null;
			typeName = CheckReplaceTypeNotRecursive( typeName );
			if( typeName == null ) return null;

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

		public String GetNativeTypeNameFromSerializee( Serializee s )
		{
			Dictionary< String, Serializee > ses = s.AsMap();
			String typeName = ses["n"].AsString();
			if( box.classes.ContainsKey( typeName ) ) return typeName;
			typeName = CheckReplaceTypeNotRecursive( typeName );
			if( typeName == null ) return null;

			Serializee g;
			if( ses.TryGetValue( "g", out g ) )
			{
				String ret = typeName + "`[";
				Serializee [] gs = g.AsArray();
				for( int i = 0; i < gs.Length; i++ )
					ret += (i==0?"":",") + GetNativeTypeNameFromSerializee( gs[i] );
				return ret + "]";
			}
			else
			{
				return typeName;
			}
		}

		public Type [] TypeNamesToArrayOfNativeTypes( Serializee [] sa )
		{
			Type [] ret = new Type[sa.Length];
			for( int i = 0; i < sa.Length; i++ )
				ret[i] = GetNativeTypeFromSerializee( sa[i] );
			return ret;
		}


		public MethodBase InternalGetNativeMethodFromTypeAndNameNoSecurity( Type declaringType, String name, Type [] parameters, Type [] genericArguments, String fullSignature )
		{
			MethodBase m;
			// Can we combine Constructor + Method?
			m = declaringType.GetMethod(
				name,
				genericArguments.Length,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
				null,
				CallingConventions.Any,
				parameters,
				null ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."

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
		    	m = ((MethodInfo)m).MakeGenericMethod( genericArguments );
			}

			return m;
		}

	}


	// This overrides System.Runtime.CompilerServices.RuntimeHelpers
	// WARNING: This class is 100% available from WITHIN cilbox.
	public class CilboxPublicUtils
	{
		public static void InitializeArray(Array arr, byte[] initializer)
		{
			if (initializer == null || arr == null)
				throw new Exception( "Error, array or initializer are null" );
			if (initializer.Length != System.Runtime.InteropServices.Marshal.SizeOf(arr.GetType().GetElementType()) * arr.Length)
				throw new Exception( "InitializeArray requires identical array byte length " + initializer.Length );
			Buffer.BlockCopy(initializer, 0, arr, 0, initializer.Length);
		}

		public static UInt32 ComputeStringHashProxy(System.String s) { return (uint)s.GetHashCode(); }
	}

	// Be warned that this class is totally available to the inner box.
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

