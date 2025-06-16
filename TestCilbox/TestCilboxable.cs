using UnityEngine;
using Cilbox;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace TestCilbox
{
	[Cilboxable]
	public class TestCilboxBehaviour : MonoBehaviour
	{
		public TestCilboxBehaviour() { }

		public void Start()
		{
			Debug.Log( "CILBOX DID START!" );
		}
	}
}

