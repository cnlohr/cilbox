using UnityEngine;
using System.Collections.Generic;
using System;

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

	public class CilboxClass
	{
		public CilboxClass( String className, String classData )
		{
			this.className = className;
			Dictionary< String, String > classProps = CilboxUtil.DeserializeDict( classData );
			metadatas = new Dictionary< String, int >();
			foreach( var k in CilboxUtil.DeserializeDict( classProps["metadatas"] ) )
			{
				metadatas[k.Key] = Convert.ToInt32( k.Value );
			}

			int id = 0;
			Dictionary< String, String > staticFields = CilboxUtil.DeserializeDict( classProps["staticFields"] );
			int sfnum = staticFields.Count;
			staticObjects = new object[sfnum];
			staticFieldNames = new String[sfnum];
			staticFieldTypes = new Type[sfnum];
			staticFieldIDs = new int[sfnum];
			foreach( var k in staticFields )
			{
				staticFieldNames[id] = k.Key;
				staticFieldTypes[id] = Type.GetType( k.Value );
				staticFieldIDs[id] = metadatas[k.Key];
				id++;
			}

			Dictionary< String, String > instanceFields = CilboxUtil.DeserializeDict( classProps["instanceFields"] );
			int ifnum = instanceFields.Count;
			instanceFieldNames = new String[ifnum];
			instanceFieldTypes = new Type[ifnum];
			staticFieldIDs = new int[ifnum];
			id = 0;
			foreach( var k in instanceFields )
			{
				instanceFieldNames[id] = k.Key;
				instanceFieldTypes[id] = Type.GetType( k.Value );
				staticFieldIDs[id] = metadatas[k.Key];
				id++;
			}

			id = 0;
			Dictionary< String, String > methods = CilboxUtil.DeserializeDict( classProps["methods"] );
			int mnum = methods.Count;
			methodNameToIndex = new Dictionary< String, int >();
			methodNames = new String[mnum];
			methodData = new byte[mnum][];
			foreach( var k in methods )
			{
				methodNames[id] = k.Key;
				int bl = k.Value.Length/2;
				methodData[id] = new byte[bl];
				for( int i = 0; i < bl; i++ )
				{
					int v = CilboxUtil.IntFromHexChar( k.Value[i*2+0] );
					if( v < 0 ) break;
					byte b = (byte)v;
					v = CilboxUtil.IntFromHexChar( k.Value[i*2+0] );
					if( v < 0 ) break;
					methodData[id][i] = (byte)(v | (b<<4));
				}
				methodNameToIndex[k.Key] = id;
				id++;
			}

			int numImportFunctions = Enum.GetNames(typeof(ImportFunctionID)).Length;
			importFunctionToId = new int[numImportFunctions];
			for( int i = 0; i < numImportFunctions; i++ )
			{
				String fn = Enum.GetName(typeof(ImportFunctionID), i);
				importFunctionToId[i] = -1;
				methodNameToIndex.TryGetValue(fn, out importFunctionToId[i]);
			}

			// Go back and fixup the first.
			methodNameToIndex.TryGetValue( ".ctor", out importFunctionToId[(int)ImportFunctionID.dotCtor] );
			
			Debug.Log( classProps["metadatas"] );
		}

		public String className;

		public object[] staticObjects;
		public String[] staticFieldNames;
		public Type[] staticFieldTypes;
		public int[] staticFieldIDs;

		public String[] instanceFieldNames;
		public Type[] instanceFieldTypes;
		public int[] instanceFieldIDs;

		// Conversion from name to metadata id.
		public Dictionary< String, int > metadatas;

		public Dictionary< String, int > methodNameToIndex;
		public String[] methodNames;
		public byte[][] methodData;
		public int[]    importFunctionToId; // from ImportFunctionID
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

			Dictionary< String, String> classData = CilboxUtil.DeserializeDict( se[0].stringData );

			foreach( var v in classData )
			{
				classes[v.Key] = new CilboxClass( v.Key, v.Value );
			}
		}

		public static CilboxClass GetClass( String className )
		{
			if( className == null ) return null;
			CilboxClass ret;
			if( classes.TryGetValue(className, out ret)) return ret;
			return null;
		}

		public static object Interpret( CilboxClass cls, object ths, ImportFunctionID iid, object [] parameters )
		{
			if( cls == null ) return null;

			int index = cls.importFunctionToId[(int)iid];

			if( index < 0 ) return null;

			byte [] bytecode = cls.methodData[index];

			if( bytecode == null || bytecode.Length == 0 ) return null;

			Debug.Log( bytecode );
			// Do the magic.

			return null;
		}
	}

	#if UNITY_EDITOR
	public class CilboxScenePostprocessor {

		[PostProcessSceneAttribute (2)]
		public static void OnPostprocessScene() {
			Debug.Log( "Postprocessing scene." );

			Assembly mscorlib = typeof(CilboxProxy).Assembly;

			Dictionary < String, String > classes = new Dictionary< String, String >();
			foreach (Type type in mscorlib.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				Dictionary < String, String > metadatas = new Dictionary< String, String> ();

				Dictionary < String, String > methods = new Dictionary< String, String> ();
				MethodInfo[] me = type.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				foreach( var m in me )
				{
					String methodName = m.Name;
					Dictionary< String, String > MethodProps = new Dictionary< String, String >();

					MethodBody mb = m.GetMethodBody();
					if( mb == null )
					{
						//Debug.Log( $"NOTE: {m.Name} does not have a body" );
						// Things like MemberwiseClone, etc.
						continue;
					}

					Dictionary< String, String > localVars = new Dictionary< String, String >();
					foreach (LocalVariableInfo lvi in mb.LocalVariables)
						localVars[lvi.ToString()] = lvi.GetType().ToString();
					MethodProps["locals"] = CilboxUtil.SerializeDict( localVars );

					String byteCode = "";
					byte [] ba = mb.GetILAsByteArray();
					for( int i = 0; i < ba.Length; i++ )
					{
						int b = ba[i];
						byteCode += CilboxUtil.HexFromNum( b>>4 ) + CilboxUtil.HexFromNum( b&0xf );
					}
					MethodProps["body"] = byteCode;
					metadatas[methodName] = m.MetadataToken.ToString(); // There's also MethodHandle
					methods[methodName] = CilboxUtil.SerializeDict( MethodProps );
				}

				Dictionary < String, String > staticFields = new Dictionary< String, String> ();
				FieldInfo[] fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static );
				foreach( var f in fi )
				{
					Debug.Log( $"Found static field {f.Name} of type {f.FieldType.FullName}" );
					staticFields[f.Name] = f.FieldType.FullName;
					metadatas[f.Name] = f.MetadataToken.ToString();
				}

				Dictionary < String, String > instanceFields = new Dictionary< String, String> ();
				fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				foreach( var f in fi )
				{
					//Debug.Log( $"Found field {f.Name} of type {f.FieldType}" );
					instanceFields[f.Name] = f.FieldType.FullName;
					metadatas[f.Name] = f.MetadataToken.ToString();
				}

				Dictionary < String, String > classProps = new Dictionary< String, String> ();
				classProps["methods"] = CilboxUtil.SerializeDict( methods );
				classProps["metadatas"] = CilboxUtil.SerializeDict( metadatas );
				classProps["staticFields"] = CilboxUtil.SerializeDict( staticFields );
				classProps["instanceFields"] = CilboxUtil.SerializeDict( instanceFields );
				classes[type.FullName] = CilboxUtil.SerializeDict( classProps );
			}

			String sAllClassData = CilboxUtil.SerializeDict( classes );

			/* Test
			Debug.Log( CilboxUtil.SerializeDict( classes ) );
			Dictionary< String, String > classTest = CilboxUtil.DeserializeDict( sAllClassData );
			foreach( var v in classTest )
			{
				Debug.Log( "CLASS" + v.Key + "=" + v.Value );
				Dictionary< String, String > testClassProps = CilboxUtil.DeserializeDict( v.Value );
				foreach( var ve in testClassProps )
				{
					Debug.Log( "PROPS" + ve.Key + "=" + ve.Value );
					Dictionary< String, String > testClassPropsMethods = CilboxUtil.DeserializeDict( ve.Value );
					foreach( var vee in testClassPropsMethods )
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
		static public String SerializeDict( Dictionary< String, String > dict )
		{
			String ret = "";
			foreach( var s in dict )
			{
				ret += "\t" + Escape(s.Key) + "\t" + Escape(s.Value);
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
		static public Dictionary < String, String > DeserializeDict( String s )
		{
			int poserror = -1;
			int pos = 0;
			Dictionary < String, String > ret = new Dictionary< String, String >();
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

