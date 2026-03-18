using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Cilbox
{
	/// <summary>
	/// Similar to BinaryReady/BinaryWriter but uses stack-allocated buffers to avoid heap allocations.
	/// </summary>
	public static class BinaryHelper
	{
		private static void Write7BitEncodedInt(List<byte> buf, int value)
		{
			uint v = (uint)value;
			while (v >= 0x80)
			{
				buf.Add((byte)(v | 0x80));
				v >>= 7;
			}
			buf.Add((byte)v);
		}

		private static int Read7BitEncodedInt(byte[] buf, ref int pos)
		{
			int result = 0, shift = 0;
			while (true)
			{
				byte b = buf[pos++];
				result |= (b & 0x7F) << shift;
				if ((b & 0x80) == 0) break;
				shift += 7;
			}
			return result;
		}

		public static int ReadInt(byte[] buf, ref int pos)
		{
			int v = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(pos));
			pos += 4;
			return v;
		}

		public static uint ReadUInt(byte[] buf, ref int pos)
		{
			uint v = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(pos));
			pos += 4;
			return v;
		}

		public static string ReadString(byte[] buf, ref int pos)
		{
			int len = Read7BitEncodedInt(buf, ref pos);
			string s = Encoding.UTF8.GetString(buf, pos, len);
			pos += len;
			return s;
		}

		public static byte ReadByte(byte[] buf, ref int pos) => buf[pos++];

		public static bool ReadBool(byte[] buf, ref int pos) => buf[pos++] != 0;

		public static short ReadShort(byte[] buf, ref int pos)
		{
			short v = BinaryPrimitives.ReadInt16LittleEndian(buf.AsSpan(pos));
			pos += 2;
			return v;
		}

		public static ushort ReadUShort(byte[] buf, ref int pos)
		{
			ushort v = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(pos));
			pos += 2;
			return v;
		}

		public static sbyte ReadSByte(byte[] buf, ref int pos) => (sbyte)buf[pos++];

		public static long ReadLong(byte[] buf, ref int pos)
		{
			long v = BinaryPrimitives.ReadInt64LittleEndian(buf.AsSpan(pos));
			pos += 8;
			return v;
		}

		public static ulong ReadULong(byte[] buf, ref int pos)
		{
			ulong v = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(pos));
			pos += 8;
			return v;
		}

		public static float ReadFloat(byte[] buf, ref int pos)
		{
			int bits = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(pos));
			pos += 4;
			return BitConverter.Int32BitsToSingle(bits);
		}

		public static double ReadDouble(byte[] buf, ref int pos)
		{
			long bits = BinaryPrimitives.ReadInt64LittleEndian(buf.AsSpan(pos));
			pos += 8;
			return BitConverter.Int64BitsToDouble(bits);
		}

		public static byte[] ReadBlob(byte[] buf, ref int pos)
		{
			int len = Read7BitEncodedInt(buf, ref pos);
			byte[] b = new byte[len];
			Buffer.BlockCopy(buf, pos, b, 0, len);
			pos += len;
			return b;
		}

		public static void WriteInt(List<byte> buf, int v)
		{
			Span<byte> tmp = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(tmp, v);
			buf.Add(tmp[0]); buf.Add(tmp[1]); buf.Add(tmp[2]); buf.Add(tmp[3]);
		}

		public static void WriteUInt(List<byte> buf, uint v)
		{
			Span<byte> tmp = stackalloc byte[4];
			BinaryPrimitives.WriteUInt32LittleEndian(tmp, v);
			buf.Add(tmp[0]); buf.Add(tmp[1]); buf.Add(tmp[2]); buf.Add(tmp[3]);
		}

		public static void WriteString(List<byte> buf, string s)
		{
			int len = Encoding.UTF8.GetByteCount(s);
			Write7BitEncodedInt(buf, len);
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			buf.AddRange(bytes);
		}

		public static void WriteByte(List<byte> buf, byte v) => buf.Add(v);

		public static void WriteBool(List<byte> buf, bool v) => buf.Add(v ? (byte)1 : (byte)0);

		public static void WriteShort(List<byte> buf, short v)
		{
			Span<byte> tmp = stackalloc byte[2];
			BinaryPrimitives.WriteInt16LittleEndian(tmp, v);
			buf.Add(tmp[0]); buf.Add(tmp[1]);
		}

		public static void WriteUShort(List<byte> buf, ushort v)
		{
			Span<byte> tmp = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16LittleEndian(tmp, v);
			buf.Add(tmp[0]); buf.Add(tmp[1]);
		}

		public static void WriteSByte(List<byte> buf, sbyte v) => buf.Add((byte)v);

		public static void WriteLong(List<byte> buf, long v)
		{
			Span<byte> tmp = stackalloc byte[8];
			BinaryPrimitives.WriteInt64LittleEndian(tmp, v);
			for (int i = 0; i < 8; i++) buf.Add(tmp[i]);
		}

		public static void WriteULong(List<byte> buf, ulong v)
		{
			Span<byte> tmp = stackalloc byte[8];
			BinaryPrimitives.WriteUInt64LittleEndian(tmp, v);
			for (int i = 0; i < 8; i++) buf.Add(tmp[i]);
		}

		public static void WriteFloat(List<byte> buf, float v)
		{
			Span<byte> tmp = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(tmp, BitConverter.SingleToInt32Bits(v));
			buf.Add(tmp[0]); buf.Add(tmp[1]); buf.Add(tmp[2]); buf.Add(tmp[3]);
		}

		public static void WriteDouble(List<byte> buf, double v)
		{
			Span<byte> tmp = stackalloc byte[8];
			BinaryPrimitives.WriteInt64LittleEndian(tmp, BitConverter.DoubleToInt64Bits(v));
			for (int i = 0; i < 8; i++) buf.Add(tmp[i]);
		}

		public static void WriteBlob(List<byte> buf, byte[] data)
		{
			Write7BitEncodedInt(buf, data.Length);
			buf.AddRange(data);
		}

		public delegate T ReadFunc<T>(byte[] buf, ref int pos);

		public static T[] ReadArray<T>(byte[] buf, ref int pos, ReadFunc<T> readElement)
		{
			int count = ReadInt(buf, ref pos);
			T[] arr = new T[count];
			for (int i = 0; i < count; i++)
				arr[i] = readElement(buf, ref pos);
			return arr;
		}

		public static void WriteArray<T>(List<byte> buf, T[] arr, Action<T> writeElement)
		{
			WriteInt(buf, arr.Length);
			for (int i = 0; i < arr.Length; i++)
				writeElement(arr[i]);
		}

		public static byte[] CompressToBytes(byte version, Action<List<byte>> writePayload)
		{
			List<byte> payload = new List<byte>();
			writePayload(payload);
			byte[] uncompressed = payload.ToArray();

			byte[] compressed;
			using (var ms = new MemoryStream())
			{
				using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
					deflate.Write(uncompressed, 0, uncompressed.Length);
				compressed = ms.ToArray();
			}

			List<byte> buf = new List<byte>(1 + 4 + compressed.Length);
			WriteByte(buf, version);
			WriteInt(buf, uncompressed.Length);
			buf.AddRange(compressed);
			return buf.ToArray();
		}

		public static T DecompressAndRead<T>(byte[] data, byte expectedVersion, string formatName,
			ReadFunc<T> readPayload)
		{
			int pos = 0;
			byte version = ReadByte(data, ref pos);
			if (version != expectedVersion)
				throw new CilboxException($"Unsupported {formatName} binary format version {version}, expected {expectedVersion}");

			int uncompressedSize = ReadInt(data, ref pos);
			byte[] uncompressed = new byte[uncompressedSize];
			using (var ms = new MemoryStream(data, pos, data.Length - pos))
			using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
			{
				int totalRead = 0;
				while (totalRead < uncompressedSize)
				{
					int read = deflate.Read(uncompressed, totalRead, uncompressedSize - totalRead);
					if (read == 0)
						throw new CilboxException($"Deflate stream ended early: got {totalRead} bytes, expected {uncompressedSize}");
					totalRead += read;
				}
			}

			int payloadpos = 0;
			return readPayload(uncompressed, ref payloadpos);
		}
	}

	public class SerializedTypeDescriptor
	{
		public string assemblyName;
		public string typeName;
		// Generic fields (present when isGeneric flag set)
		public string genericName;
		public SerializedTypeDescriptor[] genericArgs;
		// Underlying type (present when hasUnderlyingType flag set)
		public SerializedTypeDescriptor underlyingType;

		public bool IsGeneric => genericArgs != null && genericArgs.Length > 0;
		public bool HasUnderlyingType => underlyingType != null;

		public static SerializedTypeDescriptor Read(byte[] buf, ref int pos)
		{
			var td = new SerializedTypeDescriptor();
			byte flags = BinaryHelper.ReadByte(buf, ref pos);
			td.assemblyName = BinaryHelper.ReadString(buf, ref pos);
			td.typeName = BinaryHelper.ReadString(buf, ref pos);
			if ((flags & 1) != 0) // isGeneric
			{
				td.genericName = BinaryHelper.ReadString(buf, ref pos);
				td.genericArgs = BinaryHelper.ReadArray(buf, ref pos, Read);
			}
			if ((flags & 2) != 0) // hasUnderlyingType
			{
				td.underlyingType = Read(buf, ref pos);
			}
			return td;
		}

		public void Write(List<byte> buf)
		{
			byte flags = 0;
			if (IsGeneric) flags |= 1;
			if (HasUnderlyingType) flags |= 2;
			BinaryHelper.WriteByte(buf, flags);
			BinaryHelper.WriteString(buf, assemblyName);
			BinaryHelper.WriteString(buf, typeName);
			if (IsGeneric)
			{
				BinaryHelper.WriteString(buf, genericName);
				BinaryHelper.WriteArray(buf, genericArgs, e => e.Write(buf));
			}
			if (HasUnderlyingType)
			{
				underlyingType.Write(buf);
			}
		}

	}

	public class SerializedField
	{
		public string name;
		public SerializedTypeDescriptor type;

		public static SerializedField Read(byte[] buf, ref int pos)
		{
			var f = new SerializedField();
			f.name = BinaryHelper.ReadString(buf, ref pos);
			f.type = SerializedTypeDescriptor.Read(buf, ref pos);
			return f;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteString(buf, name);
			type.Write(buf);
		}
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

		public static SerializedExceptionHandler Read(byte[] buf, ref int pos)
		{
			var eh = new SerializedExceptionHandler();
			eh.flags = BinaryHelper.ReadInt(buf, ref pos);
			eh.tryOffset = BinaryHelper.ReadInt(buf, ref pos);
			eh.tryLength = BinaryHelper.ReadInt(buf, ref pos);
			eh.handlerOffset = BinaryHelper.ReadInt(buf, ref pos);
			eh.handlerLength = BinaryHelper.ReadInt(buf, ref pos);
			eh.hasCatchType = BinaryHelper.ReadBool(buf, ref pos);
			if (eh.hasCatchType)
				eh.catchType = SerializedTypeDescriptor.Read(buf, ref pos);
			return eh;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteInt(buf, flags);
			BinaryHelper.WriteInt(buf, tryOffset);
			BinaryHelper.WriteInt(buf, tryLength);
			BinaryHelper.WriteInt(buf, handlerOffset);
			BinaryHelper.WriteInt(buf, handlerLength);
			BinaryHelper.WriteBool(buf, hasCatchType);
			if (hasCatchType)
				catchType.Write(buf);
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

		public static SerializedMethod Read(byte[] buf, ref int pos)
		{
			var m = new SerializedMethod();
			m.methodName = BinaryHelper.ReadString(buf, ref pos);
			m.maxStack = BinaryHelper.ReadInt(buf, ref pos);
			byte flags = BinaryHelper.ReadByte(buf, ref pos);
			m.isVoid = (flags & 1) != 0;
			m.isStatic = (flags & 2) != 0;
			m.isCtor = (flags & 4) != 0;
			m.fullSignature = BinaryHelper.ReadString(buf, ref pos);
			m.body = BinaryHelper.ReadBlob(buf, ref pos);
			m.locals = BinaryHelper.ReadArray(buf, ref pos, SerializedField.Read);
			m.parameters = BinaryHelper.ReadArray(buf, ref pos, SerializedField.Read);
			m.exceptionHandlers = BinaryHelper.ReadArray(buf, ref pos, SerializedExceptionHandler.Read);
			return m;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteString(buf, methodName);
			BinaryHelper.WriteInt(buf, maxStack);
			byte flags = 0;
			if (isVoid) flags |= 1;
			if (isStatic) flags |= 2;
			if (isCtor) flags |= 4;
			BinaryHelper.WriteByte(buf, flags);
			BinaryHelper.WriteString(buf, fullSignature);
			BinaryHelper.WriteBlob(buf, body);
			BinaryHelper.WriteArray(buf, locals, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, parameters, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, exceptionHandlers, e => e.Write(buf));
		}
	}

	public class SerializedClass
	{
		public string className;
		public SerializedField[] staticFields;
		public SerializedField[] instanceFields;
		public SerializedMethod[] methods;

		public static SerializedClass Read(byte[] buf, ref int pos)
		{
			var c = new SerializedClass();
			c.className = BinaryHelper.ReadString(buf, ref pos);
			c.staticFields = BinaryHelper.ReadArray(buf, ref pos, SerializedField.Read);
			c.instanceFields = BinaryHelper.ReadArray(buf, ref pos, SerializedField.Read);
			c.methods = BinaryHelper.ReadArray(buf, ref pos, SerializedMethod.Read);
			return c;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteString(buf, className);
			BinaryHelper.WriteArray(buf, staticFields, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, instanceFields, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, methods, e => e.Write(buf));
		}
	}

	/// <summary>
	/// Metadata token — discriminated union keyed by MetaTokenType.
	/// Stored in a flat array indexed by integer ID.
	/// </summary>
	public class SerializedMetadataToken
	{
		public byte metaTokenType; // MetaTokenType enum value

		// mtString
		public string stringValue;

		// mtArrayInitializer
		public byte[] arrayInitData;

		// mtField
		public SerializedTypeDescriptor fieldDeclaringType;
		public string fieldName;
		public bool fieldIsStatic;
		public bool fieldHasIndex;
		public int fieldIndex;

		// mtType
		public SerializedTypeDescriptor typeDescriptor;

		// mtMethod
		public SerializedTypeDescriptor methodDeclaringType;
		public string methodName;
		public string methodFullSignature;
		public bool methodIsStatic;
		public string methodAssembly;
		public SerializedTypeDescriptor[] methodParameters;
		public SerializedTypeDescriptor[] methodGenericArguments;

		public static SerializedMetadataToken Read(byte[] buf, ref int pos)
		{
			var t = new SerializedMetadataToken();
			t.metaTokenType = BinaryHelper.ReadByte(buf, ref pos);
			MetaTokenType mt = (MetaTokenType)t.metaTokenType;

			switch (mt)
			{
			case MetaTokenType.mtString:
				t.stringValue = BinaryHelper.ReadString(buf, ref pos);
				break;
			case MetaTokenType.mtArrayInitializer:
				t.arrayInitData = BinaryHelper.ReadBlob(buf, ref pos);
				break;
			case MetaTokenType.mtField:
				t.fieldDeclaringType = SerializedTypeDescriptor.Read(buf, ref pos);
				t.fieldName = BinaryHelper.ReadString(buf, ref pos);
				t.fieldIsStatic = BinaryHelper.ReadBool(buf, ref pos);
				t.fieldHasIndex = BinaryHelper.ReadBool(buf, ref pos);
				if (t.fieldHasIndex)
					t.fieldIndex = BinaryHelper.ReadInt(buf, ref pos);
				break;
			case MetaTokenType.mtType:
				t.typeDescriptor = SerializedTypeDescriptor.Read(buf, ref pos);
				break;
			case MetaTokenType.mtMethod:
				t.methodDeclaringType = SerializedTypeDescriptor.Read(buf, ref pos);
				t.methodName = BinaryHelper.ReadString(buf, ref pos);
				t.methodFullSignature = BinaryHelper.ReadString(buf, ref pos);
				t.methodIsStatic = BinaryHelper.ReadBool(buf, ref pos);
				t.methodAssembly = BinaryHelper.ReadString(buf, ref pos);
				t.methodParameters = BinaryHelper.ReadArray(buf, ref pos, SerializedTypeDescriptor.Read);
				t.methodGenericArguments = BinaryHelper.ReadArray(buf, ref pos, SerializedTypeDescriptor.Read);
				break;
			}
			return t;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteByte(buf, metaTokenType);
			MetaTokenType mt = (MetaTokenType)metaTokenType;

			switch (mt)
			{
			case MetaTokenType.mtString:
				BinaryHelper.WriteString(buf, stringValue);
				break;
			case MetaTokenType.mtArrayInitializer:
				BinaryHelper.WriteBlob(buf, arrayInitData);
				break;
			case MetaTokenType.mtField:
				fieldDeclaringType.Write(buf);
				BinaryHelper.WriteString(buf, fieldName);
				BinaryHelper.WriteBool(buf, fieldIsStatic);
				BinaryHelper.WriteBool(buf, fieldHasIndex);
				if (fieldHasIndex)
					BinaryHelper.WriteInt(buf, fieldIndex);
				break;
			case MetaTokenType.mtType:
				typeDescriptor.Write(buf);
				break;
			case MetaTokenType.mtMethod:
				methodDeclaringType.Write(buf);
				BinaryHelper.WriteString(buf, methodName);
				BinaryHelper.WriteString(buf, methodFullSignature);
				BinaryHelper.WriteBool(buf, methodIsStatic);
				BinaryHelper.WriteString(buf, methodAssembly);
				BinaryHelper.WriteArray(buf, methodParameters ?? Array.Empty<SerializedTypeDescriptor>(), e => e.Write(buf));
				BinaryHelper.WriteArray(buf, methodGenericArguments ?? Array.Empty<SerializedTypeDescriptor>(), e => e.Write(buf));
				break;
			}
		}
	}

	public class SerializedEnumValue
	{
		public string name;
		public long value;

		public static SerializedEnumValue Read(byte[] buf, ref int pos)
		{
			var v = new SerializedEnumValue();
			v.name = BinaryHelper.ReadString(buf, ref pos);
			v.value = BinaryHelper.ReadLong(buf, ref pos);
			return v;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteString(buf, name);
			BinaryHelper.WriteLong(buf, value);
		}
	}

	public class SerializedEnum
	{
		public string enumName;
		public SerializedTypeDescriptor underlyingType;
		public SerializedEnumValue[] values;

		public static SerializedEnum Read(byte[] buf, ref int pos)
		{
			var e = new SerializedEnum();
			e.enumName = BinaryHelper.ReadString(buf, ref pos);
			e.underlyingType = SerializedTypeDescriptor.Read(buf, ref pos);
			e.values = BinaryHelper.ReadArray(buf, ref pos, SerializedEnumValue.Read);
			return e;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteString(buf, enumName);
			underlyingType.Write(buf);
			BinaryHelper.WriteArray(buf, values, e => e.Write(buf));
		}
	}

	public class SerializedAssembly
	{
		public const byte FormatVersion = 1;

		public SerializedClass[] classes;
		public SerializedMetadataToken[] metadata; // index 0 is invalid/empty sentinel
		public SerializedEnum[] enums;

		public static SerializedAssembly Read(byte[] buf, ref int pos)
		{
			var asm = new SerializedAssembly();
			asm.classes = BinaryHelper.ReadArray(buf, ref pos, SerializedClass.Read);
			asm.metadata = BinaryHelper.ReadArray(buf, ref pos, SerializedMetadataToken.Read);
			asm.enums = BinaryHelper.ReadArray(buf, ref pos, SerializedEnum.Read);
			return asm;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteArray(buf, classes, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, metadata, e => e.Write(buf));
			BinaryHelper.WriteArray(buf, enums, e => e.Write(buf));
		}

		public byte[] ToBytes() => BinaryHelper.CompressToBytes(FormatVersion, Write);

		public static SerializedAssembly FromBytes(byte[] data) =>
			BinaryHelper.DecompressAndRead(data, FormatVersion, "assembly", Read);
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

	public enum ProxyFieldType : byte
	{
		Empty       = 0,  // null field
		String      = 1,  // "s"
		Primitive   = 2,  // "e<StackType>" — primitives and enums
		CilboxRef   = 3,  // "cba" — Cilboxable object reference
		ObjectRef   = 4,  // "obj" — UnityEngine.Object reference
		Array       = 5,  // "a"
		Json        = 6,  // "j" — JsonUtility-serialized struct
	}

	public class SerializedProxyField
	{
		public byte fieldType;            // ProxyFieldType
		public string fieldName;          // null for non-root (array elements)
		public int matchingInstanceId;    // -1 for non-root

		// String / Json
		public string data;               // value as string / JSON text
		// Primitive
		public object primitiveValue;     // boxed primitive value (int, float, bool, etc.)
		public byte stackType;            // sub-discriminator for Primitive (StackType byte)

		// CilboxRef / ObjectRef
		public int fieldObjectIndex;      // index into fieldsObjects
		public bool objectRefIsNull;      // true when the original ref was null

		// Array / Json
		public SerializedTypeDescriptor elementType;

		// Array (recursive)
		public SerializedProxyField[] arrayElements;

		private static object ReadPrimitiveValue(byte[] buf, ref int pos, byte stackType)
		{
			switch (stackType)
			{
			case 0:  return BinaryHelper.ReadBool(buf, ref pos);    // Boolean
			case 1:  return BinaryHelper.ReadSByte(buf, ref pos);   // Sbyte
			case 2:  return BinaryHelper.ReadByte(buf, ref pos);    // Byte
			case 3:  return BinaryHelper.ReadShort(buf, ref pos);   // Short
			case 4:  return BinaryHelper.ReadUShort(buf, ref pos);  // Ushort
			case 5:  return BinaryHelper.ReadInt(buf, ref pos);     // Int
			case 6:  return BinaryHelper.ReadUInt(buf, ref pos);    // Uint
			case 7:  return BinaryHelper.ReadLong(buf, ref pos);    // Long
			case 8:  return BinaryHelper.ReadULong(buf, ref pos);   // Ulong
			case 9:  return BinaryHelper.ReadFloat(buf, ref pos);   // Float
			case 10: return BinaryHelper.ReadDouble(buf, ref pos);  // Double
			default: throw new CilboxException($"Unknown primitive stackType {stackType}");
			}
		}

		private static void WritePrimitiveValue(List<byte> buf, object value, byte stackType)
		{
			switch (stackType)
			{
			case 0:  BinaryHelper.WriteBool(buf, (bool)value);     break; // Boolean
			case 1:  BinaryHelper.WriteSByte(buf, (sbyte)value);   break; // Sbyte
			case 2:  BinaryHelper.WriteByte(buf, (byte)value);     break; // Byte
			case 3:  BinaryHelper.WriteShort(buf, (short)value);   break; // Short
			case 4:  BinaryHelper.WriteUShort(buf, (ushort)value); break; // Ushort
			case 5:  BinaryHelper.WriteInt(buf, (int)value);       break; // Int
			case 6:  BinaryHelper.WriteUInt(buf, (uint)value);     break; // Uint
			case 7:  BinaryHelper.WriteLong(buf, (long)value);     break; // Long
			case 8:  BinaryHelper.WriteULong(buf, (ulong)value);   break; // Ulong
			case 9:  BinaryHelper.WriteFloat(buf, (float)value);   break; // Float
			case 10: BinaryHelper.WriteDouble(buf, (double)value); break; // Double
			default: throw new CilboxException($"Unknown primitive stackType {stackType}");
			}
		}

		public static SerializedProxyField Read(byte[] buf, ref int pos)
		{
			var f = new SerializedProxyField();
			f.fieldType = BinaryHelper.ReadByte(buf, ref pos);
			bool hasName = BinaryHelper.ReadBool(buf, ref pos);
			if (hasName)
				f.fieldName = BinaryHelper.ReadString(buf, ref pos);
			f.matchingInstanceId = BinaryHelper.ReadInt(buf, ref pos);

			switch ((ProxyFieldType)f.fieldType)
			{
			case ProxyFieldType.Empty:
				break;
			case ProxyFieldType.String:
				f.data = BinaryHelper.ReadString(buf, ref pos);
				break;
			case ProxyFieldType.Primitive:
				f.stackType = BinaryHelper.ReadByte(buf, ref pos);
				f.primitiveValue = ReadPrimitiveValue(buf, ref pos, f.stackType);
				break;
			case ProxyFieldType.CilboxRef:
			case ProxyFieldType.ObjectRef:
				f.fieldObjectIndex = BinaryHelper.ReadInt(buf, ref pos);
				f.objectRefIsNull = BinaryHelper.ReadBool(buf, ref pos);
				break;
			case ProxyFieldType.Array:
				f.elementType = SerializedTypeDescriptor.Read(buf, ref pos);
				f.arrayElements = BinaryHelper.ReadArray(buf, ref pos, Read);
				break;
			case ProxyFieldType.Json:
				f.elementType = SerializedTypeDescriptor.Read(buf, ref pos);
				f.data = BinaryHelper.ReadString(buf, ref pos);
				break;
			}
			return f;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteByte(buf, fieldType);
			BinaryHelper.WriteBool(buf, fieldName != null);
			if (fieldName != null)
				BinaryHelper.WriteString(buf, fieldName);
			BinaryHelper.WriteInt(buf, matchingInstanceId);

			switch ((ProxyFieldType)fieldType)
			{
			case ProxyFieldType.Empty:
				break;
			case ProxyFieldType.String:
				BinaryHelper.WriteString(buf, data);
				break;
			case ProxyFieldType.Primitive:
				BinaryHelper.WriteByte(buf, stackType);
				WritePrimitiveValue(buf, primitiveValue, stackType);
				break;
			case ProxyFieldType.CilboxRef:
			case ProxyFieldType.ObjectRef:
				BinaryHelper.WriteInt(buf, fieldObjectIndex);
				BinaryHelper.WriteBool(buf, objectRefIsNull);
				break;
			case ProxyFieldType.Array:
				elementType.Write(buf);
				BinaryHelper.WriteArray(buf, arrayElements, e => e.Write(buf));
				break;
			case ProxyFieldType.Json:
				elementType.Write(buf);
				BinaryHelper.WriteString(buf, data);
				break;
			}
		}
	}

	public class SerializedProxy
	{
		public const byte FormatVersion = 1;
		public SerializedProxyField[] fields;

		public static SerializedProxy Read(byte[] buf, ref int pos)
		{
			var p = new SerializedProxy();
			p.fields = BinaryHelper.ReadArray(buf, ref pos, SerializedProxyField.Read);
			return p;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteArray(buf, fields, e => e.Write(buf));
		}

		public byte[] ToBytes() => BinaryHelper.CompressToBytes(FormatVersion, Write);

		public static SerializedProxy FromBytes(byte[] data) =>
			BinaryHelper.DecompressAndRead(data, FormatVersion, "proxy", Read);
	}
}
