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
		public List< UnityEngine.Object > fieldsObjects;  // This is generally only held during saving and loading, not in use.

		public CilboxClass cls;
		public Cilbox box;
		public String className;
		public byte[] serializedObjectBytes;

		public String buildTimeGuid;
		public String initialLoadPath;

		private bool proxyWasSetup = false;

		private void ProxyDebugLog( string message )
		{
				Debug.Log( $"[CilboxProxy:{gameObject.name}] {message}" );
		}

		public CilboxProxy() { }

#if UNITY_EDITOR
		public void SetupProxy( Cilbox box, MonoBehaviour mToSteal, Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap )
		{
			this.box = box;
			this.className = mToSteal.GetType().ToString();

			box.BoxInitialize();
			cls = box.GetClass( className );

			fieldsObjects = new List< UnityEngine.Object >();
			FieldInfo[] fi = mToSteal.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

			List< SerializedProxyField > lstFields = new List< SerializedProxyField >();

			foreach( var f in fi )
			{
				// TODO: Consider if we should try serializing _everything_.  No because arrays really need their constructors.
				if( !f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), true).Length <= 0 )
					continue;

				object fv = f.GetValue( mToSteal );

				int matchingInstanceNameID = -1;
				for( int k = 0; k < cls.instanceFieldNames.Length; k++ )
				{
					if( cls.instanceFieldNames[k] == f.Name )
					{
						matchingInstanceNameID = k;
					}
				}


				if( matchingInstanceNameID < 0 )
				{
					Debug.Log( $"Warning: Could not find matching instance name for {f.Name}" );
					continue;
				}

				// Serialize no matter what.
				lstFields.Add( SerializeProxyField( fv, f.Name, matchingInstanceNameID, ref refToProxyMap ) );
			}

			SerializedProxy proxy = new SerializedProxy();
			proxy.fields = lstFields.ToArray();
			serializedObjectBytes = proxy.ToBytes();

			buildTimeGuid = Guid.NewGuid().ToString();
		}


		private SerializedProxyField SerializeProxyField( object fv, String fName, int matchingInstanceNameID, ref Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap )
		{
			SerializedProxyField spf = new SerializedProxyField();
			spf.fieldName = fName;
			spf.matchingInstanceId = matchingInstanceNameID;

			// Skip null objects.
			if( fv == null )
			{
				spf.fieldType = (byte)ProxyFieldType.Empty;
				return spf;
			}

			bool hasCilboxable = CilboxUtil.HasCilboxableAttribute( fv.GetType() );

			StackType st;

			// Serialize enum field as underlying type
			if( fv.GetType().IsEnum )
			{
				object underlying = Convert.ChangeType( fv, fv.GetType().GetEnumUnderlyingType() );
				if( StackElement.TypeToStackType.TryGetValue( underlying.GetType().ToString(), out st ) && st < StackType.Object )
				{
					spf.fieldType = (byte)ProxyFieldType.Primitive;
					spf.stackType = (byte)st;
					spf.primitiveValue = underlying;
				}
			}
			// Not a proxiable script.
			else if (hasCilboxable)
			{
				spf.fieldType = (byte)ProxyFieldType.CilboxRef;
				spf.fieldObjectIndex = fieldsObjects.Count;
				spf.objectRefIsNull = fv.ToString() == "null";
				fieldsObjects.Add( refToProxyMap[(MonoBehaviour)fv] );
			}
			else if( fv is UnityEngine.Object )
			{
				spf.fieldType = (byte)ProxyFieldType.ObjectRef;
				spf.fieldObjectIndex = fieldsObjects.Count;
				spf.objectRefIsNull = fv.ToString() == "null";
				fieldsObjects.Add( (UnityEngine.Object)fv );
			}
			else if( fv is string )
			{
				spf.fieldType = (byte)ProxyFieldType.String;
				spf.data = fv.ToString();
			}
			else if( StackElement.TypeToStackType.TryGetValue( fv.GetType().ToString(), out st ) && st < StackType.Object )
			{
				spf.fieldType = (byte)ProxyFieldType.Primitive;
				spf.stackType = (byte)st;
				spf.primitiveValue = fv;
			}
			else if( fv.GetType().IsArray )
			{
				spf.fieldType = (byte)ProxyFieldType.Array;
				Type type = fv.GetType().GetElementType();
				spf.elementType = SerializedTypeDescriptorBuilder.FromNativeType( type );
				Array arr = (Array)fv;
				int len = arr.Length;
				spf.arrayElements = new SerializedProxyField[len];
				for( int i = 0; i < len; i++ )
				{
					object o = arr.GetValue(i);
					spf.arrayElements[i] = SerializeProxyField( o, null, -1, ref refToProxyMap );
				}
			}
			else
			{
				spf.fieldType = (byte)ProxyFieldType.Json;
				spf.data = JsonUtility.ToJson(fv);
				Type type = fv.GetType();
				spf.elementType = SerializedTypeDescriptorBuilder.FromNativeType( type );
			}

			return spf;
		}

#endif
		void Awake()
		{
			// You cannot do anything in Awake()  Box is not set yet.
		}

		public void RuntimeProxyLoad()
		{
			//Debug.Log( "Runtime Proxy Load " + proxyWasSetup + " " + transform.name + " " + className );
			if( proxyWasSetup ) return;
			if (box == null) return;
			box.BoxInitialize(); // In case it is not yet initialized.
			bool verboseLogging = box.verboseLogging;

#if UNITY_EDITOR
			new ProfilerMarker( "Initialize " + className ).Auto();
#endif

			GameObject obj = gameObject;
			initialLoadPath = "/" + obj.name;
			while (obj.transform.parent != null)
			{
				obj = obj.transform.parent.gameObject;
				initialLoadPath = "/" + obj.name + initialLoadPath;
			}

			if( string.IsNullOrEmpty( className ) )
			{
				Debug.LogError( $"[CilboxProxy:{gameObject.name}] RuntimeProxyLoad aborted: class {className} was not found in Cilbox assembly data." );
				return;
			}

			cls = box.GetClass( className );

			// First thing: Go through any references that are prohibited.
			for( int i = 0; i < fieldsObjects.Count; i++ )
			{
				UnityEngine.Object o = fieldsObjects[i];
				if (o == null)
				{
					// If it's null, there's nothing to safety-check.
					continue;
				}
				Type t = o.GetType();
				if(box.GetComponentTypeOverride( t.FullName, out Type overrideType )) {
					Debug.Log( $"RuntimeProxyLoad: Override {t.FullName} with {overrideType.FullName}" );
					t = overrideType;
					if(typeof(CilboxShim).IsAssignableFrom(t) && fieldsObjects[i] is Component gameObjectComponent)
					{
						GameObject gameObject = gameObjectComponent.gameObject;
						Component component;
						if(gameObject.TryGetComponent(t, out Component c)) {
							component = c;
						} else
						{
							component = gameObject.AddComponent(t);
						}
						fieldsObjects[i] = component;
					}
				}
				if( t == typeof( CilboxProxy ) )
				{
					// If it's another cilbox proxy, it's OK.
				}
				else if( !box.CheckTypeAllowed( t.FullName ) )
				{
					Debug.LogWarning( $"Contraband found in script {className} field ID {i}: {o.GetType()}" );
					fieldsObjects[i] = null;
				}
			}

			// Populate fields[]
			fields = new StackElement[cls.instanceFieldNames.Length];

			SerializedProxy proxyData = SerializedProxy.FromBytes( serializedObjectBytes );

			SerializedProxyField[] matchingProxyField = new SerializedProxyField[cls.instanceFieldNames.Length];
			foreach( SerializedProxyField spf in proxyData.fields )
			{
				// Go over the root objects, to see which ones slot in and how.
				if( (ProxyFieldType)spf.fieldType != ProxyFieldType.Empty &&
					spf.matchingInstanceId >= 0 &&
					spf.matchingInstanceId < matchingProxyField.Length )
				{
					matchingProxyField[spf.matchingInstanceId] = spf;
				}
			}

			// Preinitialize every field to its CLR default value so that non-serialized fields
			// (especially UnityEngine.Object references) are not left as implicit StackType.Boolean.
			for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
			{
				Type fieldType = cls.instanceFieldTypes[i];
				// Maybe need to GetComponentTypeOverride here as well?  Maybe not, since that should only be for actual UnityEngine.Objects, which should be null at this point if they are contraband.
				if( fieldType == null )
				{
					fields[i].LoadObject( null );
					continue;
				}
				StackType st = StackElement.StackTypeFromType( fieldType );
				if( st < StackType.Object )
				{
					fields[i].type = st;
					if (verboseLogging)
						ProxyDebugLog( $"Default field init {cls.instanceFieldNames[i]} <- default({fieldType})" );
				}
				else if( fieldType.IsValueType )
				{
					try
					{
						// We clean the fieldtype before https://github.com/cnlohr/cilbox/blob/fc608341d293186e0aacf519ea9f0beb43d42cee/Packages/com.cnlohr.cilbox/Cilbox.cs#L1389C40-L1389C67
						object defaultValue = Activator.CreateInstance( fieldType );
						fields[i].LoadObject( defaultValue );
						if (verboseLogging)
							ProxyDebugLog( $"Default field init {cls.instanceFieldNames[i]} <- default({fieldType}) [boxed]" );
					}
					catch( Exception e )
					{
						fields[i].LoadObject( null );
						Debug.LogWarning( $"[CilboxProxy:{gameObject.name}] Failed to create default value for {cls.instanceFieldNames[i]} ({fieldType}): {e.Message}" );
					}
				}
				else
				{
					fields[i].LoadObject( null );
					if (verboseLogging)
						ProxyDebugLog( $"Default field init {cls.instanceFieldNames[i]} <- null" );
				}
			}

			// Call interpreted constructor.
			box.InterpretIID( cls, this, ImportFunctionID.dotCtor, null );

			// load serialized fields.
			for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
			{
				SerializedProxyField spf = matchingProxyField[i];

				if( spf == null ) { /* Debug.Log( $"Skipping {i} {cls.instanceFieldNames[i]}" ); */ continue; }

				object o;
				bool bIsObject = LoadObjectFromProxyField( spf, out o, cls.instanceFieldNames[i], cls.instanceFieldTypes[i] );
				if( bIsObject )
					fields[i].LoadObject( o );
				else
					fields[i].Load( o );
			}


			proxyWasSetup = true;
			if (verboseLogging)
				Debug.Log( $"RuntimeProxyLoad complete for class {className}" );
		}


		// Returns: true if is object, otherwise is primitive.
		private bool LoadObjectFromProxyField( SerializedProxyField spf, out object oOut, String rootFieldName, Type inType )
		{
			ProxyFieldType ft = (ProxyFieldType)spf.fieldType;

			switch (ft)
			{
			case ProxyFieldType.CilboxRef:
			case ProxyFieldType.ObjectRef:
			{
				int iFO = spf.fieldObjectIndex;
				if( iFO < fieldsObjects.Count )
				{
					if( spf.objectRefIsNull )
					{
						// This field was null when serialized, so just return null
						oOut = null;
						return true;
					}

					UnityEngine.Object o = fieldsObjects[iFO];

					if( o )
					{
						if( o is CilboxProxy )
							((CilboxProxy)o).RuntimeProxyLoad();

						oOut = o;

						// Remove reference out of the fieldsObjects array.
						fieldsObjects[iFO] = null;

						return true;
					}
					Debug.LogWarning( $"[CilboxProxy:{gameObject.name}] Object reference slot {iFO} for field {rootFieldName} is null/missing at load time." );
				}
				else
				{
					Debug.LogWarning( $"Failure to load object in field id:{rootFieldName} of {className} (slot out of range, fieldsObjects count={fieldsObjects.Count})");
				}
				break;
			}

			case ProxyFieldType.Array:
			{
				Type t = box.usage.GetNativeTypeFromDescriptor( spf.elementType );
				bool isCilboxElementType = false;

				if (t == null)
				{
					String elementTypeName = spf.elementType.typeName;
					if (box.classes.ContainsKey(elementTypeName))
					{
						// Check the array to see if it is Cilboxed
						t = typeof(CilboxProxy);
						isCilboxElementType = true;
					}
					else
					{
						oOut = null;
						return true;
					}
				}

				if( !isCilboxElementType && !box.CheckTypeAllowed( t.ToString() ) )
				{
					proxyWasSetup = false;
					throw new Exception( $"Contraband ARRAY found in script {className} field {rootFieldName}" );
				}

				int aLen = spf.arrayElements.Length;
				Array arr = Array.CreateInstance( t, aLen );

				for( int j = 0; j < aLen; j++ )
				{
					object o;
					LoadObjectFromProxyField( spf.arrayElements[j], out o, rootFieldName, t );
					arr.SetValue( o, j );
				}

				oOut = arr;
				return true;
			}

			case ProxyFieldType.String:
			{
				oOut = spf.data;
				return false;
			}

			case ProxyFieldType.Primitive:
			{
				oOut = spf.primitiveValue;
				return false;
			}

			case ProxyFieldType.Json:
			{
				Type t = box.usage.GetNativeTypeFromDescriptor( spf.elementType );
				oOut = JsonUtility.FromJson(spf.data, t);
				return true;
			}

			default:
				break;
			}

			oOut = null;
			return false;
		}


		void Start() {
			RuntimeProxyLoad();

			// Call Awake after initialization.
			box.InterpretIID( cls, this, ImportFunctionID.Awake, null );
			box.InterpretIID( cls, this, ImportFunctionID.Start, null );
		}
		void FixedUpdate() { if( proxyWasSetup ) box.InterpretIID( cls, this, ImportFunctionID.FixedUpdate, null ); }
		void Update() { if( proxyWasSetup ) box.InterpretIID( cls, this, ImportFunctionID.Update, null ); }
		void OnEnable() { if( proxyWasSetup ) box.InterpretIID( cls, this, ImportFunctionID.OnEnable, null ); }
		void OnDisable() { if( proxyWasSetup ) box.InterpretIID( cls, this, ImportFunctionID.OnDisable, null ); }
		void OnDestroy() { if( proxyWasSetup ) box.InterpretIID( cls, this, ImportFunctionID.OnDestroy, null ); }
		void OnTriggerEnter(Collider c) { if (proxyWasSetup) box.InterpretIID(cls, this, ImportFunctionID.OnTriggerEnter, new object[] { c }); }
		void OnTriggerExit(Collider c) { if (proxyWasSetup) box.InterpretIID(cls, this, ImportFunctionID.OnTriggerExit, new object[] { c }); }
		void OnCollisionEnter(Collision c) { if (proxyWasSetup) box.InterpretIID(cls, this, ImportFunctionID.OnCollisionEnter, new object[] { c }); }
		void OnCollisionExit(Collision c) { if (proxyWasSetup) box.InterpretIID(cls, this, ImportFunctionID.OnCollisionExit, new object[] { c }); }
	}
}

