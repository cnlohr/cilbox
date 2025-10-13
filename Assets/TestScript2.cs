using UnityEngine;
using System;
using System.Collections.Generic;

[Cilboxable]
public class TestScript2 : MonoBehaviour
{
	public int testPublic;

	public Dictionary<String,int> test;

	public float[] datafloat = { 0.125f, 0.25f, 0.5f, 1.0f };
	public int[] dataint = {0x3e000000, 0x3e800000, 0x3f000000, 0x3f800000};

	public GameObject [] objs;

	void Start()
	{
		if( testPublic != 4 ) Debug.LogError($"testPublic {testPublic}");
		test = new Dictionary<String,int>();
		test["hello"] = 5;
		test["world"] = 6;
		if( test["hello"] != 5 || test["world"] != 6 ) Debug.LogError($"{test["hello"]} {test["world"]}" );
		if( datafloat[2]!= 0.75 ) Debug.LogError($"Should be 0.75 {datafloat[2]} (Changed by parameters)"); // 0.5 originally
		if( datafloat[1]!= 0.25 ) Debug.LogError($"Should be 0.25 {datafloat[1]} (Changed by constructor)");
		if( dataint[2] != 1056964608 ) Debug.LogError($"Should be 1056964608 {dataint[2]}"); // 1056964608
		if( objs != null )
		{
			if( objs.Length > 1 )
			{
				if( objs[0] != null )
					Debug.LogError($"OBJECT CHECK BAD {objs[0]}");
			}
			else
				Debug.LogError( "Objs too small" );
		}
		else
			Debug.LogError( "objs is null!" );
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
				0.5f, Mathf.Sin( (float)testPublic*0.01f ), 1 ) );
		mr.SetPropertyBlock(block);
		//Debug.Log( testPublic );
	}

}
