using UnityEngine;

[Cilboxable]
public class TestScript2 : MonoBehaviour
{
	public int framenoPublic2;
	private int framenoPrivate;
	[SerializeField] private int framenoPrivateButSerialize;
	static public int framenoPublic2Static;
	static private int framenoPrivateStatic;
	TestScript2() { Debug.Log( "XXX TestScript2 .ctor()" ); framenoPrivate = 10; }
    void Start(){ framenoPublic2++; framenoPrivate+=2; Debug.Log( $"XXX TestScript2 Start() {framenoPublic2} {framenoPrivate}" ); }
    void Awake(){ Debug.Log( "XXX TestScript2 Awake()" ); }
    void Update() { /*Debug.Log( "XXX TestScript Update()" );*/ framenoPrivate = 123456; }
    public void PublicFunction() { Debug.Log( "Public Function" ); }
    public void PublicStaticFunction() { Debug.Log( "Public Staic Function" ); }
    private void PrivateStaticFunction() { Debug.Log( "Private Static Function" ); }
}
