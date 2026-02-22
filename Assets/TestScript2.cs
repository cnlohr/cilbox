using UnityEngine;
using System;
using System.Collections.Generic;

[Cilboxable]
public class TestScript2 : MonoBehaviour
{
	public int testPublic;

	public int reverseFrameNo;

	public Dictionary<String,int> test;

	public float[] datafloat = { 0.125f, 0.25f, 0.5f, 1.0f };
	public int[] dataint = {0x3e000000, 0x3e800000, 0x3f000000, 0x3f800000};

	public GameObject [] objs;

	public Vector3 testVector = new Vector3();

	void Start()
	{
		if( testPublic != 4 ) Debug.LogError($"ERR1: testPublic {testPublic}");
		test = new Dictionary<String,int>();
		test["hello"] = 5;
		test["world"] = 6;
		if( test["hello"] != 5 || test["world"] != 6 ) Debug.LogError($"ERR0: {test["hello"]} {test["world"]}" );
		if( datafloat[2]!= 0.75 ) Debug.LogError($"ERR2: Should be 0.75 {datafloat[2]} (Changed by parameters)"); // 0.5 originally
		if( datafloat[1]!= 0.25 ) Debug.LogError($"ERR3: Should be 0.25 {datafloat[1]} (Changed by constructor)");
		if( dataint[2] != 1056964608 ) Debug.LogError($"ERR4: Should be 1056964608 {dataint[2]}"); // 1056964608
		if( objs != null )
		{
			if( objs.Length > 1 )
			{
				if( objs[0] != null )
					Debug.LogError($"OBJECT CHECK BAD {objs[0]}");
				if( objs[1] == null )
					Debug.LogError($"Did you unset objs[2]?");

				Component [] comps = objs[1].GetComponents<Component>();
				int i;
				bool bFound = false;
				for( i = 0; i < comps.Length; i++ )
				{
					if( comps[i] is TestScript )
						bFound = true;
				}
				if( !bFound )
				{
					Debug.LogError("Expected at least one component in objs to be a TestScript");
				}
				//TestScript ts = (TestScript)objs[2].GetComponent(typeof(TestScript));

				//if( ts == null )
				//	Debug.LogError( "ts should not be null" );
			}
			else
				Debug.LogError( "Objs too small" );
		}
		else
			Debug.LogError( "objs is null!" );

		testVector.x = 5;
		testVector.y++;
		if( testVector.z != 9 )
		{
			Debug.LogError( "testVector.z should be 9\n" );
		}
	}

	public void AnotherThing()
	{
		//Debug.Log( "AnotherThing()" );
		testPublic++;
	}

    void Update() {
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		MeshRenderer mr = GetComponent<MeshRenderer>();
		block.SetVector( "_Color",
			new Vector4(
				Mathf.Sin( (float)Time.time * 5.0f )*0.5f+0.5f,
				testVector.y++, Mathf.Sin( (float)testPublic*0.01f ), 1 ) );
		mr.SetPropertyBlock(block);

#if false
		// This does work...
		//  ... but at what cost?
		Component [] comps = objs[1].GetComponents<Component>();
		foreach( Component c in comps )
		{
			if( c is TestScript )
				reverseFrameNo = ((TestScript)c).framenoPublic;
		}
#endif

// OK
//		TestScript ts = objs[1].GetComponent<TestScript>();
//		if( ts )
//			reverseFrameNo = ((TestScript)ts).framenoPublic;

// OK
//		Transform tr = objs[1].GetComponent<Transform>();
//		if( tr )
//			Debug.Log( tr );

/*
		// TODO: Make this method work.
		TestScript ts = (TestScript)objs[1].GetComponent(typeof(TestScript));
		if( ts )
			Debug.Log( $"Reverse check {ts.framenoPublic }" );
*/


		// TODO: Make this method work.
		TestScript ts;
		if( objs[1].TryGetComponent<TestScript>( out ts ) )
		{
			reverseFrameNo = ((TestScript)ts).framenoPublic;
		}

// OK
//		Transform tr;
//		if( objs[1].TryGetComponent<Transform>( out tr ) )
//		{
//			Debug.Log( tr );
//		}
		

	}

}
