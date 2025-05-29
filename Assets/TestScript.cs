using UnityEngine;
using System;

[Cilboxable]
public class TestScript : MonoBehaviour
{
	public int framenoPublic;
	private int framenoPrivate;
	[SerializeField] private int framenoPrivateButSerialize;
	static public int framenoPublicStatic;
	static private int framenoPrivateStatic;
	TestScript() { framenoPrivate = 10; framenoPrivateStatic = 14; framenoPrivateStatic = 32; }
    void Start(){ framenoPublic++; framenoPrivate+=2; Debug.Log( $"XXX TestScript Start() {framenoPublic} {framenoPrivate} {framenoPrivateStatic}" ); }
    void Awake(){ }
    void Update() {
		framenoPrivate++;
		float f = Time.time / .4f;//framenoPrivate / 100.0f;
		float x = MathF.Sin( f );
		float z = MathF.Cos( f );
		int i;
		object o1 = "hello";
		object o2 = "world";
		if( o1 == o2 ) Debug.Log( "ILLEGAL MATCH" );
		for( i = 0; i < 10; i++ )
		{
			x++;
		}
		Debug.Log( $"XXX TestScript Update() {x} {z} {f}" );
		transform.position = new Vector3(x, 2, z);
	}
    public void PublicFunction() { Debug.Log( "Public Function" ); }
    public void PublicStaticFunction() { Debug.Log( "Public Staic Function" ); }
    private void PrivateStaticFunction() { Debug.Log( "Private Static Function" ); }
}
