using UnityEngine;
using Cilbox;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using TestCilbox;

namespace TestCilbox
{
	[Cilboxable]
	public class TestCilboxBehaviour : MonoBehaviour
	{
		public TestCilboxBehaviour() { }

		public void Start()
		{
			Debug.Log( "🔵TestCilboxBehaviour.Start()" );

			Validator.Set( "Start Test", "OK" );

			int i = (int)(((int)(5+2/3)+50)*2/7.3);
			Validator.Set( "Arithmatic Test", i.ToString() );

			String startMarks = Validator.Get("Start Marks");
			if( startMarks == null ) startMarks = "";
			Validator.Set( "Start Marks", startMarks + "I" );
		}
	}
}

