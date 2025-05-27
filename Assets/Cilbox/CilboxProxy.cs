using UnityEngine;
using System;

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
#endif

namespace Cilbox
{
	public class CilboxProxy : MonoBehaviour
	{
		public object [] fields;
		public CilboxClass cls;
		public String className;
		public String serializedObjectData;

		CilboxProxy() { }

#if UNITY_EDITOR
		public void SetupProxy( MonoBehaviour mToSteal )
		{
			this.className = mToSteal.GetType().ToString();
			Debug.Log( $"CilboxProxy.ctor() ClassName:{className}" );
			cls = Cilbox.GetClass( className );

			OrderedDictionary instanceFields = new OrderedDictionary();
			FieldInfo[] fi = mToSteal.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			foreach( var f in fi )
			{
				if( !f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), true).Length <= 0 )
					continue;

				instanceFields[f.Name] = f.GetValue( mToSteal ).ToString();
			}

			serializedObjectData = CilboxUtil.SerializeDict( instanceFields );
			Debug.Log( "Serializing: " + serializedObjectData );


			Awake();
		}
#endif

		void Awake()
		{
			if( string.IsNullOrEmpty( className ) ) return;

			// Populate fields[]
			if( fields == null )
			{
				cls = Cilbox.GetClass( className );
				fields = new object[cls.instanceFieldNames.Length];
				for( int i = 0; i < cls.instanceFieldNames.Length; i++ )
				{
					fields[i] = CilboxUtil.FillPossibleSystemType( cls.instanceFieldTypes[i] );
				}
			}

			Cilbox.InterpretIID( cls, this, ImportFunctionID.dotCtor, null );
			Cilbox.InterpretIID( cls, this, ImportFunctionID.Awake, null );
		}

		void Start()  { if( cls != null ) Cilbox.InterpretIID( cls, this, ImportFunctionID.Start, null ); }
		//void Update() { if( cls != null ) Cilbox.InterpretIID( cls, this, ImportFunctionID.Update, null ); }
	}
}

