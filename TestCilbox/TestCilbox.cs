using System;
using Cilbox;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;


namespace TestCilbox
{
	[CilboxTarget]
	public class CilboxTester : Cilbox.Cilbox
	{
		static HashSet<String> whiteListType = new HashSet<String>(){
			"Cilbox.CilboxPublicUtils",
			"TestCilbox.DisposeTester",
			"TestCilbox.Validator",
			"TestCilbox.TestUtil",
			"System.Math",
			"System.Array",
			"System.Boolean",
			"System.Byte",
			"System.Char",
			"System.Collections.Generic.Dictionary",
			"System.Collections.Generic.Dictionary+KeyCollection",
			"System.Collections.Generic.IEnumerable",
			"System.Double",
			"System.DateTime",
			"System.DayOfWeek",
			"System.Diagnostics.Stopwatch",
			"System.DivideByZeroException",
			"System.Exception",
			"System.IDisposable",
			"System.IndexOutOfRangeException",
			"System.Int16",
			"System.Int32",
			"System.Int64",
			"System.MathF",
			"System.NullReferenceException",
			"System.Object",
			"System.Single",
			"System.String",
			"System.TimeSpan",
			"System.UInt16",
			"System.UInt32",
			"System.ValueTuple",
			"System.Void",
			"TestCilbox.Outer+Middle+Inner",
			"UnityEngine.Component",
			"UnityEngine.Debug",
			"UnityEngine.Events.UnityAction",
			"UnityEngine.Events.UnityEvent",
			"UnityEngine.GameObject", ///////////// HMMMMMMMMMMMM
			"UnityEngine.Material",
			"UnityEngine.MaterialPropertyBlock",
			"UnityEngine.Mathf",
			"UnityEngine.MeshRenderer",
			"UnityEngine.MonoBehaviour",   ///////////// HMMMMMMMMMMMM (Note this is needed for the 'ctor, long story)
			"UnityEngine.Object",
			"UnityEngine.Random",
			"UnityEngine.Renderer",
			"UnityEngine.Time",
			"UnityEngine.Texture",
			"UnityEngine.UI.Button+ButtonClickedEvent",
			"UnityEngine.UI.Button",
			"UnityEngine.UI.InputField",
			"UnityEngine.UI.InputField+OnChangeEvent",
			"UnityEngine.UI.Scrollbar",
			"UnityEngine.UI.Selectable",
			"UnityEngine.UI.Slider",
			"UnityEngine.UI.Text",
			"UnityEngine.TextAsset",
			"UnityEngine.Texture2D",
			"UnityEngine.Transform",
			"UnityEngine.Vector4",
			"UnityEngine.Vector3",
		};

		static HashSet<String> whiteListField = new HashSet<String>(){
			"UnityEngine.Vector3.x",
			"UnityEngine.Vector3.y",
			"UnityEngine.Vector3.z",
		};

		static public HashSet<String> GetWhiteListTypes() { return whiteListType; }

		override public bool CheckTypeAllowed( String sType )
		{
			return whiteListType.Contains( sType );
		}

		public override bool CheckFieldAllowed(string sType, string sFieldName)
		{
			return whiteListField.Contains( sType + "." + sFieldName );
		}

		override public bool CheckMethodAllowed( out MethodInfo mi, Type declaringType, String name, Serializee [] parametersIn, Serializee [] genericArgumentsIn, String fullSignature )
		{
			mi = null;

			// You're allowed to get access to the constructor, nothing else.
			if( declaringType == typeof(UnityEngine.MonoBehaviour) && name != ".ctor" ) return false;
			//if( declaringType == typeof(UnityEngine.Events.UnityAction) && name != ".ctor" ) return false;
			if( name.Contains( "Invoke" ) ) return false;
			return true;
		}
	}


	public static class Validator
	{
		private static bool bDidFail = false;
		public static bool DidFail() { return bDidFail; }
		private static int numValidationErrors = 0;
		public static int NumValidationErrors() { return numValidationErrors; }
		public static Dictionary< String, String > TestOutput = new Dictionary< String, String >();
		public static Dictionary<String, int> TestCounters = new Dictionary<String, int>();
		public static void Set( String key, String val ) { TestOutput[key] = val; }
		public static String Get( String key ) { String ret = null; TestOutput.TryGetValue( key, out ret ); return ret; }
		public static void AddCount( String key )
		{
			int cur = 0;
			TestCounters.TryGetValue( key, out cur );
			cur += 1;
			TestCounters[key] = cur;
		}
		public static int GetCount( String key )
		{
			int cur = 0;
			TestCounters.TryGetValue( key, out cur );
			return cur;
		}
		public static bool Validate( String key, String comp )
		{
			String val;
			if( TestOutput.TryGetValue( key, out val ) )
			{
				if( val == comp )
				{
					Console.WriteLine( $"✅ {key} = {val} " );
					return true;
				}
				Console.WriteLine( $"❌ {key} = {val} != {comp}" );
			}
			else
			{
				Console.WriteLine( $"❌ {key} is unset (Expected {comp})" );
			}
			bDidFail = true;
			numValidationErrors++;
			return false;
		}
		public static bool ValidateCount( String key, int comp )
		{
			int val = GetCount( key );
			if( val == comp )
			{
				Console.WriteLine( $"✅ {key} count = {val} " );
				return true;
			}
			Console.WriteLine( $"❌ {key} count = {val} != {comp}" );
			bDidFail = true;
			numValidationErrors++;
			return false;
		}
	}


	public class DisposeTester : IDisposable
	{
		public DisposeTester()
		{
			Validator.Set( "Dispose", "not disposed" );
		}

		public void Dispose()
		{
			Validator.Set("Dispose", "disposed" );
		}
	}


	public class Outer<T>
	{
		public class Middle<U, V>
		{
			public class Inner<W>
			{
				public string GetTypeNames()
				{
					return typeof(T).Name + ", " + typeof(U).Name + ", " + typeof(V).Name + ", " + typeof(W).Name;
				}
			}
		}
	}


	public class TestUtil
	{
		public static void Increment(ref float val) { val += 1.0f; }
	}


	public class Program
	{
		public static int Main()
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			GameObject go = new GameObject("MyObjectToProxy");
			TestCilboxBehaviour b = go.CreateComponent<TestCilboxBehaviour>();

			GameObject go2 = new GameObject("MyObjectToProxy2");
			TestCilboxBehaviour2 b2 = go.CreateComponent<TestCilboxBehaviour2>();

			b.behaviour2 = b2;
			b2.pubsettee = 12345;

			GameObject cbobj = new GameObject("BasicCilbox");
			Cilbox.Cilbox cb = cbobj.AddComponent<CilboxTester>();
			cb.exportDebuggingData = true;

			cb.timeoutLengthUs = 50000; // 50ms
			Cilbox.CilboxScenePostprocessor.OnPostprocessScene();
			Application.CallBeforeRender();

			Thread.Sleep(50); // Give assembly time to write out.

			Cilbox.CilboxProxy proxy = go.GetComponents<Cilbox.CilboxProxy>()[0];

			try
			{
				proxy.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );
				proxy.GetType().GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );
				Validator.Validate( "Start Test", "OK" );
				Validator.Validate( "Start Marks", "I" );
				Validator.Validate( "Arithmatic Test", "15" );

				Validator.Validate( "private instance filed", "555");
				Validator.Validate( "public instance field", "556" );
				Validator.Validate( "private static field", "557" );
				Validator.Validate( "public static field", "558" );

				Validator.Validate( "Method Called On Peer", "OK" );
				Validator.Validate( "Public Field Change In Editor", "12345" );

				Validator.Validate( "recursive function", "511" );
				Validator.Validate( "string concatenation", "it works" );
				Validator.Validate( "MathF.Sin", "-0.058374193" );

				// Make sure CI can fail.
				//Validator.Validate( "Test Fail Check", "This will fail" );
			}
			catch( CilboxInterpreterTimeoutException e )
			{
				Validator.Validate( e.ToString(), "Should be no error." );
			}

			try
			{
				// In case assembly is still being generated.
				proxy.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );
			} catch( Exception e )
			{
				Debug.Log( e.ToString().Length.ToString() );
				Validator.Set( "Overtime Exception", "Thrown" );
			}
			Validator.Validate( "Overtime Exception", "Thrown" );
			Validator.Validate( "Overtime", "timed out" );
			Validator.Validate( "Update", "called" );

			Validator.Set( "Execution after timeout", "disabled" );
			proxy.GetType().GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );
			Validator.Validate( "Execution after timeout", "disabled" );

			cb.disabled = false;
			proxy.GetType().GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );

			Validator.Validate( "Manual Recover After Timeout", "recovered" );
			Validator.Validate( "FixedUpdate", "called" );

			Validator.Validate("Dispose", "disposed" );
			Validator.Validate("TryFinally", "finally");
			Validator.Validate("TryFinally2", "finally");
			Validator.Validate("Exited Dispose Tester", "yes" );
			Validator.Validate("TryCatch", "caught" );
			Validator.ValidateCount("TryFinally", 1 );
			Validator.ValidateCount("TryFinally2", 1 );

			Validator.Validate("TryFinally3", "finally");
			Validator.ValidateCount("TryFinally3", 1 );
			Validator.Validate("NullReferenceException", "caught1" );
			Validator.Validate("NullRefUnreachable", "didn't reach");
			Validator.Validate("TryFinallyNestedTest1", "finally");
			Validator.Validate("TryFinallyNestedTest2", "bottom");
			Validator.ValidateCount("TryFinallyNestedTest1", 1);
			Validator.Validate("DivideByZeroException", "caught");

			Validator.Validate("JoinFloatArrayResized", "1.5, 2.5, 3.5, 4.5");
			Validator.Validate("DictionaryKeys", "key1, key2");
			Validator.Validate("ComplexGenericType", "String, Int32, Boolean, Char");

			Validator.Validate("TestVec.x", "12");
			Validator.Validate("TestVec.y", "8");
			Validator.Validate("New myInt", "42");
			Validator.Validate("New testVec.y", "42");
			Validator.Validate("FieldAccessNullRef", "caught");
			Validator.Validate("ReadInt_1", "14");
			Validator.Validate("ReadFloat_1", "8");
			Validator.Validate("WriteInt_1", "42");
			Validator.Validate("WriteFloat_1", "42");

			Validator.Validate("NegativeIndexAccess", "caught");
			Validator.Validate("PositiveIndexAccess", "caught");

			Validator.Validate("StfldNullRef", "caught");
			Validator.Validate("LdfldaNullRef", "caught");

			// ldind/stind byte (ldind.u1 / stind.i1)
			Validator.Validate("ReadByte_1", "200");
			Validator.Validate("WriteByte_1", "42");
			Validator.Validate("New myByte", "42");

			// ldind/stind short (ldind.i2 / stind.i2)
			Validator.Validate("ReadShort_1", "1234");
			Validator.Validate("WriteShort_1", "99");
			Validator.Validate("New myShort", "99");

			// ldind/stind long (ldind.i8 / stind.i8)
			Validator.Validate("ReadLong_1", "9876543210");
			Validator.Validate("WriteLong_1", "42");
			Validator.Validate("New myLong", "42");

			// ldind/stind double (ldind.r8 / stind.r8)
			Validator.Validate("ReadDouble_1", "3.14");
			Validator.Validate("WriteDouble_1", "2.718");
			Validator.Validate("New myDouble", "2.718");

			// ldind/stind ref (ldind.ref / stind.ref)
			Validator.Validate("ReadString_1", "hello");
			Validator.Validate("WriteString_1", "world");
			Validator.Validate("New myString", "world");

			// ldind.ref / stind.ref for Cilboxable type
			Validator.Validate("ReadCilboxable", "12345");
			Validator.Validate("WriteCilboxable", "12345");
			Validator.Validate("RefCilboxable Same", "True");

			Validator.Validate("NativeRefMethodCall", "11");

			return -1 * Validator.NumValidationErrors();
		}
	}
}

