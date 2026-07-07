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
