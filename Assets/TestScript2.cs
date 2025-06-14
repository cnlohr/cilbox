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

	void Start()
	{
		test = new Dictionary<String,int>();
		test["hello"] = 5;
		test["world"] = 6;
		Debug.Log( $"{test["hello"]} {test["world"]}" );
		Debug.Log($"Should be 0.5 {datafloat[2]}"); // 0.5
		Debug.Log($"Should be 1056964608 {dataint[2]}"); // 1056964608
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
