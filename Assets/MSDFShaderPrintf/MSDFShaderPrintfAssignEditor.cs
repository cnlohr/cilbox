using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEditor;

#if false

#if  !UDONSHARP
[InitializeOnLoad]
#endif
public static class MSDFShaderPrintfAssignEditor
{
#if  !UDONSHARP
	static MSDFShaderPrintfAssignEditor()
	{
		string[] assets = AssetDatabase.FindAssets("msdfprintf"); 
		if( assets.Length > 0 )
		{
			Texture MSDFAssignTexture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(assets[0]));
			int id = Shader.PropertyToID("_UdonMSDFPrintf"); 
			Debug.Log($"Up and running {assets[0]} {AssetDatabase.GUIDToAssetPath(assets[0])} {id}");
			Debug.Log( MSDFAssignTexture );
			Shader.SetGlobalTexture( id, MSDFAssignTexture );
		}
		else
		{
			Debug.Log("Could not find asset.");
		}
	}
#endif
}
#endif

