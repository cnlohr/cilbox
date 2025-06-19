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
			"TestCilbox.Validator",
			"System.Math",
			"System.Array",
			"System.Boolean",
			"System.Byte",
			"System.Char",
			"System.Collections.Generic.Dictionary",
			"System.Double",
			"System.DateTime",
			"System.DayOfWeek",
			"System.Diagnostics.Stopwatch",
			"System.Exception",
			"System.Int32",
			"System.MathF",
			"System.Object",
			"System.Single",
			"System.String",
			"System.TimeSpan",
			"System.UInt16",
			"System.UInt32",
			"System.ValueTuple",
			"System.Void",
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

		static public HashSet<String> GetWhiteListTypes() { return whiteListType; }

		override public bool CheckTypeAllowed( String sType )
		{
			return whiteListType.Contains( sType );
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
		public static Dictionary< String, String > TestOutput = new Dictionary< String, String >();
		public static void Set( String key, String val ) { TestOutput[key] = val; }
		public static String Get( String key ) { String ret = null; TestOutput.TryGetValue( key, out ret ); return ret; }
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
			return false;
		}
	}


	public class Program
	{
		public static int Main()
		{

			GameObject go = new GameObject("MyObjectToProxy");
			TestCilboxBehaviour b = go.CreateComponent<TestCilboxBehaviour>();

			GameObject go2 = new GameObject("MyObjectToProxy2");
			TestCilboxBehaviour2 b2 = go.CreateComponent<TestCilboxBehaviour2>();

			b.behaviour2 = b2;
			b2.pubsettee = 12345;

			GameObject cbobj = new GameObject("BasicCilbox");
			Cilbox.Cilbox cb = cbobj.AddComponent<CilboxTester>();
			cb.exportDebuggingData = true;

			cb.timeoutLengthUs = 25000; // 25ms
			Cilbox.CilboxScenePostprocessor.OnPostprocessScene();
			Application.CallBeforeRender();

			Thread.Sleep(50); // Give assembly time to write out.

			Cilbox.CilboxProxy proxy = go.GetComponents<Cilbox.CilboxProxy>()[0];

			try
			{
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
			catch( Exception e )
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

			if( Validator.DidFail() ) return -5;

			return 0;
		}
	}
}

