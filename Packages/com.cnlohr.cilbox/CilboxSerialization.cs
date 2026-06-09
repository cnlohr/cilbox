using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace Cilbox
{
	/// <summary>
	/// Mirrors the wire layout of a single type-pool entry. Captured during
	/// <see cref="WriteContext.InternType"/> so <see cref="BinaryHelper.CompressToBytesPooled"/>
	/// can dump entries straight to the wire without re-walking child TDs.
	/// </summary>
	public struct TypePoolEntry
	{
		public int asmIdx;
		public int typeIdx;
		public int genIdx;
		public int[] argIdx; // null when not generic
		public int underIdx;
	}

	/// <summary>
	/// Write-side context for the deflated payload. Carries the byte buffer being
	/// appended to plus the deduplicating string and type pools. Strings written via
	/// the pooled helpers are emitted as 7-bit-encoded indices into <see cref="poolOrder"/>;
	/// TD references are emitted as 7-bit-encoded indices into <see cref="typeOrder"/>.
	/// Both pools are serialized once at the head of the deflated payload by
	/// <see cref="BinaryHelper.CompressToBytesPooled"/>.
	/// </summary>
	public class WriteContext
	{
		public List<byte> body = new List<byte>();
		public Dictionary<string, int> poolIndex = new Dictionary<string, int>();
		public List<string> poolOrder = new List<string>(); // index 0 reserved for null

		// Type pool. typeOrder[0] is the null sentinel; typePoolEntries[0] is the
		// all-zero entry mirroring that slot on the wire.
		public Dictionary<string, int> typeKeyIndex = new Dictionary<string, int>();
		public List<SerializedTypeDescriptor> typeOrder = new List<SerializedTypeDescriptor>();
		public List<TypePoolEntry> typePoolEntries = new List<TypePoolEntry>();

		public WriteContext()
		{
			poolOrder.Add(null); // slot 0 is the null sentinel
			typeOrder.Add(null);
			typePoolEntries.Add(default); // all-zero entry
		}

		public int InternString(string s)
		{
			if (s == null) return 0;
			if (poolIndex.TryGetValue(s, out int idx)) return idx;
			idx = poolOrder.Count;
			poolOrder.Add(s);
			poolIndex[s] = idx;
			return idx;
		}

		/// <summary>
		/// Interns a type descriptor and returns its pool index. Recurses into
		/// children first so the resulting wire layout is topologically ordered:
		/// every child's index is strictly less than its parent's.
		/// </summary>
		public int InternType(SerializedTypeDescriptor td)
		{
			if (td == null) return 0;

			// Recurse into children first so child indices are assigned before the parent.
			int underIdx = td.HasUnderlyingType ? InternType(td.underlyingType) : 0;
			int[] argIdx = null;
			if (td.IsGeneric)
			{
				argIdx = new int[td.genericArgs.Length];
				for (int i = 0; i < td.genericArgs.Length; i++)
					argIdx[i] = InternType(td.genericArgs[i]);
			}

			int asmIdx = InternString(td.assemblyName);
			int typeIdx = InternString(td.typeName);
			int genIdx = td.IsGeneric ? InternString(td.genericName) : 0;

			// Build a stable key from the resolved integer indices. Allocates a
			// string per intern lookup; acceptable since write-side intern is not
			// the runtime hot path.
			var sb = new StringBuilder();
			sb.Append(asmIdx).Append('|');
			sb.Append(typeIdx).Append('|');
			sb.Append(genIdx).Append('|');
			sb.Append(underIdx).Append('|');
			if (argIdx != null)
			{
				for (int i = 0; i < argIdx.Length; i++)
				{
					if (i > 0) sb.Append(',');
					sb.Append(argIdx[i]);
				}
			}
			string key = sb.ToString();

			if (typeKeyIndex.TryGetValue(key, out int idx)) return idx;
			idx = typeOrder.Count;
			typeOrder.Add(td);
			typePoolEntries.Add(new TypePoolEntry
			{
				asmIdx = asmIdx,
				typeIdx = typeIdx,
				genIdx = genIdx,
				argIdx = argIdx,
				underIdx = underIdx,
			});
			typeKeyIndex[key] = idx;
			return idx;
		}
	}

	/// <summary>
	/// Read-side context. Carries the deflated payload buffer plus the decoded
	/// string and type pools. Passed by ref to every Serialized*.Read method.
	/// Cannot be boxed or used as a generic type argument — only as a ref parameter.
	/// </summary>
	public ref struct ReadContext
	{
		public byte[] buf;
		public int pos;
		public string[] strings;
		public SerializedTypeDescriptor[] types;
	}

	/// <summary>
	/// Similar to BinaryReady/BinaryWriter but uses stack-allocated buffers to avoid heap allocations.
	/// </summary>
	public static class BinaryHelper
	{
		public static void Write7BitEncodedInt(List<byte> buf, int value)
		{
			uint v = (uint)value;
			while (v >= 0x80)
			{
				buf.Add((byte)(v | 0x80));
				v >>= 7;
			}
			buf.Add((byte)v);
		}

		public static int Read7BitEncodedInt(byte[] buf, ref int pos)
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

		public static string ReadPooledString(byte[] buf, ref int pos, string[] pool)
			=> pool[Read7BitEncodedInt(buf, ref pos)];

		public static string ReadPooledString(ref ReadContext ctx)
			=> ctx.strings[Read7BitEncodedInt(ctx.buf, ref ctx.pos)];

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

		public static void WritePooledString(WriteContext ctx, string s)
		{
			Write7BitEncodedInt(ctx.body, ctx.InternString(s));
		}

		public static void WritePooledType(WriteContext ctx, SerializedTypeDescriptor td)
		{
			Write7BitEncodedInt(ctx.body, ctx.InternType(td));
		}

		public static SerializedTypeDescriptor ReadPooledType(ref ReadContext ctx)
			=> ctx.types[Read7BitEncodedInt(ctx.buf, ref ctx.pos)];

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

		public delegate T ReadFunc<T>(ref ReadContext ctx);

		public static T[] ReadArray<T>(ref ReadContext ctx, ReadFunc<T> readElement)
		{
			int count = ReadInt(ctx.buf, ref ctx.pos);
			T[] arr = new T[count];
			for (int i = 0; i < count; i++)
				arr[i] = readElement(ref ctx);
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

		/// <summary>
		/// Context-aware variant of <see cref="WriteField(List{byte},byte,Action{List{byte}})"/>
		/// for payloads that need access to the string pool.
		/// </summary>
		public static void WriteField(WriteContext ctx, byte token, Action<WriteContext> writePayload)
		{
			List<byte> buf = ctx.body;
			buf.Add(token);
			int lengthPos = buf.Count;
			buf.Add(0); buf.Add(0); buf.Add(0); buf.Add(0); // placeholder length
			int dataStart = buf.Count;
			writePayload(ctx);
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

		public static void WriteFieldPooledString(WriteContext ctx, byte token, string value)
		{
			WriteField(ctx, token, c => WritePooledString(c, value));
		}

		/// <summary>
		/// Emits a TLV field whose payload is a 7-bit-encoded index into the type pool.
		/// Inlines the field header to avoid closure allocation per reference site.
		/// </summary>
		public static void WriteFieldPooledType(WriteContext ctx, byte token, SerializedTypeDescriptor td)
		{
			int idx = ctx.InternType(td);
			List<byte> buf = ctx.body;
			buf.Add(token);
			int lengthPos = buf.Count;
			buf.Add(0); buf.Add(0); buf.Add(0); buf.Add(0);
			int dataStart = buf.Count;
			Write7BitEncodedInt(buf, idx);
			int dataLen = buf.Count - dataStart;
			Span<byte> tmp = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(tmp, dataLen);
			buf[lengthPos] = tmp[0];
			buf[lengthPos + 1] = tmp[1];
			buf[lengthPos + 2] = tmp[2];
			buf[lengthPos + 3] = tmp[3];
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

		/// <summary>
		/// Context-aware variant of <see cref="WriteStruct(List{byte},Action{List{byte}})"/>.
		/// </summary>
		public static void WriteStruct(WriteContext ctx, Action<WriteContext> writeFields)
		{
			writeFields(ctx);
			WriteEndStruct(ctx.body);
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

		public static byte[] CompressToBytesPooled(byte version, Action<WriteContext> writePayload)
		{
			WriteContext ctx = new WriteContext();
			writePayload(ctx);

			// Assemble: [stringPoolCount][stringPool entries...][typePoolCount][typePool entries...][body bytes]
			// poolOrder[0] is the null sentinel; written as "" on the wire and
			// restored to null on read. typeOrder[0] is the null sentinel; written
			// as an all-zero entry on the wire and restored to null on read.
			List<byte> uncompressed = new List<byte>(ctx.body.Count + 64);

			// String pool
			Write7BitEncodedInt(uncompressed, ctx.poolOrder.Count);
			for (int i = 0; i < ctx.poolOrder.Count; i++)
				WriteString(uncompressed, ctx.poolOrder[i] ?? "");

			// Type pool — entries are emitted in topological order (children
			// before parents), naturally produced by InternType's recursion.
			Write7BitEncodedInt(uncompressed, ctx.typeOrder.Count);
			for (int i = 0; i < ctx.typeOrder.Count; i++)
			{
				var e = ctx.typePoolEntries[i];
				Write7BitEncodedInt(uncompressed, e.asmIdx);
				Write7BitEncodedInt(uncompressed, e.typeIdx);
				Write7BitEncodedInt(uncompressed, e.genIdx);
				int argCount = e.argIdx?.Length ?? 0;
				Write7BitEncodedInt(uncompressed, argCount);
				for (int j = 0; j < argCount; j++)
					Write7BitEncodedInt(uncompressed, e.argIdx[j]);
				Write7BitEncodedInt(uncompressed, e.underIdx);
			}

			uncompressed.AddRange(ctx.body);
			byte[] uncompressedArr = uncompressed.ToArray();

			byte[] compressed;
			using (var ms = new MemoryStream())
			{
				using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
					deflate.Write(uncompressedArr, 0, uncompressedArr.Length);
				compressed = ms.ToArray();
			}

			List<byte> buf = new List<byte>(1 + 4 + compressed.Length);
			WriteByte(buf, version);
			WriteInt(buf, uncompressedArr.Length);
			buf.AddRange(compressed);
			return buf.ToArray();
		}

		public static T DecompressAndReadPooled<T>(byte[] data, byte expectedVersion, string formatName,
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
			int poolCount = Read7BitEncodedInt(uncompressed, ref payloadpos);
			string[] pool = new string[poolCount];
			for (int i = 0; i < poolCount; i++)
				pool[i] = ReadString(uncompressed, ref payloadpos);
			// Slot 0 is the null sentinel; written as "" on the wire.
			pool[0] = null;

			int typeCount = Read7BitEncodedInt(uncompressed, ref payloadpos);
			SerializedTypeDescriptor[] types = new SerializedTypeDescriptor[typeCount];
			// Each entry's children always have lower indices, so a single linear
			// pass suffices. Slot 0 is read like the others (its all-zero indices
			// dereference null strings) and then overridden to null afterward.
			for (int i = 0; i < typeCount; i++)
				types[i] = SerializedTypeDescriptor.ReadPoolEntry(uncompressed, ref payloadpos, pool, types);
			types[0] = null;

			ReadContext ctx = new ReadContext { buf = uncompressed, pos = payloadpos, strings = pool, types = types };
			return readPayload(ref ctx);
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

		/// <summary>
		/// Reconstructs a single TD from one type-pool entry, looking children up
		/// in the already-resolved <paramref name="typesSoFar"/> array. Used by
		/// <see cref="BinaryHelper.DecompressAndReadPooled{T}"/> while populating
		/// the type pool.
		/// </summary>
		public static SerializedTypeDescriptor ReadPoolEntry(byte[] buf, ref int pos, string[] strings, SerializedTypeDescriptor[] typesSoFar)
		{
			int asmIdx = BinaryHelper.Read7BitEncodedInt(buf, ref pos);
			int typeIdx = BinaryHelper.Read7BitEncodedInt(buf, ref pos);
			int genIdx = BinaryHelper.Read7BitEncodedInt(buf, ref pos);
			int argCount = BinaryHelper.Read7BitEncodedInt(buf, ref pos);
			SerializedTypeDescriptor[] args = null;
			if (argCount > 0)
			{
				args = new SerializedTypeDescriptor[argCount];
				for (int i = 0; i < argCount; i++)
					args[i] = typesSoFar[BinaryHelper.Read7BitEncodedInt(buf, ref pos)];
			}
			int underIdx = BinaryHelper.Read7BitEncodedInt(buf, ref pos);

			var td = new SerializedTypeDescriptor();
			td.assemblyName = strings[asmIdx];
			td.typeName = strings[typeIdx];
			if (genIdx != 0)
				td.genericName = strings[genIdx];
			// argCount == 0 ⇒ genericArgs stays null, preserving IsGeneric semantics.
			td.genericArgs = args;
			if (underIdx != 0)
				td.underlyingType = typesSoFar[underIdx];
			return td;
		}
	}

	public class SerializedField
	{
		public string name;
		public SerializedTypeDescriptor type;

		const byte T_Name = 1;
		const byte T_Type = 2;

		public static readonly BinaryHelper.ReadFunc<SerializedField> ReadDelegate = Read;

		public static SerializedField Read(ref ReadContext ctx)
		{
			var f = new SerializedField();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_Name: f.name = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_Type: f.type = BinaryHelper.ReadPooledType(ref ctx); break;
				}
				ctx.pos = fieldEnd;
			}
			return f;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldPooledString(c, T_Name, name);
				BinaryHelper.WriteFieldPooledType(c, T_Type, type);
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

		public static readonly BinaryHelper.ReadFunc<SerializedExceptionHandler> ReadDelegate = Read;

		public static SerializedExceptionHandler Read(ref ReadContext ctx)
		{
			var eh = new SerializedExceptionHandler();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_Flags: eh.flags = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_TryOffset: eh.tryOffset = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_TryLength: eh.tryLength = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_HandlerOffset: eh.handlerOffset = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_HandlerLength: eh.handlerLength = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_CatchType:
						eh.hasCatchType = true;
						eh.catchType = BinaryHelper.ReadPooledType(ref ctx);
						break;
				}
				ctx.pos = fieldEnd;
			}
			return eh;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldInt(c.body, T_Flags, flags);
				BinaryHelper.WriteFieldInt(c.body, T_TryOffset, tryOffset);
				BinaryHelper.WriteFieldInt(c.body, T_TryLength, tryLength);
				BinaryHelper.WriteFieldInt(c.body, T_HandlerOffset, handlerOffset);
				BinaryHelper.WriteFieldInt(c.body, T_HandlerLength, handlerLength);
				if (hasCatchType)
					BinaryHelper.WriteFieldPooledType(c, T_CatchType, catchType);
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

		public static readonly BinaryHelper.ReadFunc<SerializedMethod> ReadDelegate = Read;

		public static SerializedMethod Read(ref ReadContext ctx)
		{
			var m = new SerializedMethod();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_MethodName: m.methodName = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_MaxStack: m.maxStack = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_Flags:
						byte flags = BinaryHelper.ReadByte(ctx.buf, ref ctx.pos);
						m.isVoid = (flags & 1) != 0;
						m.isStatic = (flags & 2) != 0;
						m.isCtor = (flags & 4) != 0;
						break;
					case T_FullSignature: m.fullSignature = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_Body: m.body = BinaryHelper.ReadBlob(ctx.buf, ref ctx.pos); break;
					case T_Locals: m.locals = BinaryHelper.ReadArray(ref ctx, SerializedField.ReadDelegate); break;
					case T_Parameters: m.parameters = BinaryHelper.ReadArray(ref ctx, SerializedField.ReadDelegate); break;
					case T_ExceptionHandlers: m.exceptionHandlers = BinaryHelper.ReadArray(ref ctx, SerializedExceptionHandler.ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return m;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldPooledString(c, T_MethodName, methodName);
				BinaryHelper.WriteFieldInt(c.body, T_MaxStack, maxStack);
				BinaryHelper.WriteField(c.body, T_Flags, b2 =>
				{
					byte flags = 0;
					if (isVoid) flags |= 1;
					if (isStatic) flags |= 2;
					if (isCtor) flags |= 4;
					BinaryHelper.WriteByte(b2, flags);
				});
				BinaryHelper.WriteFieldPooledString(c, T_FullSignature, fullSignature);
				BinaryHelper.WriteFieldBlob(c.body, T_Body, body);
				BinaryHelper.WriteField(c, T_Locals, c2 => BinaryHelper.WriteArray(c2.body, locals, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_Parameters, c2 => BinaryHelper.WriteArray(c2.body, parameters, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_ExceptionHandlers,
					c2 => BinaryHelper.WriteArray(c2.body, exceptionHandlers, e => e.Write(c2)));
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

		public static readonly BinaryHelper.ReadFunc<SerializedClass> ReadDelegate = Read;

		public static SerializedClass Read(ref ReadContext ctx)
		{
			var c = new SerializedClass();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_ClassName: c.className = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_StaticFields: c.staticFields = BinaryHelper.ReadArray(ref ctx, SerializedField.ReadDelegate); break;
					case T_InstanceFields: c.instanceFields = BinaryHelper.ReadArray(ref ctx, SerializedField.ReadDelegate); break;
					case T_Methods: c.methods = BinaryHelper.ReadArray(ref ctx, SerializedMethod.ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return c;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldPooledString(c, T_ClassName, className);
				BinaryHelper.WriteField(c, T_StaticFields, c2 => BinaryHelper.WriteArray(c2.body, staticFields, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_InstanceFields, c2 => BinaryHelper.WriteArray(c2.body, instanceFields, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_Methods, c2 => BinaryHelper.WriteArray(c2.body, methods, e => e.Write(c2)));
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

		public static readonly BinaryHelper.ReadFunc<SerializedMetadataToken> ReadDelegate = Read;

		public static SerializedMetadataToken Read(ref ReadContext ctx)
		{
			var t = new SerializedMetadataToken();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_MetaTokenType: t.metaTokenType = BinaryHelper.ReadByte(ctx.buf, ref ctx.pos); break;
					case T_StringValue: t.stringValue = BinaryHelper.ReadString(ctx.buf, ref ctx.pos); break;
					case T_ArrayInitData: t.arrayInitData = BinaryHelper.ReadBlob(ctx.buf, ref ctx.pos); break;
					case T_FieldDeclaringType: t.fieldDeclaringType = BinaryHelper.ReadPooledType(ref ctx); break;
					case T_FieldName: t.fieldName = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_FieldIsStatic: t.fieldIsStatic = BinaryHelper.ReadBool(ctx.buf, ref ctx.pos); break;
					case T_FieldIndex:
						t.fieldHasIndex = true;
						t.fieldIndex = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos);
						break;
					case T_TypeDescriptor: t.typeDescriptor = BinaryHelper.ReadPooledType(ref ctx); break;
					case T_MethodDeclaringType: t.methodDeclaringType = BinaryHelper.ReadPooledType(ref ctx); break;
					case T_MethodName: t.methodName = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_MethodFullSignature: t.methodFullSignature = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_MethodIsStatic: t.methodIsStatic = BinaryHelper.ReadBool(ctx.buf, ref ctx.pos); break;
					case T_MethodAssembly: t.methodAssembly = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_MethodParameters:
					{
						int mpCount = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos);
						var arr = new SerializedTypeDescriptor[mpCount];
						for (int i = 0; i < mpCount; i++)
							arr[i] = BinaryHelper.ReadPooledType(ref ctx);
						t.methodParameters = arr;
						break;
					}
					case T_MethodGenericArguments:
					{
						int gaCount = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos);
						var arr = new SerializedTypeDescriptor[gaCount];
						for (int i = 0; i < gaCount; i++)
							arr[i] = BinaryHelper.ReadPooledType(ref ctx);
						t.methodGenericArguments = arr;
						break;
					}
				}
				ctx.pos = fieldEnd;
			}
			return t;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldByte(c.body, T_MetaTokenType, metaTokenType);
				MetaTokenType mt = (MetaTokenType)metaTokenType;

				switch (mt)
				{
					case MetaTokenType.mtString:
						// Raw path: IL string literals don't dedup well and can be large.
						BinaryHelper.WriteFieldString(c.body, T_StringValue, stringValue);
						break;
					case MetaTokenType.mtArrayInitializer:
						BinaryHelper.WriteFieldBlob(c.body, T_ArrayInitData, arrayInitData);
						break;
					case MetaTokenType.mtField:
						BinaryHelper.WriteFieldPooledType(c, T_FieldDeclaringType, fieldDeclaringType);
						BinaryHelper.WriteFieldPooledString(c, T_FieldName, fieldName);
						BinaryHelper.WriteFieldBool(c.body, T_FieldIsStatic, fieldIsStatic);
						if (fieldHasIndex)
							BinaryHelper.WriteFieldInt(c.body, T_FieldIndex, fieldIndex);
						break;
					case MetaTokenType.mtType:
						BinaryHelper.WriteFieldPooledType(c, T_TypeDescriptor, typeDescriptor);
						break;
					case MetaTokenType.mtMethod:
						BinaryHelper.WriteFieldPooledType(c, T_MethodDeclaringType, methodDeclaringType);
						BinaryHelper.WriteFieldPooledString(c, T_MethodName, methodName);
						BinaryHelper.WriteFieldPooledString(c, T_MethodFullSignature, methodFullSignature);
						BinaryHelper.WriteFieldBool(c.body, T_MethodIsStatic, methodIsStatic);
						BinaryHelper.WriteFieldPooledString(c, T_MethodAssembly, methodAssembly);
						BinaryHelper.WriteField(c, T_MethodParameters, c2 =>
						{
							var mp = methodParameters ?? Array.Empty<SerializedTypeDescriptor>();
							BinaryHelper.WriteInt(c2.body, mp.Length);
							for (int i = 0; i < mp.Length; i++)
								BinaryHelper.WritePooledType(c2, mp[i]);
						});
						BinaryHelper.WriteField(c, T_MethodGenericArguments, c2 =>
						{
							var ga = methodGenericArguments ?? Array.Empty<SerializedTypeDescriptor>();
							BinaryHelper.WriteInt(c2.body, ga.Length);
							for (int i = 0; i < ga.Length; i++)
								BinaryHelper.WritePooledType(c2, ga[i]);
						});
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

		public static readonly BinaryHelper.ReadFunc<SerializedEnumValue> ReadDelegate = Read;

		public static SerializedEnumValue Read(ref ReadContext ctx)
		{
			var v = new SerializedEnumValue();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_Name: v.name = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_Value: v.value = BinaryHelper.ReadLong(ctx.buf, ref ctx.pos); break;
				}
				ctx.pos = fieldEnd;
			}
			return v;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldPooledString(c, T_Name, name);
				BinaryHelper.WriteFieldLong(c.body, T_Value, value);
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

		public static readonly BinaryHelper.ReadFunc<SerializedEnum> ReadDelegate = Read;

		public static SerializedEnum Read(ref ReadContext ctx)
		{
			var e = new SerializedEnum();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_EnumName: e.enumName = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_UnderlyingType: e.underlyingType = BinaryHelper.ReadPooledType(ref ctx); break;
					case T_Values: e.values = BinaryHelper.ReadArray(ref ctx, SerializedEnumValue.ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return e;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldPooledString(c, T_EnumName, enumName);
				BinaryHelper.WriteFieldPooledType(c, T_UnderlyingType, underlyingType);
				BinaryHelper.WriteField(c, T_Values, c2 => BinaryHelper.WriteArray(c2.body, values, e => e.Write(c2)));
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

		public static readonly BinaryHelper.ReadFunc<SerializedAssembly> ReadDelegate = Read;

		public static SerializedAssembly Read(ref ReadContext ctx)
		{
			var asm = new SerializedAssembly();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_Classes: asm.classes = BinaryHelper.ReadArray(ref ctx, SerializedClass.ReadDelegate); break;
					case T_Metadata: asm.metadata = BinaryHelper.ReadArray(ref ctx, SerializedMetadataToken.ReadDelegate); break;
					case T_Enums: asm.enums = BinaryHelper.ReadArray(ref ctx, SerializedEnum.ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return asm;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteField(c, T_Classes, c2 => BinaryHelper.WriteArray(c2.body, classes, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_Metadata, c2 => BinaryHelper.WriteArray(c2.body, metadata, e => e.Write(c2)));
				BinaryHelper.WriteField(c, T_Enums, c2 => BinaryHelper.WriteArray(c2.body, enums, e => e.Write(c2)));
			});
		}

		public byte[] ToBytes() => BinaryHelper.CompressToBytesPooled(FormatVersion, Write);

		public static SerializedAssembly FromBytes(byte[] data) =>
			BinaryHelper.DecompressAndReadPooled(data, FormatVersion, "assembly", ReadDelegate);
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

	// Primitive-only value payload. 16 bytes (no object slot). Wire format:
	// [byte type][value bytes].  type is the StackType byte (0..10); reference-bearing
	// StackType values (Object/Address/NativeHandle) are not representable here.
	// NOTE: keep the value-slot field offsets in lockstep with StackElement (offset 8,
	// same union layout) - the consume site copies the 8-byte `l` slot directly into
	// StackElement.l, relying on identical bit layout for b/f/d/l/e.
	[StructLayout(LayoutKind.Explicit)]
	public struct PrimitivePayload
	{
		[FieldOffset(0)] public StackType type;
		[FieldOffset(8)] public bool   b;
		[FieldOffset(8)] public float  f;
		[FieldOffset(8)] public double d;
		[FieldOffset(8)] public long   l;
		[FieldOffset(8)] public ulong  e;

		public void Unbox( object i, StackType st )
		{
			type = st;
			switch( st )
			{
				case StackType.Boolean: this.l = ((bool)i)?1:0; break;
				case StackType.Sbyte:   this.l = (sbyte)i;      break;
				case StackType.Byte:    this.e = (byte)i;       break;
				case StackType.Short:   this.l = (short)i;      break;
				case StackType.Ushort:  this.e = (ushort)i;     break;
				case StackType.Int:     this.l = (int)i;        break;
				case StackType.Uint:    this.e = (uint)i;       break;
				case StackType.Long:    this.l = (long)i;       break;
				case StackType.Ulong:   this.e = (ulong)i;      break;
				case StackType.Float:   this.l = 0; this.f = (float)i;  break;
				case StackType.Double:  this.d = (double)i;     break;
			}
		}
	}

	public class SerializedProxyField
	{
		public byte fieldType;            // ProxyFieldType
		public string fieldName;          // null for non-root (array elements)
		public int matchingInstanceId;    // -1 for non-root

		// String / Json
		public string data;               // value as string / JSON text
		// Primitive
		public PrimitivePayload primitiveValue;  // typed primitive value; primitiveValue.type holds the StackType

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

		private static PrimitivePayload ReadPrimitiveValue(byte[] buf, ref int pos, byte stackType)
		{
			PrimitivePayload el = default;
			el.type = (StackType)stackType;
			switch ((StackType)stackType)
			{
			case StackType.Boolean: el.b = BinaryHelper.ReadBool  (buf, ref pos); break;
			case StackType.Sbyte:   el.l = BinaryHelper.ReadSByte (buf, ref pos); break;
			case StackType.Byte:    el.e = BinaryHelper.ReadByte  (buf, ref pos); break;
			case StackType.Short:   el.l = BinaryHelper.ReadShort (buf, ref pos); break;
			case StackType.Ushort:  el.e = BinaryHelper.ReadUShort(buf, ref pos); break;
			case StackType.Int:     el.l = BinaryHelper.ReadInt   (buf, ref pos); break;
			case StackType.Uint:    el.e = BinaryHelper.ReadUInt  (buf, ref pos); break;
			case StackType.Long:    el.l = BinaryHelper.ReadLong  (buf, ref pos); break;
			case StackType.Ulong:   el.e = BinaryHelper.ReadULong (buf, ref pos); break;
			case StackType.Float:   el.l = 0; el.f = BinaryHelper.ReadFloat (buf, ref pos); break;  // clear high bits
			case StackType.Double:  el.d = BinaryHelper.ReadDouble(buf, ref pos); break;
			default: throw new CilboxException($"Unknown primitive stackType {stackType}");
			}
			return el;
		}

		private static void WritePrimitiveValue(List<byte> buf, in PrimitivePayload value)
		{
			switch (value.type)
			{
			case StackType.Boolean: BinaryHelper.WriteBool  (buf, value.b);          break;
			case StackType.Sbyte:   BinaryHelper.WriteSByte (buf, (sbyte)value.l);   break;
			case StackType.Byte:    BinaryHelper.WriteByte  (buf, (byte)value.e);    break;
			case StackType.Short:   BinaryHelper.WriteShort (buf, (short)value.l);   break;
			case StackType.Ushort:  BinaryHelper.WriteUShort(buf, (ushort)value.e);  break;
			case StackType.Int:     BinaryHelper.WriteInt   (buf, (int)value.l);     break;
			case StackType.Uint:    BinaryHelper.WriteUInt  (buf, (uint)value.e);    break;
			case StackType.Long:    BinaryHelper.WriteLong  (buf, value.l);          break;
			case StackType.Ulong:   BinaryHelper.WriteULong (buf, value.e);          break;
			case StackType.Float:   BinaryHelper.WriteFloat (buf, value.f);          break;
			case StackType.Double:  BinaryHelper.WriteDouble(buf, value.d);          break;
			default: throw new CilboxException($"Unknown primitive StackType {value.type}");
			}
		}

		public static readonly BinaryHelper.ReadFunc<SerializedProxyField> ReadDelegate = Read;

		public static SerializedProxyField Read(ref ReadContext ctx)
		{
			var f = new SerializedProxyField();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_FieldType: f.fieldType = BinaryHelper.ReadByte(ctx.buf, ref ctx.pos); break;
					case T_FieldName: f.fieldName = BinaryHelper.ReadPooledString(ctx.buf, ref ctx.pos, ctx.strings); break;
					case T_MatchingInstanceId: f.matchingInstanceId = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_Data: f.data = BinaryHelper.ReadString(ctx.buf, ref ctx.pos); break;
					case T_PrimitivePayload:
					{
						byte st = BinaryHelper.ReadByte(ctx.buf, ref ctx.pos);
						f.primitiveValue = ReadPrimitiveValue(ctx.buf, ref ctx.pos, st);
						break;
					}
					case T_FieldObjectIndex: f.fieldObjectIndex = BinaryHelper.ReadInt(ctx.buf, ref ctx.pos); break;
					case T_ObjectRefIsNull: f.objectRefIsNull = BinaryHelper.ReadBool(ctx.buf, ref ctx.pos); break;
					case T_ElementType: f.elementType = BinaryHelper.ReadPooledType(ref ctx); break;
					case T_ArrayElements: f.arrayElements = BinaryHelper.ReadArray(ref ctx, ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return f;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteFieldByte(c.body, T_FieldType, fieldType);
				if (fieldName != null)
					BinaryHelper.WriteFieldPooledString(c, T_FieldName, fieldName);
				BinaryHelper.WriteFieldInt(c.body, T_MatchingInstanceId, matchingInstanceId);

				switch ((ProxyFieldType)fieldType)
				{
					case ProxyFieldType.Empty:
						break;
					case ProxyFieldType.String:
						// Raw path: user JSON / string values typically don't dedup and can be large.
						BinaryHelper.WriteFieldString(c.body, T_Data, data);
						break;
					case ProxyFieldType.Primitive:
						BinaryHelper.WriteField(c.body, T_PrimitivePayload, b2 =>
						{
							BinaryHelper.WriteByte(b2, (byte)primitiveValue.type);
							WritePrimitiveValue(b2, primitiveValue);
						});
						break;
					case ProxyFieldType.CilboxRef:
					case ProxyFieldType.ObjectRef:
						BinaryHelper.WriteFieldInt(c.body, T_FieldObjectIndex, fieldObjectIndex);
						BinaryHelper.WriteFieldBool(c.body, T_ObjectRefIsNull, objectRefIsNull);
						break;
					case ProxyFieldType.Array:
						BinaryHelper.WriteFieldPooledType(c, T_ElementType, elementType);
						BinaryHelper.WriteField(c, T_ArrayElements, c2 => BinaryHelper.WriteArray(c2.body, arrayElements, e => e.Write(c2)));
						break;
					case ProxyFieldType.Json:
						BinaryHelper.WriteFieldPooledType(c, T_ElementType, elementType);
						// Raw path: user JSON / string values typically don't dedup and can be large.
						BinaryHelper.WriteFieldString(c.body, T_Data, data);
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

		public static readonly BinaryHelper.ReadFunc<SerializedProxy> ReadDelegate = Read;

		public static SerializedProxy Read(ref ReadContext ctx)
		{
			var p = new SerializedProxy();
			while (true)
			{
				byte token = BinaryHelper.ReadFieldToken(ctx.buf, ref ctx.pos);
				if (token == BinaryHelper.EndOfStruct) break;
				int fieldEnd = BinaryHelper.ReadFieldLength(ctx.buf, ref ctx.pos);
				switch (token)
				{
					case T_Fields: p.fields = BinaryHelper.ReadArray(ref ctx, SerializedProxyField.ReadDelegate); break;
				}
				ctx.pos = fieldEnd;
			}
			return p;
		}

		public void Write(WriteContext ctx)
		{
			BinaryHelper.WriteStruct(ctx, c =>
			{
				BinaryHelper.WriteField(c, T_Fields, c2 => BinaryHelper.WriteArray(c2.body, fields, e => e.Write(c2)));
			});
		}

		public byte[] ToBytes() => BinaryHelper.CompressToBytesPooled(FormatVersion, Write);

		public static SerializedProxy FromBytes(byte[] data) =>
			BinaryHelper.DecompressAndReadPooled(data, FormatVersion, "proxy", ReadDelegate);
	}
}
