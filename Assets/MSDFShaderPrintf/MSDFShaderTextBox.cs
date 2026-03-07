using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MSDFShaderTextBox : MonoBehaviour
{
	//public TextField textField;
	private MaterialPropertyBlock block;
    
	public Color emissionColor = Color.white;
	
    [TextArea(3, 10)]
    public string textField;

	// Start is called before the first frame update
	void Start()
	{
		OnValidate();
	}
	
	void OnValidate()
	{
		Renderer renderer = GetComponentInChildren<Renderer>();
Debug.Log( "RENDER: " + renderer );
		if (block == null)
			block = new MaterialPropertyBlock();
		
		int maxlinelen = 0;
		int chaid;
		string fieldText = textField;
		int length = fieldText.Length;
		int x = 0, y = 0;
		for( chaid = 0; chaid < length; chaid++ )
		{
			int c = System.Convert.ToInt32( fieldText[chaid] );
			if( c == 10 )
			{
				y++;
				x = 0;
			}
			else
			{
				if( x > maxlinelen ) maxlinelen = x;
				x++;
			}
		}
		
		int w = maxlinelen + 1;
		int h = y + 1;
				
		float [] fa = new float[1024];
		x = 0;
		y = 0;
		for( chaid = 0; chaid < fieldText.Length; chaid++ )
		{
			int c = System.Convert.ToInt32( fieldText[chaid] );
			if( c == 10 )
			{
				y++;
				x = 0;
			}
			else
			{
				int index = x+y*w;
				if( index < fa.Length )
					fa[index] = c;
				x++;
			}
		}
		block.SetFloatArray("_Text", fa);
		block.SetFloat("_TextW", w);
		block.SetFloat("_TextH", h);
		block.SetColor("_FGColor", emissionColor );
		renderer.SetPropertyBlock(block);
	}
}
