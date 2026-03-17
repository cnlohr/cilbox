using System;
using Cilbox;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using UnityEngine.SceneManagement;


namespace TestCilbox
{
	[CilboxTarget]
	public class CilboxTester : Cilbox.Cilbox
	{
		public override long MaxTimeoutLengthUs => 2000000; // 2 seconds.

		static HashSet<String> whiteListType = new HashSet<String>(){
			"Cilbox.CilboxPublicUtils",
			"TestCilbox.DisposeTester",
			"TestCilbox.Validator",
			"TestCilbox.TestEnum",
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
			"TestCilbox.TestUtil.StaticFloat",
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

		public static bool ValidatePositiveLong( String key )
		{
			string val;
			if( TestOutput.TryGetValue( key, out val ) &&
				long.TryParse( val, out long parsed ) &&
				parsed > 0 )
			{
				Console.WriteLine( $"✅ {key} = {parsed} (> 0)" );
				return true;
			}

			Console.WriteLine( $"❌ {key} is unset or not > 0" );
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


	public enum TestEnum
	{
		FirstValue,
		SecondValue,
		ThirdValue = 30,
	}


	public class TestUtil
	{
		public static float StaticFloat = 5.0f;

		public static void Increment(ref float val) { val += 1.0f; }

		public static bool TestEnumNativeEquals(TestEnum a, TestEnum b)
		{
			return a == b;
		}

		public static void GetOutVec3(out Vector3 v)
		{
			v = new Vector3(12, 8, 0);
		}

		public static void GetOutInt(out int i)
		{
			i = 42;
		}
	}


	public class Program
	{
		private const long PerfTimeoutUs = 120000000;

		private static void InvokeProxyMethod(Cilbox.CilboxProxy proxy, string methodName)
		{
			proxy.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes).Invoke(proxy, new object[0]);
		}

		private static void PrintPerfSummary()
		{
			string rootClass = PerfRootBehaviour.ClassName;
			string peerClass = PerfPeerBehaviour.ClassName;
			Console.WriteLine($"PERF class={rootClass} total_us={Validator.Get($"Perf.{rootClass}.TotalUs")}");
			Console.WriteLine($"PERF class={peerClass} total_us={Validator.Get($"Perf.{peerClass}.TotalUs")}");

			string[] taskKeys = new string[]
			{
				$"Perf.{rootClass}.RecursiveUs",
				$"Perf.{rootClass}.FourierUs",
				$"Perf.{rootClass}.TrigUs",
				$"Perf.{rootClass}.MatrixUs",
				$"Perf.{rootClass}.PeerCallsUs",
			};
			foreach( string key in taskKeys )
			{
				Console.WriteLine($"PERF metric={key} value={Validator.Get(key)}");
			}
		}

		private static void RunPerfSuite(Cilbox.Cilbox cb, Cilbox.CilboxProxy perfRootProxy, Cilbox.CilboxProxy perfPeerProxy)
		{
			cb.disabled = false;
			cb.timeoutLengthUs = PerfTimeoutUs;

			Validator.Set("PerfRunStatus", "failed");
			InvokeProxyMethod(perfPeerProxy, "Awake");
			InvokeProxyMethod(perfPeerProxy, "Start");
			InvokeProxyMethod(perfRootProxy, "Awake");
			InvokeProxyMethod(perfRootProxy, "Start");
			Validator.Set("PerfRunStatus", "complete");

			string rootClass = PerfRootBehaviour.ClassName;
			string peerClass = PerfPeerBehaviour.ClassName;
			Validator.Validate("PerfRunStatus", "complete");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.RecursiveUs");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.FourierUs");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.TrigUs");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.MatrixUs");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.PeerCallsUs");
			Validator.ValidatePositiveLong($"Perf.{rootClass}.TotalUs");
			Validator.ValidatePositiveLong($"Perf.{peerClass}.TotalUs");

			PrintPerfSummary();
		}

		public static int Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			bool runPerf = false;
			foreach( string arg in args )
			{
				if( arg == "--perf" )
				{
					runPerf = true;
					break;
				}
			}

			Cilbox.Cilbox.OnCilboxDisabled += (Cilbox.Cilbox box, string reason) =>
			{
				Validator.AddCount($"CilboxDisabled_{box.GetType().FullName}");
			};

			GameObject go = new GameObject("MyObjectToProxy");
			TestCilboxBehaviour b = go.CreateComponent<TestCilboxBehaviour>();

			GameObject go2 = new GameObject("MyObjectToProxy2");
			TestCilboxBehaviour2 b2 = go.CreateComponent<TestCilboxBehaviour2>();

			b.behaviour2 = b2;
			b2.pubsettee = 12345;

			GameObject perfRootGo = null;
			GameObject perfPeerGo = null;
			if( runPerf )
			{
				perfRootGo = new GameObject("PerfRootToProxy");
				perfPeerGo = new GameObject("PerfPeerToProxy");
				PerfRootBehaviour perfRoot = perfRootGo.CreateComponent<PerfRootBehaviour>();
				PerfPeerBehaviour perfPeer = perfPeerGo.CreateComponent<PerfPeerBehaviour>();
				perfRoot.peer = perfPeer;
			}

			GameObject cbobj = new GameObject("BasicCilbox");
			Cilbox.Cilbox cb = cbobj.AddComponent<CilboxTester>();
			cb.exportDebuggingData = true;

			// let the CI take its time running Start()
			cb.timeoutLengthUs = 200000; // 200ms
			Cilbox.CilboxScenePostprocessor.OnPostprocessScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene() );
			Application.CallBeforeRender();

			Thread.Sleep(50); // Give assembly time to write out.

			Cilbox.CilboxProxy proxy = go.GetComponents<Cilbox.CilboxProxy>()[0];
			Cilbox.CilboxProxy perfRootProxy = null;
			Cilbox.CilboxProxy perfPeerProxy = null;
			if( runPerf )
			{
				perfRootProxy = perfRootGo.GetComponents<Cilbox.CilboxProxy>()[0];
				perfPeerProxy = perfPeerGo.GetComponents<Cilbox.CilboxProxy>()[0];
			}

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
				Validator.Validate( "private static field x2", "1114" );
				Validator.Validate( "public static field", "558" );

				Validator.Validate( "Method Called On Peer", "OK" );
				Validator.Validate( "Public Field Change In Editor", "12345" );

				Validator.Validate( "recursive function", "511" );
				Validator.Validate( "string concatenation", "it works" );
				Validator.Validate( "MathF.Sin", "-0.058374193" );

				// Make sure CI can fail.
				//Validator.Validate( "Test Fail Check", "This will fail" );
			}
			catch( Exception e )
			{
				Validator.Validate( e.ToString(), "Should be no error." );
			}

			try
			{
				// Ensure 50ms timeout for the Update test.
				cb.timeoutLengthUs = 50000; // 50ms
				// In case assembly is still being generated.
				proxy.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes).Invoke( proxy, new object[0] );
			} catch( TargetInvocationException e )
			{
				if (e.InnerException is not CilboxInterpreterTimeoutException)
				{
					throw;
				}
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

			cb.timeoutLengthUs = 3000000; // should be over max
			Validator.Set("Real timeoutLengthUs", cb.timeoutLengthUs.ToString() );
			Validator.Validate("Real timeoutLengthUs", cb.MaxTimeoutLengthUs.ToString() );

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

			Validator.Validate("Vector3CheckThis", "OK");

			// MyEnum (Cilboxable enum) constants and field tests
			Validator.Validate("MyEnum.Value1", "Value1");
			Validator.Validate("MyEnum.Value2", "Value2");
			Validator.Validate("MyEnum.Value3", "Value3");
			Validator.Validate("(int)MyEnum.Value1", "0");
			Validator.Validate("(int)MyEnum.Value2", "1");
			Validator.Validate("(int)MyEnum.Value3", "30");
			Validator.Validate("MyEnum Field", "Value2");
			Validator.Validate("(int)MyEnum Field", "1");
			Validator.Validate("MyEnum Field == Value1", "False");
			Validator.Validate("MyEnum Field == Value2", "True");
			Validator.Validate("MyEnum Field == Value3", "False");
			Validator.Validate("(int)MyEnum Field == Value1", "False");
			Validator.Validate("(int)MyEnum Field == Value2", "True");
			Validator.Validate("(int)MyEnum Field == Value3", "False");

			// TestEnum (non-Cilboxable enum) constants and field tests
			Validator.Validate("TestEnum.FirstValue", "FirstValue");
			Validator.Validate("TestEnum.SecondValue", "SecondValue");
			Validator.Validate("TestEnum.ThirdValue", "ThirdValue");
			Validator.Validate("(int)TestEnum.FirstValue", "0");
			Validator.Validate("(int)TestEnum.SecondValue", "1");
			Validator.Validate("(int)TestEnum.ThirdValue", "30");
			Validator.Validate("TestEnum Field", "SecondValue");
			Validator.Validate("(int)TestEnum Field", "1");
			Validator.Validate("TestEnum Field == FirstValue", "False");
			Validator.Validate("TestEnum Field == SecondValue", "True");
			Validator.Validate("TestEnum Field == ThirdValue", "False");
			Validator.Validate("(int)TestEnum Field == FirstValue", "False");
			Validator.Validate("(int)TestEnum Field == SecondValue", "True");
			Validator.Validate("(int)TestEnum Field == ThirdValue", "False");

			// Native method calls with enum parameters
			Validator.Validate("TestEnumNativeEqualsFirstValue", "False");
			Validator.Validate("TestEnumNativeEqualsSecondValue", "True");
			Validator.Validate("TestEnumNativeEqualsThirdValue", "False");

			// Private nested enum with byte backing type
			Validator.Validate("TestState.Stopped", "Stopped");
			Validator.Validate("TestState.Playing", "Playing");
			Validator.Validate("TestState.Paused", "Paused");
			Validator.Validate("(byte)TestState.Stopped", "0");
			Validator.Validate("(byte)TestState.Playing", "1");
			Validator.Validate("(byte)TestState.Paused", "2");
			Validator.Validate("TestState Field", "Playing");
			Validator.Validate("(byte)TestState Field", "1");
			Validator.Validate("TestState Field == Stopped", "False");
			Validator.Validate("TestState Field == Playing", "True");
			Validator.Validate("TestState Field == Paused", "False");
			Validator.Validate("(byte)TestState Field == Stopped", "False");
			Validator.Validate("(byte)TestState Field == Playing", "True");
			Validator.Validate("(byte)TestState Field == Paused", "False");

			// Enum method calls (MyEnum is Cilboxable)
			Validator.ValidateCount("MyEnumMethod", 2);
			Validator.Validate("MyEnumMethod_1", "Value1");
			Validator.Validate("MyEnumMethod_2", "Value2");

			// Enum method calls (TestEnum is non-Cilboxable, ToString shows enum name)
			Validator.ValidateCount("TestEnumMethod", 2);
			Validator.Validate("TestEnumMethod_1", "FirstValue");
			Validator.Validate("TestEnumMethod_2", "SecondValue");

			Validator.ValidateCount("TestStateMethod", 2);
			Validator.Validate("TestStateMethod_1", "Stopped");
			Validator.Validate("TestStateMethod_2", "Playing");

			// MyEnum array (Cilboxable)
			Validator.Validate("MyEnum Array 0", "Value1");
			Validator.Validate("MyEnum Array int value 0", "0");
			Validator.Validate("MyEnum Array 1", "Value2");
			Validator.Validate("MyEnum Array int value 1", "1");
			Validator.Validate("MyEnum Array 2", "Value3");
			Validator.Validate("MyEnum Array int value 2", "30");

			// TestEnum array (non-Cilboxable, ToString shows enum name)
			Validator.Validate("TestEnum Array 0", "FirstValue");
			Validator.Validate("TestEnum Array int value 0", "0");
			Validator.Validate("TestEnum Array 1", "SecondValue");
			Validator.Validate("TestEnum Array int value 1", "1");
			Validator.Validate("TestEnum Array 2", "ThirdValue");
			Validator.Validate("TestEnum Array int value 2", "30");

			Validator.Validate("TestState Array 0", "Stopped");
			Validator.Validate("TestState Array byte value 0", "0");
			Validator.Validate("TestState Array 1", "Playing");
			Validator.Validate("TestState Array byte value 1", "1");
			Validator.Validate("TestState Array 2", "Paused");
			Validator.Validate("TestState Array byte value 2", "2");

			// Boxing enums
			Validator.Validate("Boxed MyEnum", "Value2");
			Validator.Validate("Boxed TestEnum", "SecondValue");

			Validator.Validate("NativeStaticFloat", "5");
			Validator.Validate("NativeStaticFloat x2", "10");
			Validator.Validate("ReadFloat_2", "10");
			Validator.Validate("WriteFloat_2", "99");
			Validator.Validate("NativeStaticFloat ref written", "99");
			Validator.Validate("ReadInt_2", "1114");

			Validator.Validate("NativeOutVec3", "(12, 8, 0)");
			Validator.Validate("CilOutVec3", "(1, 2, 3)");
			Validator.Validate("NativeOutInt", "42");
			Validator.Validate("CilOutInt", "22");
			Validator.Validate("NativeOutVec3AlreadyInit", "(12, 8, 0)");
			Validator.Validate("myBehaviour3Arr Length", "2");
			Validator.Validate("myBehaviour3Arr 0", "123");
			Validator.Validate("myBehaviour3Arr 1", "456");
			Validator.Validate("myBehaviour3Arr 1 changed", "789");

			Validator.Validate("ThrowFromOtherBehaviour1", "caught");
			Validator.Validate("ThrowFromOtherBehaviour2", "caught");
			Validator.Validate("ThrowFromOtherBehaviour2Finally", "finally");
			Validator.Validate("ThrowFromOtherConstructor", "caught");

			Validator.ValidateCount($"CilboxDisabled_{cb.GetType().FullName}", 1 );

			if( runPerf )
			{
				RunPerfSuite(cb, perfRootProxy, perfPeerProxy);
			}

			return -1 * Validator.NumValidationErrors();
		}
	}
}

