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
	public class CilboxEnvironmentHolder : MonoBehaviour
	{
		public String stringData;
	}

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
	
			// Do the magic.

			int i;

			i = 0;
			String stbc = "";
			for( i = 0; i < byteCode.Length; i++ )
				stbc += byteCode[i].ToString("X2") + " ";
			Debug.Log( "INTERPRETING " + methodName + " VALS:" + stbc + " MAX: " + MaxStackSize );

			Debug.Log( ths.fields );

// Start 02/02/7b01000004/17/58/7d0100000402027b0200000418587d02000004722f000070027b01000004//8c1f000001027b020000048c1f000001280c00000a280b00000a2a


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
					Debug.Log( "Bytecode " + b.ToString("X2") + " @ " + (i-1) + " " + stackSt);

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
						int bc = BytecodeAs32( ref i );
						MethodBase st = typeof(TestScript).Module.ResolveMethod(bc);
						ParameterInfo [] pa = st.GetParameters();
						//MethodInfo mi = (MethodInfo)st;
						int numFields = pa.Length;
						object callthis = null;
						object [] callpar = new object[numFields];
						int ik;
						for( ik = 0; ik < numFields; ik++ )
						{
							callpar[numFields-ik-1] = stack[--sp];
							Debug.Log( "VAL:" + callpar[ik] );
						}
						if( !st.IsStatic )
							callthis = stack[--sp];
						Debug.Log( " " + ((st.IsStatic)?"STATIC":"INSTANCE") + " / " + st.Name + " / " + callthis + " / fields=" + numFields );
						stack[sp++] = st.Invoke( callthis, callpar );
						break;
					}
					case 0x2a: cont = false; break; // ret

					// XXX This is wrong.  Need to learn how to unbox correctly.
					case 0x58: Debug.Log( "Add: " + sp  ); Debug.Log( stack[sp-1].GetType() + " + " + stack[sp-2].GetType() ); stack[sp-2] = Convert.ToInt32(stack[sp-1]) + Convert.ToInt32(stack[sp-2]); sp--; break; //add

					case 0x72:
					{
						// This is wrong.  We need to pull strings out in advance.
						int bc = BytecodeAs32( ref i );
						String st = typeof(TestScript).Module.ResolveString(bc);
						stack[sp++] = st;
						Debug.Log( "STRING: " + st + " from " + bc.ToString("X8") );
						break; //ldfld
					}

					case 0x7b: 
					{
						--sp; // Should be "This" XXX WRONG
						int mi;
						int bc = BytecodeAs32( ref i );
						if( !parentClass.instanceMetadataIdToFieldID.TryGetValue( bc, out mi ) )
							Breakwarn( $"Could not get field ID {bc} from metadata.", i );
						stack[sp++] = ths.fields[mi];
						break; //ldfld
					}
					case 0x7d:
					{
						int mi;
						int bc = BytecodeAs32( ref i );
						if( !parentClass.instanceMetadataIdToFieldID.TryGetValue( bc, out mi ) )
							Breakwarn( $"Could not get field ID {bc} from metadata.", i );
						ths.fields[mi] = stack[--sp];
						--sp; // Should be "This" XXX WRONG
						break; //stfld
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

		int BytecodeAs32( ref int i )
		{
			int ret = byteCode[i] |
				(byteCode[i+1]<<8) |
				(byteCode[i+2]<<16) |
				(byteCode[i+3]<<24);
			i+=4;
			return ret;
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
		public CilboxClass( String className, String classData )
		{
			this.className = className;
			OrderedDictionary classProps = CilboxUtil.DeserializeDict( classData );
			metadatas = new Dictionary< String, int >();
			foreach( DictionaryEntry k in CilboxUtil.DeserializeDict( (String)classProps["metadatas"] ) )
			{
				metadatas[(String)k.Key] = Convert.ToInt32( (String)k.Value );
			}

			int id = 0;
			OrderedDictionary staticFields = CilboxUtil.DeserializeDict( (String)classProps["staticFields"] );
			int sfnum = staticFields.Count;
			staticObjects = new object[sfnum];
			staticFieldNames = new String[sfnum];
			staticFieldTypes = new Type[sfnum];
			staticFieldIDs = new int[sfnum];
			foreach( DictionaryEntry k in staticFields )
			{
				staticFieldNames[id] = (String)k.Key;
				Type t = staticFieldTypes[id] = Type.GetType( (String)k.Value );
				staticFieldIDs[id] = metadatas[(String)k.Key];
				staticObjects[id] = CilboxUtil.FillPossibleSystemType( t );
				id++;
			}

			OrderedDictionary instanceFields = CilboxUtil.DeserializeDict( (String)classProps["instanceFields"] );
			int ifnum = instanceFields.Count;
			instanceFieldNames = new String[ifnum];
			instanceFieldTypes = new Type[ifnum];
			instanceFieldIDs = new int[ifnum];
			instanceMetadataIdToFieldID = new Dictionary< int, int >();
			id = 0;
			foreach( DictionaryEntry k in instanceFields )
			{
				instanceFieldNames[id] = (String)k.Key;
				instanceFieldTypes[id] = Type.GetType( (String)k.Value );
				instanceFieldIDs[id] = metadatas[(String)k.Key];
				instanceMetadataIdToFieldID[metadatas[(String)k.Key]] = id;
				id++;
			}

			id = 0;
			OrderedDictionary deserMethods = CilboxUtil.DeserializeDict( (String)classProps["methods"] );
			int mnum = deserMethods.Count;
			methods = new CilboxMethod[mnum];
			methodNameToIndex = new Dictionary< String, int >();
			foreach( DictionaryEntry k in deserMethods )
			{
				methods[id] = new CilboxMethod();
				methods[id].Load( this, (String)k.Key, (String)k.Value );
				methodNameToIndex[(String)k.Key] = id;
				id++;
			}

			int numImportFunctions = Enum.GetNames(typeof(ImportFunctionID)).Length;
			importFunctionToId = new int[numImportFunctions];
			for( int i = 0; i < numImportFunctions; i++ )
			{
				String fn = Enum.GetName(typeof(ImportFunctionID), i);
				if( i == 0 ) fn = ".ctor";
				int idx = 0;
				importFunctionToId[i] = -1;
				if( methodNameToIndex.TryGetValue(fn, out idx ) )
				{
					importFunctionToId[i] = idx;
				}
				Debug.Log( "MATCHING + " + fn + " : " + i + " " + importFunctionToId[i] );
			}
			//Debug.Log( classProps["metadatas"] );
		}

		public String className;

		public object[] staticObjects;
		public String[] staticFieldNames;
		public Type[] staticFieldTypes;
		public int[] staticFieldIDs;

		public String[] instanceFieldNames;
		public Type[] instanceFieldTypes;
		public int[] instanceFieldIDs;
		public Dictionary< int, int > instanceMetadataIdToFieldID;

		// Conversion from name to metadata id.
		public Dictionary< String, int > metadatas;
		public Dictionary< String, int > methodNameToIndex;

		public CilboxMethod [] methods;

		public int [] importFunctionToId; // from ImportFunctionID
	}

	public static class Cilbox
	{
		public static Dictionary< String, CilboxClass > classes;

		static Cilbox()
		{
			Debug.Log( "Cilbox Initialize" );
			classes = new Dictionary< String, CilboxClass >();

			//CilboxEnvironmentHolder [] se = UnityEngine.Object.FindObjectsByType<CilboxEnvironmentHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			CilboxEnvironmentHolder [] se = Resources.FindObjectsOfTypeAll(typeof(CilboxEnvironmentHolder)) as CilboxEnvironmentHolder [];
			if( se.Length == 0 )
			{
				Debug.LogError( "Can't find cilly environment holder. Something went wrong with CilboxScenePostprocessor" );
				return;
			}

			OrderedDictionary classData = CilboxUtil.DeserializeDict( se[0].stringData );

			foreach( DictionaryEntry v in classData )
			{
				classes[(String)v.Key] = new CilboxClass( (String)v.Key, (String)v.Value );
			}
		}

		public static CilboxClass GetClass( String className )
		{
			if( className == null ) return null;
			CilboxClass ret;
			if( classes.TryGetValue(className, out ret)) return ret;
			return null;
		}

		public static object InterpretIID( CilboxClass cls, CilboxProxy ths, ImportFunctionID iid, object [] parameters )
		{
			if( cls == null ) return null;
			int index = cls.importFunctionToId[(int)iid];
			if( index < 0 ) return null;
			return cls.methods[index].Interpret( ths, parameters );
		}
	}

	#if UNITY_EDITOR
	public class CilboxScenePostprocessor {

		[PostProcessSceneAttribute (2)]
		public static void OnPostprocessScene() {
			Debug.Log( "Postprocessing scene." );

			Assembly mscorlib = typeof(CilboxProxy).Assembly;

			OrderedDictionary classes = new OrderedDictionary();
			foreach (Type type in mscorlib.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				OrderedDictionary metadatas = new OrderedDictionary();

				OrderedDictionary methods = new OrderedDictionary();
				int mtyp;
				for( mtyp = 0; mtyp < 2; mtyp++ )
				{
					MethodBase[] me;
					if( mtyp == 0 )
						me = type.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
					else
						me = type.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );

					foreach( var m in me )
					{
						String methodName = m.Name;
						OrderedDictionary MethodProps = new OrderedDictionary();
						Debug.Log( type + " / " + m.Name );
						MethodBody mb = m.GetMethodBody();
						if( mb == null )
						{
							//Debug.Log( $"NOTE: {m.Name} does not have a body" );
							// Things like MemberwiseClone, etc.
							continue;
						}

						OrderedDictionary localVars = new OrderedDictionary();
						foreach (LocalVariableInfo lvi in mb.LocalVariables)
							localVars[lvi.ToString()] = lvi.GetType().FullName;
						MethodProps["locals"] = CilboxUtil.SerializeDict( localVars );

						String byteCode = "";
						byte [] ba = mb.GetILAsByteArray();
						for( int i = 0; i < ba.Length; i++ )
						{
							int b = ba[i];
							byteCode += CilboxUtil.HexFromNum( b>>4 ) + CilboxUtil.HexFromNum( b&0xf );
						}
						MethodProps["body"] = byteCode;

						ParameterInfo [] parameters = m.GetParameters();

						OrderedDictionary argVars = new OrderedDictionary();
						foreach (ParameterInfo p in parameters)
						{
							argVars[p.Name] = p.ParameterType.ToString();
						}
						MethodProps["parameters"] = CilboxUtil.SerializeDict( argVars );

						//Debug.Log( "STACKSIZE" + m.Name + " / " + mb.MaxStackSize + " / " + byteCode );
						MethodProps["maxStack"] = mb.MaxStackSize.ToString();
						MethodProps["isStatic"] = m.IsStatic ? "1" : "0";

						metadatas[methodName] = m.MetadataToken.ToString(); // There's also MethodHandle
						methods[methodName] = CilboxUtil.SerializeDict( MethodProps );
					}
				}

				OrderedDictionary staticFields = new OrderedDictionary();
				FieldInfo[] fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static );
				foreach( var f in fi )
				{
					//Debug.Log( $"Found static field {f.Name} of type {f.FieldType.FullName}" );
					staticFields[f.Name] = f.FieldType.FullName;
					metadatas[f.Name] = f.MetadataToken.ToString();
				}

				OrderedDictionary instanceFields = new OrderedDictionary();
				fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				foreach( var f in fi )
				{
					//Debug.Log( $"Found field {f.Name} of type {f.FieldType}" );
					instanceFields[f.Name] = f.FieldType.FullName;
					metadatas[f.Name] = f.MetadataToken.ToString();
				}

				OrderedDictionary classProps = new OrderedDictionary();
				classProps["methods"] = CilboxUtil.SerializeDict( methods );
				classProps["metadatas"] = CilboxUtil.SerializeDict( metadatas );
				classProps["staticFields"] = CilboxUtil.SerializeDict( staticFields );
				classProps["instanceFields"] = CilboxUtil.SerializeDict( instanceFields );
				classes[type.FullName] = CilboxUtil.SerializeDict( classProps );
			}

			String sAllClassData = CilboxUtil.SerializeDict( classes );

			/* Test
			Debug.Log( CilboxUtil.SerializeDict( classes ) );
			OrderedDictionary classTest = CilboxUtil.DeserializeDict( sAllClassData );
			foreach( DictionaryEntry v in classTest )
			{
				Debug.Log( "CLASS" + v.Key + "=" + v.Value );
				OrderedDictionary testClassProps = CilboxUtil.DeserializeDict( v.Value );
				foreach( DictionaryEntry ve in testClassProps )
				{
					Debug.Log( "PROPS" + ve.Key + "=" + ve.Value );
					OrderedDictionary testClassPropsMethods = CilboxUtil.DeserializeDict( ve.Value );
					foreach( DictionaryEntry vee in testClassPropsMethods )
					{
						Debug.Log( "METHOD" + vee.Key + "=" + vee.Value );
					}
				}
			} */

			CilboxEnvironmentHolder [] se = Resources.FindObjectsOfTypeAll(typeof(CilboxEnvironmentHolder)) as CilboxEnvironmentHolder [];
			CilboxEnvironmentHolder tac;
			if( se.Length != 0 )
			{
				tac = se[0];
			}
			else
			{
				GameObject cillyDataObject = new GameObject("CilboxData");
				cillyDataObject.hideFlags = HideFlags.HideAndDontSave;
				tac = cillyDataObject.AddComponent( typeof(CilboxEnvironmentHolder) ) as CilboxEnvironmentHolder;
			}
			tac.stringData = sAllClassData;
			//cube.transform.position = new Vector3(0.0f, 0.5f, 0.0f);

			// Iterate over all GameObjects, and find the ones that have Cilboxable scripts.
			object[] obj = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			foreach (object o in obj)
			{
				GameObject g = (GameObject) o;
				MonoBehaviour [] scripts = g.GetComponents<MonoBehaviour>();
				foreach (MonoBehaviour m in scripts )
				{
					// Not a proxiable script.
					if (m.GetType().GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0)
						continue;

					CilboxProxy p = g.AddComponent<CilboxProxy>();
					p.SetupProxy( m );
					UnityEngine.Object.DestroyImmediate( m );
				}
			}
		}
	}
	#endif









	public static class CilboxUtil
	{
		static public object FillPossibleSystemType( Type t )
		{
			if( t == typeof(System.Int32) )
				return new System.Int32();
			else
				return null;
		}

		static public int IntFromHexChar( char c )
		{
			if( c >= '0' && c <= '9' )
				return c - '0';
			else if( c >= 'a' & c <= 'f' )
				return c - 'a' + 10;
			else
				return -1;
		}

		static public String HexFromNum( int n )
		{
			char c = (char) ( '0' + (n & 0xf));
			if( c > '9' ) c = (char)( c + 'a' - '9' - 1);
			return "" + c;
		}
		static public String Escape( String s )
		{
			String ret = "";
			foreach( char c in s )
			{
				if( c == '\\' )
					ret += "\\\\";
				else if( c == '\t' )
					ret += "\\t";
				else if( c == '\n' )
					ret += "\\n";
				else if( c > 0x7f || c < 20 )
					ret += "\\" + HexFromNum( c>>12 ) + HexFromNum( c >> 8 ) + HexFromNum( c >> 4 ) + HexFromNum( c >> 0 );
				else
					ret += c;
			}
			return ret;
		}
		// \tkey\tvalue\tkey\tvalue\tkey\tvalue\n
		static public String ParseString( String s, ref int pos, ref int poserror )
		{
			String ret = "";
			int hexmode = 0;
			int hexchar = 0;
			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( hexmode != 0 )
				{
					//Debug.Log( "TEX: " + (int)c + "/" + hexmode );
					if( c == '\\' && hexmode == 1 )
					{
						ret += (char)'\\';
						hexchar = 0; hexmode = 0;
					}
					else if( c == 't' && hexmode == 1 )
					{
						ret += (char)'\t';
						hexchar = 0; hexmode = 0;
					}
					else if( c == 'n' && hexmode == 1 )
					{
						ret += (char)'\n';
						hexchar = 0; hexmode = 0;
					}
					else
					{
						hexchar = hexchar << 4;
						int v = IntFromHexChar( c );
						if( v < 0 ) break;
						hexchar |= v;
						hexmode++;
						if( hexmode == 4 )
						{
							ret += (char)hexchar;
							hexchar = 0;
							hexmode = 0;
						}
					}
				}
				else
				{
					//Debug.Log( c + " " + (c == '\t' || c == '\n') );
					if( c == '\\' )
						hexmode = 1;
					else if( c == '\t' || c == '\n' )
						break;
					else
						ret += c;
				}
			}
			if( hexmode != 0 )
			{
				poserror = pos;
			}
			//Debug.Log( "PAX: " + poserror + " / " + hexmode );
			return ret;
		}
		static public String SerializeDict( OrderedDictionary dict )
		{
			String ret = "";
			foreach( DictionaryEntry s in dict )
			{
				ret += "\t" + Escape((String)s.Key) + "\t" + Escape((String)s.Value);
			}
			return ret + "\n";
		}

		/*
		// Turns out we didn't need this yet.
		static public String [] DeserializeArray( String s )
		{
			int poserror = -1;
			int pos = 0;
			List< String > ret = new List< String >();
			poserror = -1;

			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( c == '\n' ) break;
				if( c == '\t' ) continue;
				pos--;
				ret.Add( ParseString( s, ref pos, ref poserror ) );
				if( poserror >= 0 )
					break;
			}
			if( poserror >= 0 )
			{
				Debug.LogError( $"Erorr parsing dictionary at char {poserror}\n{s}" );
			}
			return ret.ToArray();
		}*/
		static public OrderedDictionary DeserializeDict( String s )
		{
			int poserror = -1;
			int pos = 0;
			OrderedDictionary ret = new OrderedDictionary();
			int mode = 0;
			String key = "";
			poserror = -1;

			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( mode == 0 )
				{
					//Debug.Log( "NEXT" + (int)c );
					if( c == '\n' ) { pos++; break; }
					else if( c == '\t' ) { /* OK */ }
					else { poserror = pos; break; }
				}
				else if( mode == 1 )
				{
					key = ParseString( s, ref pos, ref poserror );
					pos--;
				}
				else if( mode == 2 )
				{
					if( c != '\t' ) { poserror = pos; break; }
				}
				else if( mode == 3 )
				{
					ret[key] = ParseString( s, ref pos, ref poserror );
					pos--;
				}
				mode = (mode+1) % 4;
				if( poserror >= 0 )
					break;
			}
			if( poserror >= 0 )
			{
				Debug.LogError( $"Erorr parsing dictionary at char {poserror}\n{s}" );
			}
			return ret;
		}
	}

	public enum ImportFunctionID
	{
		dotCtor, // Must be at index 0.
		Update,
		Start,
		Awake,
	}
}

