using UnityEngine;
using System;
using System.Collections.Generic;

[Cilboxable]
public class TestScript2 : MonoBehaviour
{
	public int testPublic;

	public Dictionary<String,int> test;

	void Start()
	{
		test = new Dictionary<String,int>();
		test["hello"] = 5;
		test["world"] = 6;
		Debug.Log( $"{test["hello"]} {test["world"]}" );
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
				0.5f, Mathf.Sin( (float)testPublic ), 1 ) );
		mr.SetPropertyBlock(block);
		//Debug.Log( testPublic );
	}

}
