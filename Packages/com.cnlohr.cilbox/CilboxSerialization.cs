using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Cilbox
{
	public class SerializedTypeDescriptor
	{
		public string assemblyName;
		public string typeName;
		// Generic fields (present when genericArgs token written)
		public string genericName;
		public SerializedTypeDescriptor[] genericArgs;
		// Underlying type (present when underlyingType token written)
		public SerializedTypeDescriptor underlyingType;

		public bool IsGeneric => genericArgs != null && genericArgs.Length > 0;
		public bool HasUnderlyingType => underlyingType != null;

		// Mirrors CilboxUtil.GetSerializeeFromNativeType key order: a, then
		// (n, gn, g) for generics or (n, [ut]) otherwise.
		public Serializee ToSerializee()
		{
			Dictionary<String, Serializee> ret = new Dictionary<String, Serializee>();
			ret["a"] = new Serializee( assemblyName );
			if( IsGeneric )
			{
				ret["n"] = new Serializee( typeName );
				ret["gn"] = new Serializee( genericName );
				Serializee[] sg = new Serializee[genericArgs.Length];
				for( int i = 0; i < genericArgs.Length; i++ )
					sg[i] = genericArgs[i].ToSerializee();
				ret["g"] = new Serializee( sg );
			}
			else
			{
				ret["n"] = new Serializee( typeName );
				if( underlyingType != null )
					ret["ut"] = underlyingType.ToSerializee();
			}
			return new Serializee( ret );
		}

		public static SerializedTypeDescriptor FromSerializee( Serializee s )
		{
			Dictionary<String, Serializee> m = s.AsMap();
			SerializedTypeDescriptor td = new SerializedTypeDescriptor();
			td.assemblyName = m["a"].AsString();
			td.typeName = m["n"].AsString();
			if( m.TryGetValue( "g", out Serializee g ) )
			{
				td.genericName = m["gn"].AsString();
				Serializee[] ga = g.AsArray();
				td.genericArgs = new SerializedTypeDescriptor[ga.Length];
				for( int i = 0; i < ga.Length; i++ )
					td.genericArgs[i] = FromSerializee( ga[i] );
			}
			if( m.TryGetValue( "ut", out Serializee ut ) )
				td.underlyingType = FromSerializee( ut );
			return td;
		}

		public static SerializedTypeDescriptor[] FromSerializeeArray(Serializee s)
		{
			Serializee[] sArr = s.AsArray();
			SerializedTypeDescriptor[] tdArr = new SerializedTypeDescriptor[sArr.Length];
			for (int i = 0; i < sArr.Length; i++)
			{
				tdArr[i] = FromSerializee(sArr[i]);
			}
			return tdArr;
		}
	}

	public class SerializedField
	{
		public string name;
		public SerializedTypeDescriptor type;

		// The same struct is serialized with two different type-key schemes:
		//  - class fields use "type"
		//  - method locals / parameters use "dt"
		private Serializee ToSerializeeWithTypeKey( String typeKey )
		{
			Dictionary<String, Serializee> ret = new Dictionary<String, Serializee>();
			ret["name"] = new Serializee( name );
			ret[typeKey] = type.ToSerializee();
			return new Serializee( ret );
		}

		public Serializee ToClassFieldSerializee() => ToSerializeeWithTypeKey( "type" );
		public Serializee ToMethodFieldSerializee() => ToSerializeeWithTypeKey( "dt" );

		private static SerializedField FromSerializeeWithTypeKey( Serializee s, String typeKey )
		{
			Dictionary<String, Serializee> m = s.AsMap();
			SerializedField f = new SerializedField();
			f.name = m["name"].AsString();
			f.type = SerializedTypeDescriptor.FromSerializee( m[typeKey] );
			return f;
		}

		public static SerializedField FromClassFieldSerializee( Serializee s ) => FromSerializeeWithTypeKey( s, "type" );
		public static SerializedField FromMethodFieldSerializee( Serializee s ) => FromSerializeeWithTypeKey( s, "dt" );
	}

	public class SerializedExceptionHandler
	{
		public int flags;
		public int tryOffset;
		public int tryLength;
		public int handlerOffset;
		public int handlerLength;
		public bool hasCatchType;
		public SerializedTypeDescriptor catchType;

		public Serializee ToSerializee()
		{
			Dictionary<String, Serializee> ret = new Dictionary<String, Serializee>();
			ret["flags"] = new Serializee(flags.ToString());
			ret["tryOff"] = new Serializee(tryOffset.ToString());
			ret["tryLen"] = new Serializee(tryLength.ToString());
			ret["hOff"] = new Serializee(handlerOffset.ToString());
			ret["hLen"] = new Serializee(handlerLength.ToString());
			if (hasCatchType)
				ret["cType"] = catchType.ToSerializee();
			return new Serializee(ret);
		}

		public static SerializedExceptionHandler FromSerializee(Serializee s)
		{
			Dictionary<String, Serializee> m = s.AsMap();
			SerializedExceptionHandler eh = new SerializedExceptionHandler();
			eh.flags = Int32.Parse(m["flags"].AsString());
			eh.tryOffset = Int32.Parse(m["tryOff"].AsString());
			eh.tryLength = Int32.Parse(m["tryLen"].AsString());
			eh.handlerOffset = Int32.Parse(m["hOff"].AsString());
			eh.handlerLength = Int32.Parse(m["hLen"].AsString());
			if (m.TryGetValue("cType", out Serializee cType))
			{
				eh.hasCatchType = true;
				eh.catchType = SerializedTypeDescriptor.FromSerializee( cType );
			}

			return eh;
		}
	}

	public class SerializedMethod
	{
		public string methodName;
		public int maxStack;
		public bool isVoid;
		public bool isStatic;
		public bool isCtor;
		public string fullSignature;
		public byte[] body;
		public SerializedField[] locals;
		public SerializedField[] parameters;
		public SerializedExceptionHandler[] exceptionHandlers;

		// for legacy serializations, methodName is the enclosing Map key, not part of this map.
		// Master MethodProps order: body, [eh], locals, parameters, maxStack,
		// isVoid, isCtor, isStatic, name, fullSignature.
		public Serializee ToSerializee()
		{
			Dictionary<String, Serializee> ret = new Dictionary<String, Serializee>();
			ret["body"] = Serializee.CreateFromBlob( body );

			if( exceptionHandlers != null && exceptionHandlers.Length > 0 )
			{
				Serializee[] eh = new Serializee[exceptionHandlers.Length];
				for( int i = 0; i < exceptionHandlers.Length; i++ )
					eh[i] = exceptionHandlers[i].ToSerializee();
				ret["eh"] = new Serializee( eh );
			}

			Serializee[] loc = new Serializee[locals.Length];
			for( int i = 0; i < locals.Length; i++ )
				loc[i] = locals[i].ToMethodFieldSerializee();
			ret["locals"] = new Serializee( loc );

			Serializee[] par = new Serializee[parameters.Length];
			for( int i = 0; i < parameters.Length; i++ )
				par[i] = parameters[i].ToMethodFieldSerializee();
			ret["parameters"] = new Serializee( par );

			ret["maxStack"] = new Serializee( maxStack.ToString() );
			ret["isVoid"] = new Serializee( isVoid ? "1" : "0" );
			ret["isCtor"] = new Serializee( isCtor ? "1" : "0" );
			ret["isStatic"] = new Serializee( isStatic ? "1" : "0" );
			ret["name"] = new Serializee( methodName );
			ret["fullSignature"] = new Serializee( fullSignature );
			return new Serializee( ret );
		}

		public static SerializedMethod FromSerializee( Serializee s, String methodName )
		{
			Dictionary<String, Serializee> m = s.AsMap();
			SerializedMethod sm = new SerializedMethod();
			sm.methodName = methodName;
			sm.body = m["body"].AsBlob();

			if( m.TryGetValue( "eh", out Serializee ehS ) )
			{
				Serializee[] eh = ehS.AsArray();
				sm.exceptionHandlers = new SerializedExceptionHandler[eh.Length];
				for( int i = 0; i < eh.Length; i++ )
					sm.exceptionHandlers[i] = SerializedExceptionHandler.FromSerializee( eh[i] );
			}
			else
			{
				sm.exceptionHandlers = new SerializedExceptionHandler[0];
			}

			Serializee[] loc = m["locals"].AsArray();
			sm.locals = new SerializedField[loc.Length];
			for( int i = 0; i < loc.Length; i++ )
				sm.locals[i] = SerializedField.FromMethodFieldSerializee( loc[i] );

			Serializee[] par = m["parameters"].AsArray();
			sm.parameters = new SerializedField[par.Length];
			for( int i = 0; i < par.Length; i++ )
				sm.parameters[i] = SerializedField.FromMethodFieldSerializee( par[i] );

			if( m.TryGetValue("isCtor", out Serializee isCtorSer) )
			{
				sm.isCtor = Convert.ToInt32(isCtorSer.AsString()) != 0;
			}
			else
			{
				// Backward compatibility for payloads generated before isCtor existed.
				sm.isCtor = methodName == ".ctor" || methodName == ".cctor" || sm.fullSignature.StartsWith("Void .ctor(") || sm.fullSignature.StartsWith("Void .cctor(");
			}

			sm.maxStack = Int32.Parse( m["maxStack"].AsString() );
			sm.isVoid = Int32.Parse( m["isVoid"].AsString() ) != 0;
			sm.isStatic = Int32.Parse( m["isStatic"].AsString() ) != 0;
			sm.fullSignature = m["fullSignature"].AsString();
			if (m.TryGetValue( "name", out Serializee nameS))
			{
				sm.methodName = nameS.AsString();
			}
			return sm;
		}
	}

	/// <summary>
	/// Helper to build a SerializedTypeDescriptor from a native System.Type.
	/// Used by the editor compiler to build type descriptors from native System.Type.
	/// </summary>
	public static class SerializedTypeDescriptorBuilder
	{
		public static SerializedTypeDescriptor FromNativeType(Type t)
		{
			var td = new SerializedTypeDescriptor();
			td.assemblyName = t.Assembly.GetName().Name;

			if (t.IsGenericType)
			{
				string genericDefName = t.GetGenericTypeDefinition().FullName;
				// Strip arity markers (`1, `2) from the generic definition name
				var sb = new StringBuilder();
				for (int i = 0; i < genericDefName.Length; i++)
				{
					if (genericDefName[i] != '`')
					{
						sb.Append(genericDefName[i]);
						continue;
					}

					int j = i + 1;
					while (j < genericDefName.Length && char.IsDigit(genericDefName[j]))
						j++;
					if (j == genericDefName.Length || genericDefName[j] == '+' || genericDefName[j] == '[')
						i = j - 1;
				}
				td.typeName = sb.ToString();
				td.genericName = genericDefName;
				Type[] ta = t.GenericTypeArguments;
				td.genericArgs = new SerializedTypeDescriptor[ta.Length];
				for (int i = 0; i < ta.Length; i++)
					td.genericArgs[i] = FromNativeType(ta[i]);
			}
			else
			{
				td.typeName = t.FullName;
				if (t.IsEnum && CilboxUtil.HasCilboxableAttribute(t))
					td.underlyingType = FromNativeType(t.GetEnumUnderlyingType());
			}
			return td;
		}
	}
}
