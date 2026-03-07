using UnityEngine;
using System;
using Cilbox;

[Cilboxable]
public class TestExplode : MonoBehaviour
{
	public UnityEngine.UI.Button buttonControl;
	public GameObject thisIsNull;

	void Start(){
		buttonControl.onClick.AddListener( () => CauseExplosion() );
	}

	public void CauseExplosion()
	{
		for( int i = 5; i > -1; i-- )
			Debug.Log( $"Exploding in{i} ({20/i})" );
	}
}
