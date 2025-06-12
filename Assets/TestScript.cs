using UnityEngine;
using System;
using Cilbox;

[Cilboxable]
public class TestScript : MonoBehaviour
{
	public int framenoPublic;
	private int framenoPrivate;
	private int delegateCount;
	[SerializeField] private int framenoPrivateButSerialize;
	static public int framenoPublicStatic;
	static private int framenoPrivateStatic;

	public UnityEngine.UI.Text textControl;
	public UnityEngine.UI.Button buttonControl1;
	public UnityEngine.UI.Button buttonControl2;
	public UnityEngine.UI.InputField inputField;

	public TestScript2 testScript2;

	private String lastButton = "No Button";

	TestScript() { framenoPrivate = 10; framenoPrivateStatic = 14; framenoPrivateStatic = 32; }
	void Start(){
		framenoPublic++;
		framenoPrivate+=2;
		Debug.Log( $"XXX TestScript Start() {framenoPublic} {framenoPrivate} {framenoPrivateStatic}" );
		Debug.Log( testScript2 );
		System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
		stopWatch.Start();
		testScript2.testPublic = 88;
		int fi = Fib(4);
		int rc = RecursePO2( 3 );
		stopWatch.Stop();
		TimeSpan ts = stopWatch.Elapsed;
		Debug.Log( $"RecursionTest :{rc} Time:{ts.Milliseconds}ms" );
		buttonControl1.onClick.AddListener( () => ClickDelegate("Button 1") );
		buttonControl2.onClick.AddListener( () => ClickDelegate("Button 2") );
		//inputField.onValueChanged.AddListener( (String s) => ClickDelegate(s) );
	}

	public void ClickDelegate(String s) { Debug.Log( $"Delegate {s}" ); lastButton = s; delegateCount++; }

	void Awake(){ }
	public int Fib( int n ) { if( n < 2 ) return n; else return Fib(n-1) + Fib(n-2); }
	public int RecursePO2( int n ) { if( n == 0 ) return 1; else return RecursePO2(n-1) + RecursePO2(n-1); }
	object SubFunction( float a, float b ) { return a + b; }

	void Update() {
		testScript2.AnotherThing();
		framenoPublic++;
		textControl.text = framenoPublic + "\n" + delegateCount + " - " + lastButton;

		float f = Time.time / .4f;//framenoPrivate / 100.0f;
		float x = MathF.Sin( f );
		float z = MathF.Cos( f );
		int i;
		object o1 = "hello";
		object o2 = "world";
		if( o1 == o2 ) Debug.LogError( "ILLEGAL MATCH" );
		for( i = 0; i < 10; i++ )
		{
			x = (float)SubFunction( x, 1.0f );
		}
		//Debug.Log( $"XXX TestScript Update() {x} {z} {f}" );
		transform.position = new Vector3(x, 2, z);

	}

	public void PublicFunction() { Debug.Log( "Public Function" ); }
	public void PublicStaticFunction() { Debug.Log( "Public Staic Function" ); }
	private void PrivateStaticFunction() { Debug.Log( "Private Static Function" ); }
}
