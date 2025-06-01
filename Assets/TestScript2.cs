using UnityEngine;

[Cilboxable]
public class TestScript2 : MonoBehaviour
{
    void Update() {	
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		MeshRenderer mr = GetComponent<MeshRenderer>();
		block.SetVector( "_Color", new Vector4( Mathf.Sin( (float)Time.time )*0.5f+0.5f, 0.5f, 0, 1 ) );
		mr.SetPropertyBlock(block);
	}
}
