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

		// ── Token-based field serialization ──
		// Format per field: [token:byte] [length:int32] [payload:bytes...]
		// End of struct:    [0x00]

		public const byte EndOfStruct = 0;

		/// <summary>
		/// Writes a length-prefixed field: token byte, int32 payload length, then payload.
		/// </summary>
		public static void WriteField(List<byte> buf, byte token, Action<List<byte>> writePayload)
		{
			buf.Add(token);
			int lengthPos = buf.Count;
			buf.Add(0); buf.Add(0); buf.Add(0); buf.Add(0); // placeholder length
			int dataStart = buf.Count;
			writePayload(buf);
			int dataLen = buf.Count - dataStart;
			Span<byte> tmp = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(tmp, dataLen);
			buf[lengthPos] = tmp[0];
			buf[lengthPos + 1] = tmp[1];
			buf[lengthPos + 2] = tmp[2];
			buf[lengthPos + 3] = tmp[3];
		}

		public static void WriteEndStruct(List<byte> buf) => buf.Add(EndOfStruct);

		// ── WriteField convenience overloads (avoid lambda allocation for common types) ──

		public static void WriteFieldString(List<byte> buf, byte token, string value)
		{
			WriteField(buf, token, b => WriteString(b, value));
		}

		public static void WriteFieldInt(List<byte> buf, byte token, int value)
		{
			WriteField(buf, token, b => WriteInt(b, value));
		}

		public static void WriteFieldBool(List<byte> buf, byte token, bool value)
		{
			WriteField(buf, token, b => WriteBool(b, value));
		}

		public static void WriteFieldByte(List<byte> buf, byte token, byte value)
		{
			WriteField(buf, token, b => WriteByte(b, value));
		}

		public static void WriteFieldLong(List<byte> buf, byte token, long value)
		{
			WriteField(buf, token, b => WriteLong(b, value));
		}

		public static void WriteFieldBlob(List<byte> buf, byte token, byte[] value)
		{
			WriteField(buf, token, b => WriteBlob(b, value));
		}

		/// <summary>
		/// Writes fields via the callback, then automatically appends EndOfStruct.
		/// </summary>
		public static void WriteStruct(List<byte> buf, Action<List<byte>> writeFields)
		{
			writeFields(buf);
			WriteEndStruct(buf);
		}

		// ── ReadFields helper (encapsulates the token loop) ──

		public delegate void FieldReader(byte token, byte[] buf, ref int pos);

		/// <summary>
		/// Reads fields in a token loop until EndOfStruct. For each field, calls the handler
		/// with the token, buffer, and position. Automatically skips to fieldEnd after each field.
		/// </summary>
		public static void ReadFields(byte[] buf, ref int pos, FieldReader handler)
		{
			while (true)
			{
				byte token = ReadFieldToken(buf, ref pos);
				if (token == EndOfStruct) break;
				int fieldEnd = ReadFieldLength(buf, ref pos);
				handler(token, buf, ref pos);
				pos = fieldEnd;
			}
		}

		/// <summary>
		/// Reads the next field token. Returns EndOfStruct (0) at struct boundary.
		/// </summary>
		public static byte ReadFieldToken(byte[] buf, ref int pos) => buf[pos++];

		/// <summary>
		/// Reads the int32 field payload length and returns the end position of the field.
		/// The caller reads the payload, then sets pos = fieldEnd to ensure alignment.
		/// </summary>
		public static int ReadFieldLength(byte[] buf, ref int pos)
		{
			int len = ReadInt(buf, ref pos);
			return pos + len;
		}

		/// <summary>
		/// Skips an unknown field by reading its length prefix and advancing past the payload.
		/// </summary>
		public static void SkipField(byte[] buf, ref int pos)
		{
			int len = ReadInt(buf, ref pos);
			pos += len;
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
		// Generic fields (present when genericArgs token written)
		public string genericName;
		public SerializedTypeDescriptor[] genericArgs;
		// Underlying type (present when underlyingType token written)
		public SerializedTypeDescriptor underlyingType;

		public bool IsGeneric => genericArgs != null && genericArgs.Length > 0;
		public bool HasUnderlyingType => underlyingType != null;

		// Field tokens
		const byte T_AssemblyName = 1;
		const byte T_TypeName = 2;
		const byte T_GenericName = 3;
		const byte T_GenericArgs = 4;
		const byte T_UnderlyingType = 5;

		public static SerializedTypeDescriptor Read(byte[] buf, ref int pos)
		{
			var td = new SerializedTypeDescriptor();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_AssemblyName: td.assemblyName = BinaryHelper.ReadString(b, ref p); break;
					case T_TypeName: td.typeName = BinaryHelper.ReadString(b, ref p); break;
					case T_GenericName: td.genericName = BinaryHelper.ReadString(b, ref p); break;
					case T_GenericArgs: td.genericArgs = BinaryHelper.ReadArray(b, ref p, Read); break;
					case T_UnderlyingType: td.underlyingType = Read(b, ref p); break;
				}
			});
			return td;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_AssemblyName, assemblyName);
				BinaryHelper.WriteFieldString(b, T_TypeName, typeName);
				if (IsGeneric)
				{
					BinaryHelper.WriteFieldString(b, T_GenericName, genericName);
					BinaryHelper.WriteField(b, T_GenericArgs, b2 => BinaryHelper.WriteArray(b2, genericArgs, e => e.Write(b2)));
				}
				if (HasUnderlyingType)
				{
					BinaryHelper.WriteField(b, T_UnderlyingType, b2 => underlyingType.Write(b2));
				}
			});
		}

	}

	public class SerializedField
	{
		public string name;
		public SerializedTypeDescriptor type;

		const byte T_Name = 1;
		const byte T_Type = 2;

		public static SerializedField Read(byte[] buf, ref int pos)
		{
			var f = new SerializedField();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_Name: f.name = BinaryHelper.ReadString(b, ref p); break;
					case T_Type: f.type = SerializedTypeDescriptor.Read(b, ref p); break;
				}
			});
			return f;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_Name, name);
				BinaryHelper.WriteField(b, T_Type, b2 => type.Write(b2));
			});
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

		const byte T_Flags = 1;
		const byte T_TryOffset = 2;
		const byte T_TryLength = 3;
		const byte T_HandlerOffset = 4;
		const byte T_HandlerLength = 5;
		const byte T_CatchType = 6; // presence implies hasCatchType=true

		public static SerializedExceptionHandler Read(byte[] buf, ref int pos)
		{
			var eh = new SerializedExceptionHandler();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_Flags: eh.flags = BinaryHelper.ReadInt(b, ref p); break;
					case T_TryOffset: eh.tryOffset = BinaryHelper.ReadInt(b, ref p); break;
					case T_TryLength: eh.tryLength = BinaryHelper.ReadInt(b, ref p); break;
					case T_HandlerOffset: eh.handlerOffset = BinaryHelper.ReadInt(b, ref p); break;
					case T_HandlerLength: eh.handlerLength = BinaryHelper.ReadInt(b, ref p); break;
					case T_CatchType:
						eh.hasCatchType = true;
						eh.catchType = SerializedTypeDescriptor.Read(b, ref p);
						break;
				}
			});
			return eh;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldInt(b, T_Flags, flags);
				BinaryHelper.WriteFieldInt(b, T_TryOffset, tryOffset);
				BinaryHelper.WriteFieldInt(b, T_TryLength, tryLength);
				BinaryHelper.WriteFieldInt(b, T_HandlerOffset, handlerOffset);
				BinaryHelper.WriteFieldInt(b, T_HandlerLength, handlerLength);
				if (hasCatchType)
					BinaryHelper.WriteField(b, T_CatchType, b2 => catchType.Write(b2));
			});
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

		const byte T_MethodName = 1;
		const byte T_MaxStack = 2;
		const byte T_Flags = 3; // packed: isVoid|isStatic|isCtor
		const byte T_FullSignature = 4;
		const byte T_Body = 5;
		const byte T_Locals = 6;
		const byte T_Parameters = 7;
		const byte T_ExceptionHandlers = 8;

		public static SerializedMethod Read(byte[] buf, ref int pos)
		{
			var m = new SerializedMethod();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_MethodName: m.methodName = BinaryHelper.ReadString(b, ref p); break;
					case T_MaxStack: m.maxStack = BinaryHelper.ReadInt(b, ref p); break;
					case T_Flags:
						byte flags = BinaryHelper.ReadByte(b, ref p);
						m.isVoid = (flags & 1) != 0;
						m.isStatic = (flags & 2) != 0;
						m.isCtor = (flags & 4) != 0;
						break;
					case T_FullSignature: m.fullSignature = BinaryHelper.ReadString(b, ref p); break;
					case T_Body: m.body = BinaryHelper.ReadBlob(b, ref p); break;
					case T_Locals: m.locals = BinaryHelper.ReadArray(b, ref p, SerializedField.Read); break;
					case T_Parameters: m.parameters = BinaryHelper.ReadArray(b, ref p, SerializedField.Read); break;
					case T_ExceptionHandlers:
						m.exceptionHandlers =
							BinaryHelper.ReadArray(b, ref p, SerializedExceptionHandler.Read); break;
				}
			});
			return m;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_MethodName, methodName);
				BinaryHelper.WriteFieldInt(b, T_MaxStack, maxStack);
				BinaryHelper.WriteField(b, T_Flags, b2 =>
				{
					byte flags = 0;
					if (isVoid) flags |= 1;
					if (isStatic) flags |= 2;
					if (isCtor) flags |= 4;
					BinaryHelper.WriteByte(b2, flags);
				});
				BinaryHelper.WriteFieldString(b, T_FullSignature, fullSignature);
				BinaryHelper.WriteFieldBlob(b, T_Body, body);
				BinaryHelper.WriteField(b, T_Locals, b2 => BinaryHelper.WriteArray(b2, locals, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_Parameters, b2 => BinaryHelper.WriteArray(b2, parameters, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_ExceptionHandlers,
					b2 => BinaryHelper.WriteArray(b2, exceptionHandlers, e => e.Write(b2)));
			});
		}
	}

	public class SerializedClass
	{
		public string className;
		public SerializedField[] staticFields;
		public SerializedField[] instanceFields;
		public SerializedMethod[] methods;

		const byte T_ClassName = 1;
		const byte T_StaticFields = 2;
		const byte T_InstanceFields = 3;
		const byte T_Methods = 4;

		public static SerializedClass Read(byte[] buf, ref int pos)
		{
			var c = new SerializedClass();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_ClassName: c.className = BinaryHelper.ReadString(b, ref p); break;
					case T_StaticFields: c.staticFields = BinaryHelper.ReadArray(b, ref p, SerializedField.Read); break;
					case T_InstanceFields: c.instanceFields = BinaryHelper.ReadArray(b, ref p, SerializedField.Read); break;
					case T_Methods: c.methods = BinaryHelper.ReadArray(b, ref p, SerializedMethod.Read); break;
				}
			});
			return c;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_ClassName, className);
				BinaryHelper.WriteField(b, T_StaticFields, b2 => BinaryHelper.WriteArray(b2, staticFields, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_InstanceFields, b2 => BinaryHelper.WriteArray(b2, instanceFields, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_Methods, b2 => BinaryHelper.WriteArray(b2, methods, e => e.Write(b2)));
			});
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

		const byte T_MetaTokenType = 1;
		const byte T_StringValue = 2;
		const byte T_ArrayInitData = 3;
		const byte T_FieldDeclaringType = 4;
		const byte T_FieldName = 5;
		const byte T_FieldIsStatic = 6;
		const byte T_FieldIndex = 7; // presence implies fieldHasIndex=true
		const byte T_TypeDescriptor = 8;
		const byte T_MethodDeclaringType = 9;
		const byte T_MethodName = 10;
		const byte T_MethodFullSignature = 11;
		const byte T_MethodIsStatic = 12;
		const byte T_MethodAssembly = 13;
		const byte T_MethodParameters = 14;
		const byte T_MethodGenericArguments = 15;

		public static SerializedMetadataToken Read(byte[] buf, ref int pos)
		{
			var t = new SerializedMetadataToken();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_MetaTokenType: t.metaTokenType = BinaryHelper.ReadByte(b, ref p); break;
					case T_StringValue: t.stringValue = BinaryHelper.ReadString(b, ref p); break;
					case T_ArrayInitData: t.arrayInitData = BinaryHelper.ReadBlob(b, ref p); break;
					case T_FieldDeclaringType: t.fieldDeclaringType = SerializedTypeDescriptor.Read(b, ref p); break;
					case T_FieldName: t.fieldName = BinaryHelper.ReadString(b, ref p); break;
					case T_FieldIsStatic: t.fieldIsStatic = BinaryHelper.ReadBool(b, ref p); break;
					case T_FieldIndex:
						t.fieldHasIndex = true;
						t.fieldIndex = BinaryHelper.ReadInt(b, ref p);
						break;
					case T_TypeDescriptor: t.typeDescriptor = SerializedTypeDescriptor.Read(b, ref p); break;
					case T_MethodDeclaringType: t.methodDeclaringType = SerializedTypeDescriptor.Read(b, ref p); break;
					case T_MethodName: t.methodName = BinaryHelper.ReadString(b, ref p); break;
					case T_MethodFullSignature: t.methodFullSignature = BinaryHelper.ReadString(b, ref p); break;
					case T_MethodIsStatic: t.methodIsStatic = BinaryHelper.ReadBool(b, ref p); break;
					case T_MethodAssembly: t.methodAssembly = BinaryHelper.ReadString(b, ref p); break;
					case T_MethodParameters: t.methodParameters = BinaryHelper.ReadArray(b, ref p, SerializedTypeDescriptor.Read); break;
					case T_MethodGenericArguments: t.methodGenericArguments = BinaryHelper.ReadArray(b, ref p, SerializedTypeDescriptor.Read); break;
				}
			});
			return t;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldByte(b, T_MetaTokenType, metaTokenType);
				MetaTokenType mt = (MetaTokenType)metaTokenType;

				switch (mt)
				{
					case MetaTokenType.mtString:
						BinaryHelper.WriteFieldString(b, T_StringValue, stringValue);
						break;
					case MetaTokenType.mtArrayInitializer:
						BinaryHelper.WriteFieldBlob(b, T_ArrayInitData, arrayInitData);
						break;
					case MetaTokenType.mtField:
						BinaryHelper.WriteField(b, T_FieldDeclaringType, b2 => fieldDeclaringType.Write(b2));
						BinaryHelper.WriteFieldString(b, T_FieldName, fieldName);
						BinaryHelper.WriteFieldBool(b, T_FieldIsStatic, fieldIsStatic);
						if (fieldHasIndex)
							BinaryHelper.WriteFieldInt(b, T_FieldIndex, fieldIndex);
						break;
					case MetaTokenType.mtType:
						BinaryHelper.WriteField(b, T_TypeDescriptor, b2 => typeDescriptor.Write(b2));
						break;
					case MetaTokenType.mtMethod:
						BinaryHelper.WriteField(b, T_MethodDeclaringType, b2 => methodDeclaringType.Write(b2));
						BinaryHelper.WriteFieldString(b, T_MethodName, methodName);
						BinaryHelper.WriteFieldString(b, T_MethodFullSignature, methodFullSignature);
						BinaryHelper.WriteFieldBool(b, T_MethodIsStatic, methodIsStatic);
						BinaryHelper.WriteFieldString(b, T_MethodAssembly, methodAssembly);
						BinaryHelper.WriteField(b, T_MethodParameters,
							b2 => BinaryHelper.WriteArray(b2, methodParameters ?? Array.Empty<SerializedTypeDescriptor>(),
								e => e.Write(b2)));
						BinaryHelper.WriteField(b, T_MethodGenericArguments,
							b2 => BinaryHelper.WriteArray(b2,
								methodGenericArguments ?? Array.Empty<SerializedTypeDescriptor>(), e => e.Write(b2)));
						break;
				}
			});
		}
	}

	public class SerializedEnumValue
	{
		public string name;
		public long value;

		const byte T_Name = 1;
		const byte T_Value = 2;

		public static SerializedEnumValue Read(byte[] buf, ref int pos)
		{
			var v = new SerializedEnumValue();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_Name: v.name = BinaryHelper.ReadString(b, ref p); break;
					case T_Value: v.value = BinaryHelper.ReadLong(b, ref p); break;
				}
			});
			return v;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_Name, name);
				BinaryHelper.WriteFieldLong(b, T_Value, value);
			});
		}
	}

	public class SerializedEnum
	{
		public string enumName;
		public SerializedTypeDescriptor underlyingType;
		public SerializedEnumValue[] values;

		const byte T_EnumName = 1;
		const byte T_UnderlyingType = 2;
		const byte T_Values = 3;

		public static SerializedEnum Read(byte[] buf, ref int pos)
		{
			var e = new SerializedEnum();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_EnumName: e.enumName = BinaryHelper.ReadString(b, ref p); break;
					case T_UnderlyingType: e.underlyingType = SerializedTypeDescriptor.Read(b, ref p); break;
					case T_Values: e.values = BinaryHelper.ReadArray(b, ref p, SerializedEnumValue.Read); break;
				}
			});
			return e;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldString(b, T_EnumName, enumName);
				BinaryHelper.WriteField(b, T_UnderlyingType, b2 => underlyingType.Write(b2));
				BinaryHelper.WriteField(b, T_Values, b2 => BinaryHelper.WriteArray(b2, values, e => e.Write(b2)));
			});
		}
	}

	public class SerializedAssembly
	{
		public const byte FormatVersion = 1;

		public SerializedClass[] classes;
		public SerializedMetadataToken[] metadata; // index 0 is invalid/empty sentinel
		public SerializedEnum[] enums;

		const byte T_Classes = 1;
		const byte T_Metadata = 2;
		const byte T_Enums = 3;

		public static SerializedAssembly Read(byte[] buf, ref int pos)
		{
			var asm = new SerializedAssembly();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_Classes: asm.classes = BinaryHelper.ReadArray(b, ref p, SerializedClass.Read); break;
					case T_Metadata: asm.metadata = BinaryHelper.ReadArray(b, ref p, SerializedMetadataToken.Read); break;
					case T_Enums: asm.enums = BinaryHelper.ReadArray(b, ref p, SerializedEnum.Read); break;
				}
			});
			return asm;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteField(b, T_Classes, b2 => BinaryHelper.WriteArray(b2, classes, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_Metadata, b2 => BinaryHelper.WriteArray(b2, metadata, e => e.Write(b2)));
				BinaryHelper.WriteField(b, T_Enums, b2 => BinaryHelper.WriteArray(b2, enums, e => e.Write(b2)));
			});
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

		const byte T_FieldType = 1;
		const byte T_FieldName = 2;
		const byte T_MatchingInstanceId = 3;
		const byte T_Data = 4;				// String / Json
		const byte T_PrimitivePayload = 5;	// stackType byte + primitive value bytes
		const byte T_FieldObjectIndex = 6;	// CilboxRef / ObjectRef
		const byte T_ObjectRefIsNull = 7;	// CilboxRef / ObjectRef
		const byte T_ElementType = 8;		// Array / Json
		const byte T_ArrayElements = 9;		// Array

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
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int p)
			{
				switch (token)
				{
					case T_FieldType: f.fieldType = BinaryHelper.ReadByte(b, ref p); break;
					case T_FieldName: f.fieldName = BinaryHelper.ReadString(b, ref p); break;
					case T_MatchingInstanceId: f.matchingInstanceId = BinaryHelper.ReadInt(b, ref p); break;
					case T_Data: f.data = BinaryHelper.ReadString(b, ref p); break;
					case T_PrimitivePayload:
						f.stackType = BinaryHelper.ReadByte(b, ref p);
						f.primitiveValue = ReadPrimitiveValue(b, ref p, f.stackType);
						break;
					case T_FieldObjectIndex: f.fieldObjectIndex = BinaryHelper.ReadInt(b, ref p); break;
					case T_ObjectRefIsNull: f.objectRefIsNull = BinaryHelper.ReadBool(b, ref p); break;
					case T_ElementType: f.elementType = SerializedTypeDescriptor.Read(b, ref p); break;
					case T_ArrayElements: f.arrayElements = BinaryHelper.ReadArray(b, ref p, Read); break;
				}
			});
			return f;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteFieldByte(b, T_FieldType, fieldType);
				if (fieldName != null)
					BinaryHelper.WriteFieldString(b, T_FieldName, fieldName);
				BinaryHelper.WriteFieldInt(b, T_MatchingInstanceId, matchingInstanceId);

				switch ((ProxyFieldType)fieldType)
				{
					case ProxyFieldType.Empty:
						break;
					case ProxyFieldType.String:
						BinaryHelper.WriteFieldString(b, T_Data, data);
						break;
					case ProxyFieldType.Primitive:
						BinaryHelper.WriteField(b, T_PrimitivePayload, b2 =>
						{
							BinaryHelper.WriteByte(b2, stackType);
							WritePrimitiveValue(b2, primitiveValue, stackType);
						});
						break;
					case ProxyFieldType.CilboxRef:
					case ProxyFieldType.ObjectRef:
						BinaryHelper.WriteFieldInt(b, T_FieldObjectIndex, fieldObjectIndex);
						BinaryHelper.WriteFieldBool(b, T_ObjectRefIsNull, objectRefIsNull);
						break;
					case ProxyFieldType.Array:
						BinaryHelper.WriteField(b, T_ElementType, b2 => elementType.Write(b2));
						BinaryHelper.WriteField(b, T_ArrayElements, b2 => BinaryHelper.WriteArray(b2, arrayElements, e => e.Write(b2)));
						break;
					case ProxyFieldType.Json:
						BinaryHelper.WriteField(b, T_ElementType, b2 => elementType.Write(b2));
						BinaryHelper.WriteFieldString(b, T_Data, data);
						break;
				}
			});
		}
	}

	public class SerializedProxy
	{
		public const byte FormatVersion = 1;
		public SerializedProxyField[] fields;

		const byte T_Fields = 1;

		public static SerializedProxy Read(byte[] buf, ref int pos)
		{
			var p = new SerializedProxy();
			BinaryHelper.ReadFields(buf, ref pos, delegate(byte token, byte[] b, ref int pp)
			{
				switch (token)
				{
					case T_Fields: p.fields = BinaryHelper.ReadArray(b, ref pp, SerializedProxyField.Read); break;
				}
			});
			return p;
		}

		public void Write(List<byte> buf)
		{
			BinaryHelper.WriteStruct(buf, b =>
			{
				BinaryHelper.WriteField(b, T_Fields, b2 => BinaryHelper.WriteArray(b2, fields, e => e.Write(b2)));
			});
		}

		public byte[] ToBytes() => BinaryHelper.CompressToBytes(FormatVersion, Write);

		public static SerializedProxy FromBytes(byte[] data) =>
			BinaryHelper.DecompressAndRead(data, FormatVersion, "proxy", Read);
	}
}
