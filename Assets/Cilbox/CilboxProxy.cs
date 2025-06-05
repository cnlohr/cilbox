using UnityEngine;
using System;

using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;

namespace Cilbox
{
	public class CilboxProxy : MonoBehaviour
	{
		public object [] fields;
		public MonoBehaviour [] fieldsObjects;

		public CilboxClass cls;
		public Cilbox box;
		public String className;
		public String serializedObjectData;

		CilboxProxy() { }

#if UNITY_EDITOR
		public void SetupProxy( Cilbox box, MonoBehaviour mToSteal, Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap )
		{
			this.box = box;
			this.className = mToSteal.GetType().ToString();
			Debug.Log( $"CilboxProxy.ctor() ClassName:{className} Box:{box}" );

			box.BoxInitialize();
			cls = box.GetClass( className );

			OrderedDictionary instanceFields = new OrderedDictionary();
			FieldInfo[] fi = mToSteal.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			foreach( var f in fi )
			{
				if( !f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), true).Length <= 0 )
					continue;

				object fv = f.GetValue( mToSteal );

				bool bHandled = false;
				fieldsObjects = new MonoBehaviour[cls.instanceFieldNames.Length];

				// Skip null objects.
				if (fv != null)
				{
					object[] attribs = fv.GetType().GetCustomAttributes(typeof(CilboxableAttribute), true);
					// Not a proxiable script.
					if (attribs != null && attribs.Length > 0)
					{

						// This is a cilboxable thing.
						int k;
						for( k = 0; k < cls.instanceFieldNames.Length; k++ )
						{
							if( cls.instanceFieldNames[k] == f.Name )
							{
								fieldsObjects[k] = refToProxyMap[(MonoBehaviour)fv];
								bHandled = true;
								break;
							}
						}
					}
				}

				if( !bHandled )
					serializedObjectData = CilboxUtil.SerializeDict( instanceFields );
				//Debug.Log( "Serializing: " + serializedObjectData );
			}

			Awake();
		}
#endif

		void Awake()
		{
			// Tricky: Stuff really isn't even ready here :(  I don't know if we can try to get this going.
		}

		void Start()  {
			Debug.Log( "Proxy Class Name: " + className );
			box.BoxInitialize(); // In case it is not yet initialized.

			if( string.IsNullOrEmpty( className ) ) return;

			if( fields == null )
			{
				cls = box.GetClass( className );
				// Populate fields[]
				fields = new object[cls.instanceFieldNames.Length];

				// Call interpreted constructor.
				box.InterpretIID( cls, this, ImportFunctionID.dotCtor, null );

				OrderedDictionary d = CilboxUtil.DeserializeDict( serializedObjectData );
				for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
				{
					if( fieldsObjects[i] != null )
					{
						fields[i] = fieldsObjects[i];
					}
					else if( ! (fields[i] is object) )
					{
						String fieldValue = (String)d[cls.instanceFieldNames[i]];
						fields[i] = CilboxUtil.DeserializeDataForProxyField( cls.instanceFieldTypes[i], fieldValue );
					}
				}
			}

			box.InterpretIID( cls, this, ImportFunctionID.Awake, null ); // Does this go before or after initialized fields.
			box.InterpretIID( cls, this, ImportFunctionID.Start, null );
		}
		void Update() { if( box != null ) box.InterpretIID( cls, this, ImportFunctionID.Update, null ); }
	}
}

