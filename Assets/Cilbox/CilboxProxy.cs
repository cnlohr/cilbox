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
		public StackElement [] fields;
		public UnityEngine.Object [] fieldsObjects;
		public bool [] isFieldsObject;

		public CilboxClass cls;
		public Cilbox box;
		public String className;
		public String serializedObjectData;

		private bool proxyWasSetup = false;

		CilboxProxy() { }

#if UNITY_EDITOR
		public void SetupProxy( Cilbox box, MonoBehaviour mToSteal, Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap )
		{
			this.box = box;
			this.className = mToSteal.GetType().ToString();
			//Debug.Log( $"CilboxProxy.ctor() ClassName:{className} Box:{box}" );

			box.BoxInitialize();
			cls = box.GetClass( className );

			fieldsObjects = new UnityEngine.Object[cls.instanceFieldNames.Length];
			isFieldsObject = new bool[cls.instanceFieldNames.Length];

			OrderedDictionary instanceFields = new OrderedDictionary();
			FieldInfo[] fi = mToSteal.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			foreach( var f in fi )
			{
				// TODO: Consider if we should try serializing _everything_.  No because arrays really need their constructors.
				if( !f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), true).Length <= 0 )
					continue;

				object fv = f.GetValue( mToSteal );

				bool bHandled = false;

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
								isFieldsObject[k] = true;
								bHandled = true;
								break;
							}
						}
					}
					else if( fv is UnityEngine.Object )
					{
						int k;
						for( k = 0; k < cls.instanceFieldNames.Length; k++ )
						{
							if( cls.instanceFieldNames[k] == f.Name )
							{
								fieldsObjects[k] = (UnityEngine.Object)fv;
								isFieldsObject[k] = true;
								bHandled = true;
								break;
							}
						}
						if( k == cls.instanceFieldNames.Length )
						{
							Debug.LogWarning( "Failed to link field object " + f.Name );
						}
					}

					if( !bHandled )
					{
						if( fv is UnityEngine.Object)
						{
							if( (UnityEngine.Object)fv )
							{
								Debug.LogError( $"Error: Field {f.Name} in {cls.className} is a UnityObject but could not be added to the fieldsObjects" );
								//instanceFields[f.Name] = fv.ToString();
							}
							else
							{
								instanceFields[f.Name] = null;
							}
						}
						else
						{
							instanceFields[f.Name] = fv.ToString();
						}
					}
				}
				//Debug.Log( "Serializing: " + serializedObjectData );
			}
			serializedObjectData = CilboxUtil.SerializeDict( instanceFields );
		}
#endif

		void Awake()
		{
			// Tricky: Stuff really isn't even ready here :(  I don't know if we can try to get this going.
		}

		public void RuntimeProxyLoad()
		{
			//Debug.Log( "Runtime Proxy Load " + proxyWasSetup + " " + transform.name + " " + className );
			if( proxyWasSetup ) return;

			cls = box.GetClass( className );
			// Populate fields[]

			fields = new StackElement[cls.instanceFieldNames.Length];

			// Call interpreted constructor.
			box.InterpretIID( cls, this, ImportFunctionID.dotCtor, null );

			OrderedDictionary d = CilboxUtil.DeserializeDict( serializedObjectData );

			for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
			{
				//Debug.Log( $"isObject: {isFieldsObject[i]} name {cls.instanceFieldNames[i]} Is Object: {fields[i] is object}  typ {cls.instanceFieldTypes[i]}" );
				if( isFieldsObject[i] )
				{
					UnityEngine.Object o = fieldsObjects[i];
					if( o )
					{
						if( o is CilboxProxy )
							((CilboxProxy)o).RuntimeProxyLoad();
						fields[i].Load( fieldsObjects[i] );
					}
				}
#if false
				// What is this even for anyway?????

				else if( ! (fields[i] is object) ) // Has the constructor already filled it out?
				{
					String cfn = cls.instanceFieldNames[i];
					object v = d[cfn];
					if( v != null )
					{
						String fieldValue = (String)v;
						//Debug.Log( $"Deserializing: {cfn} from {fieldValue} type {cls.instanceFieldTypes[i]}" );
						fields[i].Load( CilboxUtil.DeserializeDataForProxyField( cls.instanceFieldTypes[i], fieldValue ) );
					}
					else
					{
						// It's probably a private.
						//Debug.LogError( "Could not find field " + cfn + " on class " + cls.className );
						Type t = cls.instanceFieldTypes[i];
						if( t.IsValueType )
						{
							fields[i].Load( Activator.CreateInstance( t ) );
						}
					}
				}
#endif
			}

			box.InterpretIID( cls, this, ImportFunctionID.Awake, null ); // Does this go before or after initialized fields.
			box.InterpretIID( cls, this, ImportFunctionID.Start, null );

			proxyWasSetup = true;
		}

		void Start()  {
			box.BoxInitialize(); // In case it is not yet initialized.
			if( string.IsNullOrEmpty( className ) )
			{
				Debug.LogError( "Class name not set" );
				return;
			}

			RuntimeProxyLoad();
		}
		void Update() { if( box != null ) box.InterpretIID( cls, this, ImportFunctionID.Update, null ); }
		void FixedUpdate() { if( box != null ) box.InterpretIID( cls, this, ImportFunctionID.FixedUpdate, null ); }
	}
}

