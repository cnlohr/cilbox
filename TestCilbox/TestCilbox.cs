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
			"System.Array",
			"System.Boolean",
			"System.Byte",
			"System.Char",
			"System.Collections.Generic.Dictionary",
			"System.DateTime",
			"System.DayOfWeek",
			"System.Diagnostics.Stopwatch",
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


	public class Program
	{
		public static void Main()
		{
			GameObject go = new GameObject("MyObjectToProxy");
			TestCilboxBehaviour b = go.CreateComponent<TestCilboxBehaviour>();

			GameObject cbobj = new GameObject("BasicCilbox");
			Cilbox.Cilbox cb = cbobj.AddComponent<CilboxTester>();

			Debug.Log(  typeof(Cilbox.CilboxProxy).GetConstructor(new Type[]{}).ToString() );
			Console.WriteLine( "Start" );
			Cilbox.CilboxScenePostprocessor.OnPostprocessScene();
			Console.WriteLine( "Converting" );
			Application.CallBeforeRender();
			Console.WriteLine( "Done" );
		}
	}
}

