using UnityEngine;

//[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MSDFShaderPrintfGlobalAssign : MonoBehaviour
{
	public Texture MSDFAssignTexture;

	void Start()
	{
		int id = Shader.PropertyToID("_UdonMSDFPrintf"); 
		Shader.SetGlobalTexture( id, MSDFAssignTexture );
	}
}
