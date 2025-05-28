using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.Reflection;
#endif

// To add [Cilboxable] to your classes that you want exported.
public class CilboxableAttribute : Attribute { }

namespace Cilbox
{
	public class CilboxMethod
	{
		public void Load( CilboxClass cclass, String name, String payload )
		{
			methodName = name;
			parentClass = cclass;
			OrderedDictionary methodProps = CilboxUtil.DeserializeDict( payload );

			var vl = CilboxUtil.DeserializeDict( (String)methodProps["locals"] );
			methodLocals = new String[vl.Count];
			methodLocalTypes = new String[vl.Count];
			int iid = 0;
			foreach( DictionaryEntry ln in vl )
			{
				methodLocals[iid] = (String)ln.Key;
				methodLocalTypes[iid] = (String)ln.Key;
			}

			var pl = (String)methodProps["body"];
			int bl = pl.Length/2;
			byteCode = new byte[bl];

			//Debug.Log( methodName + " " + pl );
			for( int i = 0; i < bl; i++ )
			{
				int v = CilboxUtil.IntFromHexChar( pl[i*2+0] );
				if( v < 0 ) break;
				byte b = (byte)v;
				v = CilboxUtil.IntFromHexChar( pl[i*2+1] );
				if( v < 0 ) break;
				b = (byte)(v | (b<<4));
				byteCode[i] = b;
			}

			MaxStackSize = Convert.ToInt32(((String)methodProps["maxStack"]));
			isStatic = Convert.ToInt32(((String)methodProps["isStatic"])) != 0;
		}
		public void Breakwarn( String message, int bytecodeplace )
		{
			// TODO: Add debugging info.
			Debug.Log( $"Breakwarn: {message} Class: {parentClass.className}, Function: {methodName}, Bytecode: {bytecodeplace}" );
		}

		public object Interpret( CilboxProxy ths, object [] parametersIn )
		{
			if( byteCode == null || byteCode.Length == 0 || disabled ) return null;
			Cilbox box = parentClass.box;
	
			// Do the magic.

			int i;

			i = 0;
			String stbc = ""; for( i = 0; i < byteCode.Length; i++ ) stbc += byteCode[i].ToString("X2") + " "; Debug.Log( "INTERPRETING " + methodName + " VALS:" + stbc + " MAX: " + MaxStackSize );
			//Debug.Log( ths.fields );

			object [] stack = new object[MaxStackSize];
			object [] localVars = new object[methodLocals.Length];
			int sp = 0;

			object [] parameters;
			if( isStatic )
				parameters = parametersIn;
			else
			{
				int plen = 0;
				if( parametersIn != null ) plen = parametersIn.Length;
				parameters = new object[plen+1];
				parameters[0] = ths;
			}

			bool cont = true;
			int ctr = 0;
			i = 0;
			try
			{
				do
				{
					byte b = byteCode[i++];
					String stackSt = "";
					for( int sk = 0; sk < stack.Length; sk++ )
					{
						if( stack[sk] != null )
							stackSt += "/" + stack[sk].ToString();
						else
							stackSt += "/null";
					}
					//Debug.Log( "Bytecode " + b.ToString("X2") + " @ " + (i-1) + " " + stackSt);

					switch( b )
					{
					case 0x00: break; // nop
					case 0x01: cont = false; Breakwarn( "Debug Break", i ); break; // break
					case 0x02: stack[sp++] = parameters[0]; break; //ldarg.0
					case 0x03: stack[sp++] = parameters[1]; break; //ldarg.1
					case 0x04: stack[sp++] = parameters[2]; break; //ldarg.2
					case 0x05: stack[sp++] = parameters[3]; break; //ldarg.3
					case 0x06: stack[sp++] = localVars[0]; break; //ldloc.0
					case 0x07: stack[sp++] = localVars[1]; break; //ldloc.1
					case 0x08: stack[sp++] = localVars[2]; break; //ldloc.2
					case 0x09: stack[sp++] = localVars[3]; break; //ldloc.3
					case 0x0a: localVars[0] = stack[--sp]; break; //stloc.0
					case 0x0b: localVars[1] = stack[--sp]; break; //stloc.1
					case 0x0c: localVars[2] = stack[--sp]; break; //stloc.2
					case 0x0d: localVars[3] = stack[--sp]; break; //stloc.3
					//case 0x0e: stack[sp++] = parameters[byteCode[i++]]; break; //ldarg.0
					//case 0x0e: stack[sp++] = parameters[byteCode[i++]]; break; //ldarg.0
					// Some more...
					case 0x14: stack[sp++] = null; break; // ldnull
					case 0x15: stack[sp++] = (Int32)(-1); break; // ldc.i4.m1
					case 0x16: stack[sp++] = (Int32)(0); break; // ldc.i4.0
					case 0x17: stack[sp++] = (Int32)(1); break; // ldc.i4.1
					case 0x18: stack[sp++] = (Int32)(2); break; // ldc.i4.2
					case 0x19: stack[sp++] = (Int32)(3); break; // ldc.i4.3
					case 0x1a: stack[sp++] = (Int32)(4); break; // ldc.i4.4
					case 0x1b: stack[sp++] = (Int32)(5); break; // ldc.i4.5
					case 0x1c: stack[sp++] = (Int32)(6); break; // ldc.i4.6
					case 0x1d: stack[sp++] = (Int32)(7); break; // ldc.i4.7
					case 0x1e: stack[sp++] = (Int32)(8); break; // ldc.i4.8

					case 0x1f: Debug.Log( "ldc " + byteCode[i] ); stack[sp++] = byteCode[i++]; break; // ldc.i4.s <int8>
					case 0x20: stack[sp++] = BytecodeAs32( ref i ); break; // ldc.i4.s <int8>
					case 0x28: 
					{
						// Call
						uint bc = BytecodeAs32( ref i );
						CilMetadataTokenInfo dt = box.metadatas[bc];
						if( dt.nativeToken != 0 )
						{
							MethodBase st = dt.assembly.ManifestModule.ResolveMethod((int)dt.nativeToken);
							ParameterInfo [] pa = st.GetParameters();
							//MethodInfo mi = (MethodInfo)st;
							int numFields = pa.Length;
							object callthis = null;
							object [] callpar = new object[numFields];
							int ik;
							for( ik = 0; ik < numFields; ik++ )
							{
								callpar[numFields-ik-1] = stack[--sp];
							}
							if( !st.IsStatic )
								callthis = stack[--sp];
							//Debug.Log( " " + ((st.IsStatic)?"STATIC":"INSTANCE") + " / " + st.Name + " / " + callthis + " / fields=" + numFields );
							stack[sp++] = st.Invoke( callthis, callpar );
						}
						else
						{
							Breakwarn( $"Function {dt.fields[2]} not found", i );
						}
						break;
					}
					case 0x2a: cont = false; break; // ret

					// XXX This is wrong.  Need to learn how to unbox correctly.
					case 0x58: Debug.Log( "Add: " + sp  ); Debug.Log( stack[sp-1].GetType() + " + " + stack[sp-2].GetType() ); stack[sp-2] = Convert.ToInt32(stack[sp-1]) + Convert.ToInt32(stack[sp-2]); sp--; break; //add

					case 0x72:
					{
						uint bc = BytecodeAs32( ref i );
						stack[sp++] = box.metadatas[bc].fields[0];
						break; //ldfld
					}

					case 0x7b: 
					{
						--sp; // Should be "This" XXX WRONG
						uint bc = BytecodeAs32( ref i );
						stack[sp++] = ths.fields[box.metadatas[bc].fieldIndex];
						break; //ldfld
					}
					case 0x7d:
					{
						uint bc = BytecodeAs32( ref i );
						ths.fields[box.metadatas[bc].fieldIndex] = stack[--sp];
						--sp; // Should be "This" XXX WRONG
						break; //stfld
					}
					case 0x7e: 
					{
						uint bc = BytecodeAs32( ref i );
						stack[sp++] = parentClass.staticFields[box.metadatas[bc].fieldIndex];
						break; //ldsfld
					}

					case 0x8C: BytecodeAs32( ref i ); break; // box (This pulls off a type, but I think everything is boxed, so no big deal)

					default: Breakwarn( $"Opcode {b} unimplemented", i ); disabled = true; cont = false; break;

					}
					//Update 022040e201007d020000042a

					ctr++;
					if( ctr > 10000 )
					{
						Breakwarn( "Infinite Loop", i );
						disabled = true;
						break;
					}
				}
				while( cont );
			}
			catch( Exception e )
			{
				disabled = true;
				Breakwarn( e.ToString(), i );
			}

			return null;
		}

		uint BytecodeAs32( ref int i )
		{
			return (uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, 4 );
		}

		public bool disabled;
		public CilboxClass parentClass;
		public int MaxStackSize;
		public String methodName;
		public String[] methodLocals;
		public String[] methodLocalTypes;
		public byte[] byteCode;
		public bool isStatic;
	}

	public class CilboxClass
	{
		public Cilbox box;
		public String className;

		public object[] staticFields;
		public String[] staticFieldNames;
		public Type[] staticFieldTypes;

		public String[] instanceFieldNames;
		public Type[] instanceFieldTypes;

		// Conversion from name to metadata id.
		//public Dictionary< String, uint > metadatas;
		public Dictionary< String, uint > methodNameToIndex;

		public CilboxMethod [] methods;

		public uint [] importFunctionToId; // from ImportFunctionID

		public CilboxClass( Cilbox box, String className, String classData )
		{
			this.box = box;
			this.className = className;
			OrderedDictionary classProps = CilboxUtil.DeserializeDict( classData );

			uint id = 0;
			OrderedDictionary staticFields = CilboxUtil.DeserializeDict( (String)classProps["staticFields"] );
			int sfnum = staticFields.Count;
			this.staticFields = new object[sfnum];
			staticFieldNames = new String[sfnum];
			staticFieldTypes = new Type[sfnum];
			foreach( DictionaryEntry k in staticFields )
			{
				String fieldName = staticFieldNames[id] = (String)k.Key;
				Type t = staticFieldTypes[id] = Type.GetType( (String)k.Value );

				//staticFieldIDs[id] = Cilbox.FindInternalMetadataID( className, 4, fieldName );
				this.staticFields[id] = CilboxUtil.FillPossibleSystemType( t );
				id++;
			}

			OrderedDictionary instanceFields = CilboxUtil.DeserializeDict( (String)classProps["instanceFields"] );
			int ifnum = instanceFields.Count;
			instanceFieldNames = new String[ifnum];
			instanceFieldTypes = new Type[ifnum];
			//instanceFieldIDs = new uint[ifnum];
			id = 0;
			foreach( DictionaryEntry k in instanceFields )
			{
				String fieldName = instanceFieldNames[id] = (String)k.Key;
				instanceFieldTypes[id] = Type.GetType( (String)k.Value );
				//instanceFieldIDs[id] = Cilbox.FindInternalMetadataID( className, 4, fieldName );
				id++;
			}

			id = 0;
			OrderedDictionary deserMethods = CilboxUtil.DeserializeDict( (String)classProps["methods"] );
			int mnum = deserMethods.Count;
			methods = new CilboxMethod[mnum];
			methodNameToIndex = new Dictionary< String, uint >();
			foreach( DictionaryEntry k in deserMethods )
			{
				methods[id] = new CilboxMethod();
				methods[id].Load( this, (String)k.Key, (String)k.Value );
				methodNameToIndex[(String)k.Key] = id;
				id++;
			}

			int numImportFunctions = Enum.GetNames(typeof(ImportFunctionID)).Length;
			importFunctionToId = new uint[numImportFunctions];
			for( int i = 0; i < numImportFunctions; i++ )
			{
				String fn = Enum.GetName(typeof(ImportFunctionID), i);
				if( i == 0 ) fn = ".ctor";
				uint idx = 0;
				importFunctionToId[i] = 0xffffffff;
				if( methodNameToIndex.TryGetValue(fn, out idx ) )
				{
					importFunctionToId[i] = idx;
				}
				//Debug.Log( "MATCHING + " + fn + " : " + i + " " + importFunctionToId[i] );
			}
		}
	}

	public class CilMetadataTokenInfo
	{
		public CilMetadataTokenInfo( MetaTokenType type, String [] fields ) { this.type = type; this.fields = fields; }
		public MetaTokenType type;
		public int fieldIndex; // Only used for fields.
		public int nativeToken; // Only used for native function calls.
		public Assembly assembly; // Only used for native function calls.

		// For string, type = 7, string is in fields[0]
		// For methods, type = 10, Declaring Type is in fields[0], Method is in fields[1], Full name is in fields[2] assembly name is in fields[3]
		// For fields, type = 4, Declaring Type is in fields[0], Name is in fields[1], Type is in fields[2]
		public String [] fields;
	}

	public enum MetaTokenType
	{
		mtField = 4,
		mtString = 7,
		mtMethod = 10,
	}

	public class Cilbox : MonoBehaviour
	{
		public Dictionary< String, CilboxClass > classes;
		public CilMetadataTokenInfo [] metadatas;
		public String assemblyData;
		public bool initialized;

		Cilbox()
		{
			initialized = false;
		}

		public void BoxInitialize()
		{
			Debug.Log( $"Cilbox Initialize called {initialized}" );
			if( initialized ) return;
			initialized = true;

			Debug.Log( "Cilbox Initialize" );
			classes = new Dictionary< String, CilboxClass >();

			//CilboxEnvironmentHolder [] se = UnityEngine.Object.FindObjectsByType<CilboxEnvironmentHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			//CilboxEnvironmentHolder [] se = Resources.FindObjectsOfTypeAll(typeof(CilboxEnvironmentHolder)) as CilboxEnvironmentHolder [];
			//if( se.Length == 0 )
			//{
			//	Debug.LogError( "Can't find cilly environment holder. Something went wrong with CilboxScenePostprocessor" );
			//	return;
			//}

			OrderedDictionary assemblyRoot = CilboxUtil.DeserializeDict( assemblyData );
			OrderedDictionary classData = CilboxUtil.DeserializeDict( (String)assemblyRoot["classes"] );
			OrderedDictionary metaData = CilboxUtil.DeserializeDict( (String)assemblyRoot["metadata"] );

			metadatas = new CilMetadataTokenInfo[metaData.Count+1]; // element 0 is invalid.
			metadatas[0] = new CilMetadataTokenInfo( 0, new String[]{ "INVALID METADATA" } );

			foreach( DictionaryEntry v in metaData )
			{
				int mid = Convert.ToInt32((String)v.Key);
				String [] st = CilboxUtil.DeserializeArray( (String)v.Value );

				//Debug.Log( $"ST {(String)v.Value} => {st.Length} from {(String)v.Key}" );
				if( st.Length < 2 )
				{
					Debug.LogWarning( "Metadata read error. Could not interpret " + (String)v.Value );
					continue;
				}
				String [] fields = new String[st.Length-1];
				Array.Copy( st, 1, fields, 0, st.Length-1 );
				MetaTokenType metatype = (MetaTokenType)Convert.ToInt32(st[0]);
				CilMetadataTokenInfo t = metadatas[mid] = new CilMetadataTokenInfo( metatype, fields );

				if( metatype == MetaTokenType.mtField && st.Length > 4 )
				{
					// The type has been "sealed" so-to-speak. In that we have an index for it.
					metadatas[mid].fieldIndex = Convert.ToInt32(st[4]);
				}

				if( metatype == MetaTokenType.mtMethod )
				{
					// Function call
					// TODO: Need to figure out if this is an interpreted call or a native call.
					// (Or a please explode, for instance if you violate security rules)
					String parentType = st[1];
					String fullSignature = st[3];
					String useAssembly = st[4];

					foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						if( assembly.GetName().ToString() != useAssembly ) continue;
						var tt = assembly.GetType( parentType );
						if (tt != null)
						{
							MethodBase[] methods = tt.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
							foreach( MethodBase m in methods )
							{
								if( m.ToString() == fullSignature )
								{
									t.nativeToken = m.MetadataToken;
									t.assembly = assembly;
									break;
								}
							}

							if( t.nativeToken == 0 )
							{
								methods = tt.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
								foreach( MethodBase m in methods )
								{
									if( m.ToString() == fullSignature )
									{
										t.nativeToken = m.MetadataToken;
										t.assembly = assembly;
										break;
									}
								}
							}
						}
					}
				}
			}
			

			foreach( DictionaryEntry v in classData )
			{
				CilboxClass cls = new CilboxClass( this, (String)v.Key, (String)v.Value );
				classes[(String)v.Key] = cls;
			}
		}

		public CilboxClass GetClass( String className )
		{
			if( className == null ) return null;
			CilboxClass ret;
			if( classes.TryGetValue(className, out ret)) return ret;
			return null;
		}

		public object InterpretIID( CilboxClass cls, CilboxProxy ths, ImportFunctionID iid, object [] parameters )
		{
			if( cls == null ) return null;
			uint index = cls.importFunctionToId[(uint)iid];
			if( index == 0xffffffff ) return null;
			return cls.methods[index].Interpret( ths, parameters );
		}
	}



	///////////////////////////////////////////////////////////////////////////
	//  EXPORTING  ////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////

	#if UNITY_EDITOR
	public class CilboxScenePostprocessor {

		[PostProcessSceneAttribute (2)]
		public static void OnPostprocessScene() {
			Debug.Log( "Postprocessing scene." );

			Assembly proxyAssembly = typeof(CilboxProxy).Assembly;

			OrderedDictionary assemblyMetadata = new OrderedDictionary();
			Dictionary< int, uint> assemblyMetadataReverseOriginal = new Dictionary< int, uint >();

			int mdcount = 1; // token 0 is invalid.

			OrderedDictionary classes = new OrderedDictionary();
			Dictionary< String, OrderedDictionary > allClassMethods = new Dictionary< String, OrderedDictionary>();

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				OrderedDictionary methods = new OrderedDictionary();

				int mtyp; // Which round of methods are we getting.

				// Iterate twice. Once for methods, then for constructors.
				for( mtyp = 0; mtyp < 2; mtyp++ )
				{
					MethodBase[] me;
					if( mtyp == 0 )
						me = type.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
					else
						me = type.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );

					foreach( var m in me )
					{
						if( m.DeclaringType.Assembly != proxyAssembly )
						{
							// We can't export things that are part of Unity.
							continue;
						}

						String methodName = m.Name;
						OrderedDictionary MethodProps = new OrderedDictionary();
						//Debug.Log( type + " / " + m.Name );
						MethodBody mb = m.GetMethodBody();
						if( mb == null )
						{
							//Debug.Log( $"NOTE: {m.Name} does not have a body" );
							// Things like MemberwiseClone, etc.
							continue;
						}

						byte [] byteCode = mb.GetILAsByteArray();

						//if( !ExtractAndTransformMetas( proxyAssembly, ref ba, ref assemblyMetadata, ref assemblyMetadataReverseOriginal, ref mdcount ) ) continue;
						//static bool ExtractAndTransformMetas( Assembly proxyAssembly, ref byte [] byteCode, ref OrderedDictionary od, ref Dictionary< uint, uint > assemblyMetadataReverseOriginal, ref int mdcount )
						{
							int i = 0;
							i = 0;
							try {
								do
								{
									CilboxUtil.OpCodes.OpCode oc = CilboxUtil.OpCodes.ReadOpCode( byteCode, ref i );
									int opLen = CilboxUtil.OpCodes.OperandLength[(int)oc.OperandType];
									int backupi = i;
									uint operand = (uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, opLen );
									bool changeOperand = true;
									uint writebackToken = (uint)mdcount;

									// Check to see if this is a meta that we care about.  Then rewrite in a new identifier.
									// ResolveField, ResolveMember, ResolveMethod, ResolveSignature, ResolveString, ResolveType
									// We sort of want to let the other end know what they are. So we mark them with the code
									// from here: https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Metadata/TableHeap.cs#L16

									if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineString )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = (uint)mdcount;
											assemblyMetadata[(mdcount++).ToString()] = ((int)MetaTokenType.mtString) + "\t" + proxyAssembly.ManifestModule.ResolveString( (int)operand );
										}
									}
									else if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineMethod )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = (uint)mdcount;
											MethodBase tmb = proxyAssembly.ManifestModule.ResolveMethod( (int)operand );
											assemblyMetadata[(mdcount++).ToString()] = ((int)MetaTokenType.mtMethod) + "\t" + tmb.DeclaringType + "\t" + tmb.Name + "\t" + tmb + "\t" + tmb.DeclaringType.Assembly.GetName();
										}
									}
									else if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineField )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = (uint)mdcount;
											FieldInfo rf = proxyAssembly.ManifestModule.ResolveField( (int)operand );
											assemblyMetadata[(mdcount++).ToString()] = ((int)MetaTokenType.mtField) + "\t" + rf.DeclaringType + "\t" + rf.Name + "\t" + rf.FieldType;
										}
									}
									else
										changeOperand = false;

									if( changeOperand )
									{
										i = backupi;
										//Debug.Log( "MDC: " + mdcount + "Found OP:" + operand.ToString( "X8" ) + " WBT: " + writebackToken + " VALUE:" + assemblyMetadata[(writebackToken).ToString()] );
										assemblyMetadataReverseOriginal[(int)operand] = writebackToken;
										CilboxUtil.BytecodeReplaceLiteral( ref byteCode, ref i, opLen, writebackToken );
									}
									if( i >= byteCode.Length ) break;
								} while( true );
							}
							catch( Exception e )
							{
								Debug.LogWarning( e );
								continue;
							}
						}

						String byteCodeStr = "";
						for( int i = 0; i < byteCode.Length; i++ )
						{
							int b = byteCode[i];
							byteCodeStr += CilboxUtil.HexFromNum( b>>4 ) + CilboxUtil.HexFromNum( b&0xf );
						}

						MethodProps["body"] = byteCodeStr;

						OrderedDictionary localVars = new OrderedDictionary();
						foreach (LocalVariableInfo lvi in mb.LocalVariables)
							localVars[lvi.ToString()] = lvi.GetType().FullName;
						MethodProps["locals"] = CilboxUtil.SerializeDict( localVars );

						ParameterInfo [] parameters = m.GetParameters();

						OrderedDictionary argVars = new OrderedDictionary();
						foreach (ParameterInfo p in parameters)
						{
							argVars[p.Name] = p.ParameterType.ToString();
						}
						MethodProps["parameters"] = CilboxUtil.SerializeDict( argVars );

						MethodProps["maxStack"] = mb.MaxStackSize.ToString();
						MethodProps["isStatic"] = m.IsStatic ? "1" : "0";

						//metadatas[methodName] = m.MetadataToken.ToString(); // There's also MethodHandle
						methods[methodName] = CilboxUtil.SerializeDict( MethodProps );
					}
				}

				allClassMethods[type.FullName] = methods;
			}

			// Now that we've iterated through all classes, and collected all possible uses of field IDs,
			// go through the classes again, collecting the fields themselves.

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				OrderedDictionary staticFields = new OrderedDictionary();
				int sfid = 0;
				FieldInfo[] fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static );
				foreach( var f in fi )
				{
					staticFields[f.Name] = f.FieldType.FullName;

					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
					{
						assemblyMetadata[mdid.ToString()] += "\t" + sfid;
					}
					sfid++;
				}

				OrderedDictionary instanceFields = new OrderedDictionary();
				fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				int ifid = 0;
				foreach( var f in fi )
				{
					instanceFields[f.Name] = f.FieldType.FullName;
					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
					{
						assemblyMetadata[mdid.ToString()] += "\t" + ifid;
					}
					ifid++;
				}

				OrderedDictionary classProps = new OrderedDictionary();
				classProps["methods"] = CilboxUtil.SerializeDict( allClassMethods[type.FullName] );
				classProps["staticFields"] = CilboxUtil.SerializeDict( staticFields );
				classProps["instanceFields"] = CilboxUtil.SerializeDict( instanceFields );
				classes[type.FullName] = CilboxUtil.SerializeDict( classProps );
			}

			OrderedDictionary assemblyRoot = new OrderedDictionary();
			assemblyRoot["classes"] = CilboxUtil.SerializeDict( classes );
			assemblyRoot["metadata"] = CilboxUtil.SerializeDict( assemblyMetadata );

			String sAllAssemblyData = CilboxUtil.SerializeDict( assemblyRoot );

			Cilbox [] se = Resources.FindObjectsOfTypeAll(typeof(Cilbox)) as Cilbox [];
			Cilbox tac;
			if( se.Length != 0 )
			{
				tac = se[0];
			}
			else
			{
				GameObject cilboxDataObject = new GameObject("CilboxData");
				cilboxDataObject.hideFlags = HideFlags.HideAndDontSave;
				tac = cilboxDataObject.AddComponent( typeof(Cilbox) ) as Cilbox;
			}
			tac.assemblyData = sAllAssemblyData;
			tac.initialized = false; // Force reinitializaiton.

			// Iterate over all GameObjects, and find the ones that have Cilboxable scripts.
			object[] obj = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			foreach (object o in obj)
			{
				GameObject g = (GameObject) o;
				MonoBehaviour [] scripts = g.GetComponents<MonoBehaviour>();
				foreach (MonoBehaviour m in scripts )
				{
					// Skip null objects.
					if (m == null)
						continue;
					object[] attribs = m.GetType().GetCustomAttributes(typeof(CilboxableAttribute), true);
					// Not a proxiable script.
					if (attribs == null || attribs.Length <= 0)
						continue;

					CilboxProxy p = g.AddComponent<CilboxProxy>();
					p.SetupProxy( tac, m );
					UnityEngine.Object.DestroyImmediate( m );
				}
			}
		}
	}
	#endif

	public enum ImportFunctionID
	{
		dotCtor, // Must be at index 0.
		Update,
		Start,
		Awake,
	}
}

