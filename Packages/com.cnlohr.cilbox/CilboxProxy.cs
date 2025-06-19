using UnityEngine;
using System;

using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using Unity.Profiling;
#endif

namespace Cilbox
{
	public class CilboxProxy : MonoBehaviour
	{
		public StackElement [] fields;
		public UnityEngine.Object [] fieldsObjects;  // This is generally only held 
		public bool [] isFieldsObject;

		public CilboxClass cls;
		public Cilbox box;
		public String className;
		public String serializedObjectData;

		private bool proxyWasSetup = false;

		public CilboxProxy() { }

#if UNITY_EDITOR
		public void SetupProxy( Cilbox box, MonoBehaviour mToSteal, Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap )
		{
			this.box = box;
			this.className = mToSteal.GetType().ToString();

			box.BoxInitialize();
			cls = box.GetClass( className );

			fieldsObjects = new UnityEngine.Object[cls.instanceFieldNames.Length];
			isFieldsObject = new bool[cls.instanceFieldNames.Length];

			Dictionary<String, Serializee> instanceFields = new Dictionary<String, Serializee>();
			FieldInfo[] fi = mToSteal.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			foreach( var f in fi )
			{
				// TODO: Consider if we should try serializing _everything_.  No because arrays really need their constructors.
				if( !f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), true).Length <= 0 )
					continue;

				object fv = f.GetValue( mToSteal );

				bool bHandled = false;

				int matchingInstanceNameID = -1;
				for( int k = 0; k < cls.instanceFieldNames.Length; k++ )
				{
					if( cls.instanceFieldNames[k] == f.Name )
					{
						matchingInstanceNameID = k;
					}
				}

				if( matchingInstanceNameID < 0 ) continue;

				// Skip null objects.
				if (fv != null)
				{
					object[] attribs = fv.GetType().GetCustomAttributes(typeof(CilboxableAttribute), true);
					// Not a proxiable script.
					if (attribs != null && attribs.Length > 0)
					{
						// This is a cilboxable thing.
						fieldsObjects[matchingInstanceNameID] = refToProxyMap[(MonoBehaviour)fv];
						isFieldsObject[matchingInstanceNameID] = true;
						bHandled = true;
					}
					else if( fv is UnityEngine.Object )
					{
						fieldsObjects[matchingInstanceNameID] = (UnityEngine.Object)fv;
						isFieldsObject[matchingInstanceNameID] = true;
						bHandled = true;

						if( matchingInstanceNameID == -1 )
						{
							Debug.LogWarning( "Failed to link field object " + f.Name );
						}
					}

					if( !bHandled )
					{
						StackType st;
						if( StackElement.TypeToStackType.TryGetValue( fv.GetType().ToString(), out st ) && st < StackType.Object || fv is string )
							instanceFields[f.Name] = new Serializee( fv.ToString() );
						else if( fv.GetType() == typeof(String) )
							instanceFields[f.Name] = new Serializee( fv.ToString() );								
					}
				}
			}
			serializedObjectData = Convert.ToBase64String(new Serializee(instanceFields).DumpAsMemory().ToArray());
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

			Dictionary< String, Serializee > d = new Serializee( Convert.FromBase64String( serializedObjectData ), Serializee.ElementType.Map ).AsMap();
			Serializee tv;

			// Preinitialize any default values needed.
			for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
			{
				if( !isFieldsObject[i] && !d.TryGetValue( cls.instanceFieldNames[i], out tv ) )
				{
					// The value is not in the serialized data.  Need to make a guess, and give it a default initialization.
					StackType st;
					if( StackElement.TypeToStackType.TryGetValue(cls.instanceFieldTypes[i].ToString(), out st ) )
					{
						fields[i].type = st;
					}
					else
					{
						fields[i].LoadObject( null );
					}
				}
			}

			// Call interpreted constructor.
			box.InterpretIID( cls, this, ImportFunctionID.dotCtor, null );

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
						fieldsObjects[i] = null;
					}
				}
				else
				{
					if( d.TryGetValue( cls.instanceFieldNames[i], out tv ) )
					{
						fields[i].Load( CilboxUtil.DeserializeDataForProxyField( cls.instanceFieldTypes[i], tv.AsString() ) );
					}
				}
				//Debug.Log( $"{i} Output Type:{fields[i].type} Name:{cls.instanceFieldNames[i]} C# field Name:{cls.instanceFieldTypes[i]} Type:{fields[i].type} Value:{((fields[i].type<StackType.Object)?(fields[i].i):(fields[i].o))}" );
			}

			box.InterpretIID( cls, this, ImportFunctionID.Awake, null ); // Does this go before or after initialized fields.
			box.InterpretIID( cls, this, ImportFunctionID.Start, null );

			proxyWasSetup = true;
		}

		void Start()  {
			box.BoxInitialize(); // In case it is not yet initialized.

#if UNITY_EDITOR
			new ProfilerMarker( "Initialize " + className ).Auto();
#endif

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

