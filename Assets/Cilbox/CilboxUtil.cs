using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.Reflection;
#endif

namespace Cilbox
{
	public static class CilboxUtil
	{
		static public object DeserializeDataForProxyField( Type t, String sInitialize )
		{
			if( sInitialize != null && sInitialize.Length > 0 )
				return TypeDescriptor.GetConverter(t).ConvertFrom(sInitialize);
			else
			{
				if( !t.IsPrimitive ) 
					return null;
				else
					return Activator.CreateInstance(t);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct IntFloatConverter
		{
			[FieldOffset(0)]private float f;
			[FieldOffset(0)]private double d;
			[FieldOffset(0)]private int i;
			[FieldOffset(0)]private uint u;
			[FieldOffset(0)]private long l;
			[FieldOffset(0)]private ulong e;
			public static float ConvertItoF(int value)
			{
				return new IntFloatConverter { i = value }.f;
			}
			public static float ConvertUtoF(uint value)
			{
				return new IntFloatConverter { u = value }.f;
			}
			public static int ConvertFtoI(float value)
			{
				return new IntFloatConverter { f = value }.i;
			}
			public static double ConvertEtoD(ulong value)
			{
				return new IntFloatConverter { e = value }.d;
			}
		}

		static public int IntFromHexChar( char c )
		{
			if( c >= '0' && c <= '9' )
				return c - '0';
			else if( c >= 'a' & c <= 'f' )
				return c - 'a' + 10;
			else
				return -1;
		}

		static public String HexFromNum( int n )
		{
			char c = (char) ( '0' + (n & 0xf));
			if( c > '9' ) c = (char)( c + 'a' - '9' - 1);
			return "" + c;
		}
		static public String Escape( String s )
		{
			String ret = "";
			foreach( char c in s )
			{
				if( c == '\\' )
					ret += "\\\\";
				else if( c == '\t' )
					ret += "\\t";
				else if( c == '\n' )
					ret += "\\n";
				else if( c > 0x7f || c < 20 )
					ret += "\\" + HexFromNum( c>>12 ) + HexFromNum( c >> 8 ) + HexFromNum( c >> 4 ) + HexFromNum( c >> 0 );
				else
					ret += c;
			}
			return ret;
		}
		// \tkey\tvalue\tkey\tvalue\tkey\tvalue\n
		static public String ParseString( String s, ref int pos, ref int poserror )
		{
			String ret = "";
			int hexmode = 0;
			int hexchar = 0;
			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( hexmode != 0 )
				{
					//Debug.Log( "TEX: " + (int)c + "/" + hexmode );
					if( c == '\\' && hexmode == 1 )
					{
						ret += (char)'\\';
						hexchar = 0; hexmode = 0;
					}
					else if( c == 't' && hexmode == 1 )
					{
						ret += (char)'\t';
						hexchar = 0; hexmode = 0;
					}
					else if( c == 'n' && hexmode == 1 )
					{
						ret += (char)'\n';
						hexchar = 0; hexmode = 0;
					}
					else
					{
						hexchar = hexchar << 4;
						int v = IntFromHexChar( c );
						if( v < 0 ) break;
						hexchar |= v;
						hexmode++;
						if( hexmode == 4 )
						{
							ret += (char)hexchar;
							hexchar = 0;
							hexmode = 0;
						}
					}
				}
				else
				{
					//Debug.Log( c + " " + (c == '\t' || c == '\n') );
					if( c == '\\' )
						hexmode = 1;
					else if( c == '\t' || c == '\n' )
						break;
					else
						ret += c;
				}
			}
			if( hexmode != 0 )
			{
				poserror = pos;
			}
			//Debug.Log( "PAX: " + poserror + " / " + hexmode );
			return ret;
		}

		public static readonly Dictionary< String, StackType > TypeToStackType = new Dictionary<String, StackType>(){
			{ "System.Boolean", StackType.Boolean },
			{ "System.SByte", StackType.Sbyte },
			{ "System.Byte", StackType.Byte },
			{ "System.Int16", StackType.Short },
			{ "System.UInt16", StackType.Ushort },
			{ "System.Int32", StackType.Int },
			{ "System.UInt32", StackType.Uint },
			{ "System.Int64", StackType.Long },
			{ "System.UInt64", StackType.Ulong },
			{ "System.Single", StackType.Float },
			{ "System.Dobule", StackType.Double },
			{ "object", StackType.Object } };

		static public String SerializeDict( OrderedDictionary dict )
		{
			String ret = "";
			foreach( DictionaryEntry s in dict )
			{
				ret += "\t" + Escape((String)s.Key) + "\t" + Escape((String)s.Value);
			}
			return ret + "\n";
		}

		static public String SerializeArray( String [] arr )
		{
			String ret = "";
			int i;
			for( i = 0; i < arr.Length; i++ )
			{
				ret += Escape(arr[i]);
				if( i < arr.Length-1 ) ret += "\t";
			}
			return ret;
		}

		static public String [] DeserializeArray( String s )
		{
			if( s == null ) return new String[0];
			int poserror = -1;
			int pos = 0;
			List< String > ret = new List< String >();
			poserror = -1;

			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( c == '\n' ) break;
				if( c == '\t' ) continue;
				ret.Add( ParseString( s, ref pos, ref poserror ) );
				if( poserror >= 0 )
					break;
			}
			if( poserror >= 0 )
			{
				Debug.LogError( $"Erorr parsing dictionary at char {poserror}\n{s}" );
			}
			return ret.ToArray();
		}
		static public OrderedDictionary DeserializeDict( String s )
		{
			if( s == null ) return null;
			OrderedDictionary ret = new OrderedDictionary();
			int poserror = -1;
			int pos = 0;
			int mode = 0;
			String key = "";
			poserror = -1;

			for( ; pos < s.Length; pos++ )
			{
				char c = s[pos];
				if( mode == 0 )
				{
					//Debug.Log( "NEXT" + (int)c );
					if( c == '\n' ) { pos++; break; }
					else if( c == '\t' ) { /* OK */ }
					else { poserror = pos; break; }
				}
				else if( mode == 1 )
				{
					key = ParseString( s, ref pos, ref poserror );
					pos--;
				}
				else if( mode == 2 )
				{
					if( c != '\t' ) { poserror = pos; break; }
				}
				else if( mode == 3 )
				{
					ret[key] = ParseString( s, ref pos, ref poserror );
					pos--;
				}
				mode = (mode+1) % 4;
				if( poserror >= 0 )
					break;
			}
			if( poserror >= 0 )
			{
				Debug.LogError( $"Erorr parsing dictionary at char {poserror} ({s})" );
			}
			return ret;
		}


		public static ulong BytecodePullLiteral( byte[] byteCode, ref int i, int len )
		{
			ulong ret = 0;
			for( int lr = 0; lr < len; lr++ )
			{
				ret |= ((ulong)byteCode[i++]) << (lr*8);
			}
			return ret;
		}

		public static void BytecodeReplaceLiteral( ref byte[] byteCode, ref int i, int len, ulong operand )
		{
			for( int lr = 0; lr < len; lr++ )
			{
				byteCode[i++] = (byte)(operand & 0xff);
				operand >>= 8;
			}
		}



		///////////////////////////////////////////////////////////////////////////
		//  REFLECTION HELPERS  ///////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////
		public static Type GetNativeTypeFromName( String useAssembly, String typeName )
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if( assembly.GetName().Name != useAssembly ) continue;
				var tt = assembly.GetTypes();
				foreach( Type lt in tt )
				{
					if( lt.FullName == typeName )
					{
						return lt;
					}
				}
			}
			return null;
		}

		public static Type[] TypeNamesToArrayOfNativeTypes( String [] parameterNames )
		{
			if( parameterNames == null ) return null;
			Type[] ret = new Type[parameterNames.Length];
			for( int i = 0; i < parameterNames.Length; i++ )
			{
				String [] assemblyAndTypeName = DeserializeArray( parameterNames[i] );
				Type pt = ret[i] = GetNativeTypeFromName( assemblyAndTypeName[0], assemblyAndTypeName[1] );
			}
			return ret;
		}

		public static MonoBehaviour [] GetAllBehavioursThatNeedCilboxing()
		{
			List<MonoBehaviour> ret = new List<MonoBehaviour>();

			object[] objToCheck = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			foreach (object o in objToCheck)
			{
				GameObject g = (GameObject) o;
				MonoBehaviour [] scripts = g.GetComponents<MonoBehaviour>();
				foreach (MonoBehaviour m in scripts )
				{
					// Skip null objects.
					if (m == null)
						continue;
					object[] attribs = m.GetType().GetCustomAttributes(typeof(CilboxableAttribute), true);
					// Not a proxiable script.
					if (attribs == null || attribs.Length <= 0)
						continue;
					ret.Add(m);
				}
			}
			return ret.ToArray();
		}


		///////////////////////////////////////////////////////////////////////////
		//  DEFS FROM CECIL FOR PARSING CIL  //////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////




		// From https://raw.githubusercontent.com/jbevain/cecil/refs/heads/master/Mono.Cecil.Cil/OpCodes.cs
		//
		// Author:
		//   Jb Evain (jbevain@gmail.com)
		//
		// Copyright (c) 2008 - 2015 Jb Evain
		// Copyright (c) 2008 - 2011 Novell, Inc.
		//
		// Licensed under the MIT/X11 license.
		//

		public static class OpCodes {

			public enum FlowControl {
				Branch,
				Break,
				Call,
				Cond_Branch,
				Meta,
				Next,
				Phi,
				Return,
				Throw,
			}

			public enum OpCodeType {
				Annotation,
				Macro,
				Nternal,
				Objmodel,
				Prefix,
				Primitive,
			}

			public enum OperandType {
				InlineBrTarget = 0,
				InlineField,
				InlineI,
				InlineI8,
				InlineMethod,
				InlineNone,
				InlinePhi,
				InlineR,
				InlineSig,
				InlineString,
				InlineSwitch,
				InlineTok,
				InlineType,
				InlineVar,
				InlineArg,
				ShortInlineBrTarget,
				ShortInlineI,
				ShortInlineR,
				ShortInlineVar,
				ShortInlineArg,
			}

			// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.operandtype?view=net-9.0
			static readonly public int [] OperandLength = {
				4, // InlineBrTarget
				4, // InlineField
				4, // InlineI
				8, // InlineI8
				4, // InlineMethod
				0, // InlineNone
				0, // InlinePhi = Unused (Not a real type)
				8, // InlineR
				4, // InlineSig
				4, // InlineString
				4, // InlineSwitch
				4, // InlineTok - The operand is a FieldRef, MethodRef, or TypeRef token.
				4, // InlineType
				2, // InlineVar
				2, // InlineArg
				1, // ShortInlineBrTarget
				1, // ShortInlineI
				4, // ShortInlineR
				1, // ShortInlineVar
				1, // ShortInlineArg
			};

			public enum StackBehaviour {
				Pop0,
				Pop1,
				Pop1_pop1,
				Popi,
				Popi_pop1,
				Popi_popi,
				Popi_popi8,
				Popi_popi_popi,
				Popi_popr4,
				Popi_popr8,
				Popref,
				Popref_pop1,
				Popref_popi,
				Popref_popi_popi,
				Popref_popi_popi8,
				Popref_popi_popr4,
				Popref_popi_popr8,
				Popref_popi_popref,
				PopAll,
				Push0,
				Push1,
				Push1_push1,
				Pushi,
				Pushi8,
				Pushr4,
				Pushr8,
				Pushref,
				Varpop,
				Varpush,
			}

			public enum Code {
				Nop,
				Break,
				Ldarg_0,
				Ldarg_1,
				Ldarg_2,
				Ldarg_3,
				Ldloc_0,
				Ldloc_1,
				Ldloc_2,
				Ldloc_3,
				Stloc_0,
				Stloc_1,
				Stloc_2,
				Stloc_3,
				Ldarg_S,
				Ldarga_S,
				Starg_S,
				Ldloc_S,
				Ldloca_S,
				Stloc_S,
				Ldnull,
				Ldc_I4_M1,
				Ldc_I4_0,
				Ldc_I4_1,
				Ldc_I4_2,
				Ldc_I4_3,
				Ldc_I4_4,
				Ldc_I4_5,
				Ldc_I4_6,
				Ldc_I4_7,
				Ldc_I4_8,
				Ldc_I4_S,
				Ldc_I4,
				Ldc_I8,
				Ldc_R4,
				Ldc_R8,
				Dup,
				Pop,
				Jmp,
				Call,
				Calli,
				Ret,
				Br_S,
				Brfalse_S,
				Brtrue_S,
				Beq_S,
				Bge_S,
				Bgt_S,
				Ble_S,
				Blt_S,
				Bne_Un_S,
				Bge_Un_S,
				Bgt_Un_S,
				Ble_Un_S,
				Blt_Un_S,
				Br,
				Brfalse,
				Brtrue,
				Beq,
				Bge,
				Bgt,
				Ble,
				Blt,
				Bne_Un,
				Bge_Un,
				Bgt_Un,
				Ble_Un,
				Blt_Un,
				Switch,
				Ldind_I1,
				Ldind_U1,
				Ldind_I2,
				Ldind_U2,
				Ldind_I4,
				Ldind_U4,
				Ldind_I8,
				Ldind_I,
				Ldind_R4,
				Ldind_R8,
				Ldind_Ref,
				Stind_Ref,
				Stind_I1,
				Stind_I2,
				Stind_I4,
				Stind_I8,
				Stind_R4,
				Stind_R8,
				Add,
				Sub,
				Mul,
				Div,
				Div_Un,
				Rem,
				Rem_Un,
				And,
				Or,
				Xor,
				Shl,
				Shr,
				Shr_Un,
				Neg,
				Not,
				Conv_I1,
				Conv_I2,
				Conv_I4,
				Conv_I8,
				Conv_R4,
				Conv_R8,
				Conv_U4,
				Conv_U8,
				Callvirt,
				Cpobj,
				Ldobj,
				Ldstr,
				Newobj,
				Castclass,
				Isinst,
				Conv_R_Un,
				Unbox,
				Throw,
				Ldfld,
				Ldflda,
				Stfld,
				Ldsfld,
				Ldsflda,
				Stsfld,
				Stobj,
				Conv_Ovf_I1_Un,
				Conv_Ovf_I2_Un,
				Conv_Ovf_I4_Un,
				Conv_Ovf_I8_Un,
				Conv_Ovf_U1_Un,
				Conv_Ovf_U2_Un,
				Conv_Ovf_U4_Un,
				Conv_Ovf_U8_Un,
				Conv_Ovf_I_Un,
				Conv_Ovf_U_Un,
				Box,
				Newarr,
				Ldlen,
				Ldelema,
				Ldelem_I1,
				Ldelem_U1,
				Ldelem_I2,
				Ldelem_U2,
				Ldelem_I4,
				Ldelem_U4,
				Ldelem_I8,
				Ldelem_I,
				Ldelem_R4,
				Ldelem_R8,
				Ldelem_Ref,
				Stelem_I,
				Stelem_I1,
				Stelem_I2,
				Stelem_I4,
				Stelem_I8,
				Stelem_R4,
				Stelem_R8,
				Stelem_Ref,
				Ldelem_Any,
				Stelem_Any,
				Unbox_Any,
				Conv_Ovf_I1,
				Conv_Ovf_U1,
				Conv_Ovf_I2,
				Conv_Ovf_U2,
				Conv_Ovf_I4,
				Conv_Ovf_U4,
				Conv_Ovf_I8,
				Conv_Ovf_U8,
				Refanyval,
				Ckfinite,
				Mkrefany,
				Ldtoken,
				Conv_U2,
				Conv_U1,
				Conv_I,
				Conv_Ovf_I,
				Conv_Ovf_U,
				Add_Ovf,
				Add_Ovf_Un,
				Mul_Ovf,
				Mul_Ovf_Un,
				Sub_Ovf,
				Sub_Ovf_Un,
				Endfinally,
				Leave,
				Leave_S,
				Stind_I,
				Conv_U,
				Arglist,
				Ceq,
				Cgt,
				Cgt_Un,
				Clt,
				Clt_Un,
				Ldftn,
				Ldvirtftn,
				Ldarg,
				Ldarga,
				Starg,
				Ldloc,
				Ldloca,
				Stloc,
				Localloc,
				Endfilter,
				Unaligned,
				Volatile,
				Tail,
				Initobj,
				Constrained,
				Cpblk,
				Initblk,
				No,
				Rethrow,
				Sizeof,
				Refanytype,
				Readonly,
			}
			public struct OpCode : IEquatable<OpCode> {

				readonly byte op1;
				readonly byte op2;
				readonly byte code;
				readonly byte flow_control;
				readonly byte opcode_type;
				readonly byte operand_type;
				readonly byte stack_behavior_pop;
				readonly byte stack_behavior_push;

				public string Name {
					get { return OpCodeNames.names [(int) Code]; }
				}

				public int Size {
					get { return op1 == 0xff ? 1 : 2; }
				}

				public byte Op1 {
					get { return op1; }
				}

				public byte Op2 {
					get { return op2; }
				}

				public short Value {
					get { return op1 == 0xff ? op2 : (short) ((op1 << 8) | op2); }
				}

				public Code Code {
					get { return (Code) code; }
				}

				public FlowControl FlowControl {
					get { return (FlowControl) flow_control; }
				}

				public OpCodeType OpCodeType {
					get { return (OpCodeType) opcode_type; }
				}

				public OperandType OperandType {
					get { return (OperandType) operand_type; }
				}

				public StackBehaviour StackBehaviourPop {
					get { return (StackBehaviour) stack_behavior_pop; }
				}

				public StackBehaviour StackBehaviourPush {
					get { return (StackBehaviour) stack_behavior_push; }
				}

				internal OpCode (int x, int y)
				{
					this.op1 = (byte) ((x >> 0) & 0xff);
					this.op2 = (byte) ((x >> 8) & 0xff);
					this.code = (byte) ((x >> 16) & 0xff);
					this.flow_control = (byte) ((x >> 24) & 0xff);

					this.opcode_type = (byte) ((y >> 0) & 0xff);
					this.operand_type = (byte) ((y >> 8) & 0xff);
					this.stack_behavior_pop = (byte) ((y >> 16) & 0xff);
					this.stack_behavior_push = (byte) ((y >> 24) & 0xff);

					if (op1 == 0xff)
						OpCodes.OneByteOpCode [op2] = this;
					else
						OpCodes.TwoBytesOpCode [op2] = this;
				}

				public override int GetHashCode ()
				{
					return Value;
				}

				public override bool Equals (object obj)
				{
					if (!(obj is OpCode))
						return false;

					var opcode = (OpCode) obj;
					return op1 == opcode.op1 && op2 == opcode.op2;
				}

				public bool Equals (OpCode opcode)
				{
					return op1 == opcode.op1 && op2 == opcode.op2;
				}

				public static bool operator == (OpCode one, OpCode other)
				{
					return one.op1 == other.op1 && one.op2 == other.op2;
				}

				public static bool operator != (OpCode one, OpCode other)
				{
					return one.op1 != other.op1 || one.op2 != other.op2;
				}

				public override string ToString ()
				{
					return Name;
				}
			}

			// Actual opcodes.

			static readonly OpCode [] OneByteOpCode = new OpCode [0xe0 + 1];
			static readonly OpCode [] TwoBytesOpCode = new OpCode [0x1e + 1];


			public static OpCode ReadOpCode ( byte[] bytecode, ref int i )
			{
				//Debug.Log( $"Reading Opcodes: {bytecode[0]} {(bytecode.Length>1?bytecode[1]:-1)}" );
				var il_opcode = bytecode[i++];
				if( il_opcode != 0xfe )
					return OpCodes.OneByteOpCode [il_opcode];
				else
					return OpCodes.TwoBytesOpCode [ bytecode[i++] ];
			}


			public static readonly OpCode Nop = new OpCode (
				0xff << 0 | 0x00 << 8 | (byte) Code.Nop << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Break = new OpCode (
				0xff << 0 | 0x01 << 8 | (byte) Code.Break << 16 | (byte) FlowControl.Break << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldarg_0 = new OpCode (
				0xff << 0 | 0x02 << 8 | (byte) Code.Ldarg_0 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldarg_1 = new OpCode (
				0xff << 0 | 0x03 << 8 | (byte) Code.Ldarg_1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldarg_2 = new OpCode (
				0xff << 0 | 0x04 << 8 | (byte) Code.Ldarg_2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldarg_3 = new OpCode (
				0xff << 0 | 0x05 << 8 | (byte) Code.Ldarg_3 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloc_0 = new OpCode (
				0xff << 0 | 0x06 << 8 | (byte) Code.Ldloc_0 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloc_1 = new OpCode (
				0xff << 0 | 0x07 << 8 | (byte) Code.Ldloc_1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloc_2 = new OpCode (
				0xff << 0 | 0x08 << 8 | (byte) Code.Ldloc_2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloc_3 = new OpCode (
				0xff << 0 | 0x09 << 8 | (byte) Code.Ldloc_3 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Stloc_0 = new OpCode (
				0xff << 0 | 0x0a << 8 | (byte) Code.Stloc_0 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stloc_1 = new OpCode (
				0xff << 0 | 0x0b << 8 | (byte) Code.Stloc_1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stloc_2 = new OpCode (
				0xff << 0 | 0x0c << 8 | (byte) Code.Stloc_2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stloc_3 = new OpCode (
				0xff << 0 | 0x0d << 8 | (byte) Code.Stloc_3 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldarg_S = new OpCode (
				0xff << 0 | 0x0e << 8 | (byte) Code.Ldarg_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineArg << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldarga_S = new OpCode (
				0xff << 0 | 0x0f << 8 | (byte) Code.Ldarga_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineArg << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Starg_S = new OpCode (
				0xff << 0 | 0x10 << 8 | (byte) Code.Starg_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineArg << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldloc_S = new OpCode (
				0xff << 0 | 0x11 << 8 | (byte) Code.Ldloc_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineVar << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloca_S = new OpCode (
				0xff << 0 | 0x12 << 8 | (byte) Code.Ldloca_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineVar << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Stloc_S = new OpCode (
				0xff << 0 | 0x13 << 8 | (byte) Code.Stloc_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineVar << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldnull = new OpCode (
				0xff << 0 | 0x14 << 8 | (byte) Code.Ldnull << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Ldc_I4_M1 = new OpCode (
				0xff << 0 | 0x15 << 8 | (byte) Code.Ldc_I4_M1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_0 = new OpCode (
				0xff << 0 | 0x16 << 8 | (byte) Code.Ldc_I4_0 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_1 = new OpCode (
				0xff << 0 | 0x17 << 8 | (byte) Code.Ldc_I4_1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_2 = new OpCode (
				0xff << 0 | 0x18 << 8 | (byte) Code.Ldc_I4_2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_3 = new OpCode (
				0xff << 0 | 0x19 << 8 | (byte) Code.Ldc_I4_3 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_4 = new OpCode (
				0xff << 0 | 0x1a << 8 | (byte) Code.Ldc_I4_4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_5 = new OpCode (
				0xff << 0 | 0x1b << 8 | (byte) Code.Ldc_I4_5 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_6 = new OpCode (
				0xff << 0 | 0x1c << 8 | (byte) Code.Ldc_I4_6 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_7 = new OpCode (
				0xff << 0 | 0x1d << 8 | (byte) Code.Ldc_I4_7 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_8 = new OpCode (
				0xff << 0 | 0x1e << 8 | (byte) Code.Ldc_I4_8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4_S = new OpCode (
				0xff << 0 | 0x1f << 8 | (byte) Code.Ldc_I4_S << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineI << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I4 = new OpCode (
				0xff << 0 | 0x20 << 8 | (byte) Code.Ldc_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineI << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldc_I8 = new OpCode (
				0xff << 0 | 0x21 << 8 | (byte) Code.Ldc_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineI8 << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Ldc_R4 = new OpCode (
				0xff << 0 | 0x22 << 8 | (byte) Code.Ldc_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.ShortInlineR << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushr4 << 24);

			public static readonly OpCode Ldc_R8 = new OpCode (
				0xff << 0 | 0x23 << 8 | (byte) Code.Ldc_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineR << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Dup = new OpCode (
				0xff << 0 | 0x25 << 8 | (byte) Code.Dup << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push1_push1 << 24);

			public static readonly OpCode Pop = new OpCode (
				0xff << 0 | 0x26 << 8 | (byte) Code.Pop << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Jmp = new OpCode (
				0xff << 0 | 0x27 << 8 | (byte) Code.Jmp << 16 | (byte) FlowControl.Call << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Call = new OpCode (
				0xff << 0 | 0x28 << 8 | (byte) Code.Call << 16 | (byte) FlowControl.Call << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Varpop << 16 | (byte) StackBehaviour.Varpush << 24);

			public static readonly OpCode Calli = new OpCode (
				0xff << 0 | 0x29 << 8 | (byte) Code.Calli << 16 | (byte) FlowControl.Call << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineSig << 8 | (byte) StackBehaviour.Varpop << 16 | (byte) StackBehaviour.Varpush << 24);

			public static readonly OpCode Ret = new OpCode (
				0xff << 0 | 0x2a << 8 | (byte) Code.Ret << 16 | (byte) FlowControl.Return << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Varpop << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Br_S = new OpCode (
				0xff << 0 | 0x2b << 8 | (byte) Code.Br_S << 16 | (byte) FlowControl.Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Brfalse_S = new OpCode (
				0xff << 0 | 0x2c << 8 | (byte) Code.Brfalse_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Brtrue_S = new OpCode (
				0xff << 0 | 0x2d << 8 | (byte) Code.Brtrue_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Beq_S = new OpCode (
				0xff << 0 | 0x2e << 8 | (byte) Code.Beq_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bge_S = new OpCode (
				0xff << 0 | 0x2f << 8 | (byte) Code.Bge_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bgt_S = new OpCode (
				0xff << 0 | 0x30 << 8 | (byte) Code.Bgt_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ble_S = new OpCode (
				0xff << 0 | 0x31 << 8 | (byte) Code.Ble_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Blt_S = new OpCode (
				0xff << 0 | 0x32 << 8 | (byte) Code.Blt_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bne_Un_S = new OpCode (
				0xff << 0 | 0x33 << 8 | (byte) Code.Bne_Un_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bge_Un_S = new OpCode (
				0xff << 0 | 0x34 << 8 | (byte) Code.Bge_Un_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bgt_Un_S = new OpCode (
				0xff << 0 | 0x35 << 8 | (byte) Code.Bgt_Un_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ble_Un_S = new OpCode (
				0xff << 0 | 0x36 << 8 | (byte) Code.Ble_Un_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Blt_Un_S = new OpCode (
				0xff << 0 | 0x37 << 8 | (byte) Code.Blt_Un_S << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Br = new OpCode (
				0xff << 0 | 0x38 << 8 | (byte) Code.Br << 16 | (byte) FlowControl.Branch << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Brfalse = new OpCode (
				0xff << 0 | 0x39 << 8 | (byte) Code.Brfalse << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Brtrue = new OpCode (
				0xff << 0 | 0x3a << 8 | (byte) Code.Brtrue << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Beq = new OpCode (
				0xff << 0 | 0x3b << 8 | (byte) Code.Beq << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bge = new OpCode (
				0xff << 0 | 0x3c << 8 | (byte) Code.Bge << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bgt = new OpCode (
				0xff << 0 | 0x3d << 8 | (byte) Code.Bgt << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ble = new OpCode (
				0xff << 0 | 0x3e << 8 | (byte) Code.Ble << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Blt = new OpCode (
				0xff << 0 | 0x3f << 8 | (byte) Code.Blt << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bne_Un = new OpCode (
				0xff << 0 | 0x40 << 8 | (byte) Code.Bne_Un << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bge_Un = new OpCode (
				0xff << 0 | 0x41 << 8 | (byte) Code.Bge_Un << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Bgt_Un = new OpCode (
				0xff << 0 | 0x42 << 8 | (byte) Code.Bgt_Un << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ble_Un = new OpCode (
				0xff << 0 | 0x43 << 8 | (byte) Code.Ble_Un << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Blt_Un = new OpCode (
				0xff << 0 | 0x44 << 8 | (byte) Code.Blt_Un << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Switch = new OpCode (
				0xff << 0 | 0x45 << 8 | (byte) Code.Switch << 16 | (byte) FlowControl.Cond_Branch << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineSwitch << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldind_I1 = new OpCode (
				0xff << 0 | 0x46 << 8 | (byte) Code.Ldind_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_U1 = new OpCode (
				0xff << 0 | 0x47 << 8 | (byte) Code.Ldind_U1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_I2 = new OpCode (
				0xff << 0 | 0x48 << 8 | (byte) Code.Ldind_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_U2 = new OpCode (
				0xff << 0 | 0x49 << 8 | (byte) Code.Ldind_U2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_I4 = new OpCode (
				0xff << 0 | 0x4a << 8 | (byte) Code.Ldind_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_U4 = new OpCode (
				0xff << 0 | 0x4b << 8 | (byte) Code.Ldind_U4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_I8 = new OpCode (
				0xff << 0 | 0x4c << 8 | (byte) Code.Ldind_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Ldind_I = new OpCode (
				0xff << 0 | 0x4d << 8 | (byte) Code.Ldind_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldind_R4 = new OpCode (
				0xff << 0 | 0x4e << 8 | (byte) Code.Ldind_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushr4 << 24);

			public static readonly OpCode Ldind_R8 = new OpCode (
				0xff << 0 | 0x4f << 8 | (byte) Code.Ldind_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Ldind_Ref = new OpCode (
				0xff << 0 | 0x50 << 8 | (byte) Code.Ldind_Ref << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Stind_Ref = new OpCode (
				0xff << 0 | 0x51 << 8 | (byte) Code.Stind_Ref << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_I1 = new OpCode (
				0xff << 0 | 0x52 << 8 | (byte) Code.Stind_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_I2 = new OpCode (
				0xff << 0 | 0x53 << 8 | (byte) Code.Stind_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_I4 = new OpCode (
				0xff << 0 | 0x54 << 8 | (byte) Code.Stind_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_I8 = new OpCode (
				0xff << 0 | 0x55 << 8 | (byte) Code.Stind_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi8 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_R4 = new OpCode (
				0xff << 0 | 0x56 << 8 | (byte) Code.Stind_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popr4 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_R8 = new OpCode (
				0xff << 0 | 0x57 << 8 | (byte) Code.Stind_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popr8 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Add = new OpCode (
				0xff << 0 | 0x58 << 8 | (byte) Code.Add << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Sub = new OpCode (
				0xff << 0 | 0x59 << 8 | (byte) Code.Sub << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Mul = new OpCode (
				0xff << 0 | 0x5a << 8 | (byte) Code.Mul << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Div = new OpCode (
				0xff << 0 | 0x5b << 8 | (byte) Code.Div << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Div_Un = new OpCode (
				0xff << 0 | 0x5c << 8 | (byte) Code.Div_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Rem = new OpCode (
				0xff << 0 | 0x5d << 8 | (byte) Code.Rem << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Rem_Un = new OpCode (
				0xff << 0 | 0x5e << 8 | (byte) Code.Rem_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode And = new OpCode (
				0xff << 0 | 0x5f << 8 | (byte) Code.And << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Or = new OpCode (
				0xff << 0 | 0x60 << 8 | (byte) Code.Or << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Xor = new OpCode (
				0xff << 0 | 0x61 << 8 | (byte) Code.Xor << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Shl = new OpCode (
				0xff << 0 | 0x62 << 8 | (byte) Code.Shl << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Shr = new OpCode (
				0xff << 0 | 0x63 << 8 | (byte) Code.Shr << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Shr_Un = new OpCode (
				0xff << 0 | 0x64 << 8 | (byte) Code.Shr_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Neg = new OpCode (
				0xff << 0 | 0x65 << 8 | (byte) Code.Neg << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Not = new OpCode (
				0xff << 0 | 0x66 << 8 | (byte) Code.Not << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Conv_I1 = new OpCode (
				0xff << 0 | 0x67 << 8 | (byte) Code.Conv_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_I2 = new OpCode (
				0xff << 0 | 0x68 << 8 | (byte) Code.Conv_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_I4 = new OpCode (
				0xff << 0 | 0x69 << 8 | (byte) Code.Conv_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_I8 = new OpCode (
				0xff << 0 | 0x6a << 8 | (byte) Code.Conv_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Conv_R4 = new OpCode (
				0xff << 0 | 0x6b << 8 | (byte) Code.Conv_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushr4 << 24);

			public static readonly OpCode Conv_R8 = new OpCode (
				0xff << 0 | 0x6c << 8 | (byte) Code.Conv_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Conv_U4 = new OpCode (
				0xff << 0 | 0x6d << 8 | (byte) Code.Conv_U4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_U8 = new OpCode (
				0xff << 0 | 0x6e << 8 | (byte) Code.Conv_U8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Callvirt = new OpCode (
				0xff << 0 | 0x6f << 8 | (byte) Code.Callvirt << 16 | (byte) FlowControl.Call << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Varpop << 16 | (byte) StackBehaviour.Varpush << 24);

			public static readonly OpCode Cpobj = new OpCode (
				0xff << 0 | 0x70 << 8 | (byte) Code.Cpobj << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldobj = new OpCode (
				0xff << 0 | 0x71 << 8 | (byte) Code.Ldobj << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldstr = new OpCode (
				0xff << 0 | 0x72 << 8 | (byte) Code.Ldstr << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineString << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Newobj = new OpCode (
				0xff << 0 | 0x73 << 8 | (byte) Code.Newobj << 16 | (byte) FlowControl.Call << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Varpop << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Castclass = new OpCode (
				0xff << 0 | 0x74 << 8 | (byte) Code.Castclass << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Isinst = new OpCode (
				0xff << 0 | 0x75 << 8 | (byte) Code.Isinst << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_R_Un = new OpCode (
				0xff << 0 | 0x76 << 8 | (byte) Code.Conv_R_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Unbox = new OpCode (
				0xff << 0 | 0x79 << 8 | (byte) Code.Unbox << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Throw = new OpCode (
				0xff << 0 | 0x7a << 8 | (byte) Code.Throw << 16 | (byte) FlowControl.Throw << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldfld = new OpCode (
				0xff << 0 | 0x7b << 8 | (byte) Code.Ldfld << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldflda = new OpCode (
				0xff << 0 | 0x7c << 8 | (byte) Code.Ldflda << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Stfld = new OpCode (
				0xff << 0 | 0x7d << 8 | (byte) Code.Stfld << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Popref_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldsfld = new OpCode (
				0xff << 0 | 0x7e << 8 | (byte) Code.Ldsfld << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldsflda = new OpCode (
				0xff << 0 | 0x7f << 8 | (byte) Code.Ldsflda << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Stsfld = new OpCode (
				0xff << 0 | 0x80 << 8 | (byte) Code.Stsfld << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineField << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stobj = new OpCode (
				0xff << 0 | 0x81 << 8 | (byte) Code.Stobj << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi_pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Conv_Ovf_I1_Un = new OpCode (
				0xff << 0 | 0x82 << 8 | (byte) Code.Conv_Ovf_I1_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I2_Un = new OpCode (
				0xff << 0 | 0x83 << 8 | (byte) Code.Conv_Ovf_I2_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I4_Un = new OpCode (
				0xff << 0 | 0x84 << 8 | (byte) Code.Conv_Ovf_I4_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I8_Un = new OpCode (
				0xff << 0 | 0x85 << 8 | (byte) Code.Conv_Ovf_I8_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Conv_Ovf_U1_Un = new OpCode (
				0xff << 0 | 0x86 << 8 | (byte) Code.Conv_Ovf_U1_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U2_Un = new OpCode (
				0xff << 0 | 0x87 << 8 | (byte) Code.Conv_Ovf_U2_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U4_Un = new OpCode (
				0xff << 0 | 0x88 << 8 | (byte) Code.Conv_Ovf_U4_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U8_Un = new OpCode (
				0xff << 0 | 0x89 << 8 | (byte) Code.Conv_Ovf_U8_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Conv_Ovf_I_Un = new OpCode (
				0xff << 0 | 0x8a << 8 | (byte) Code.Conv_Ovf_I_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U_Un = new OpCode (
				0xff << 0 | 0x8b << 8 | (byte) Code.Conv_Ovf_U_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Box = new OpCode (
				0xff << 0 | 0x8c << 8 | (byte) Code.Box << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Newarr = new OpCode (
				0xff << 0 | 0x8d << 8 | (byte) Code.Newarr << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Ldlen = new OpCode (
				0xff << 0 | 0x8e << 8 | (byte) Code.Ldlen << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelema = new OpCode (
				0xff << 0 | 0x8f << 8 | (byte) Code.Ldelema << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_I1 = new OpCode (
				0xff << 0 | 0x90 << 8 | (byte) Code.Ldelem_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_U1 = new OpCode (
				0xff << 0 | 0x91 << 8 | (byte) Code.Ldelem_U1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_I2 = new OpCode (
				0xff << 0 | 0x92 << 8 | (byte) Code.Ldelem_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_U2 = new OpCode (
				0xff << 0 | 0x93 << 8 | (byte) Code.Ldelem_U2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_I4 = new OpCode (
				0xff << 0 | 0x94 << 8 | (byte) Code.Ldelem_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_U4 = new OpCode (
				0xff << 0 | 0x95 << 8 | (byte) Code.Ldelem_U4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_I8 = new OpCode (
				0xff << 0 | 0x96 << 8 | (byte) Code.Ldelem_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Ldelem_I = new OpCode (
				0xff << 0 | 0x97 << 8 | (byte) Code.Ldelem_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldelem_R4 = new OpCode (
				0xff << 0 | 0x98 << 8 | (byte) Code.Ldelem_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushr4 << 24);

			public static readonly OpCode Ldelem_R8 = new OpCode (
				0xff << 0 | 0x99 << 8 | (byte) Code.Ldelem_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Ldelem_Ref = new OpCode (
				0xff << 0 | 0x9a << 8 | (byte) Code.Ldelem_Ref << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Pushref << 24);

			public static readonly OpCode Stelem_I = new OpCode (
				0xff << 0 | 0x9b << 8 | (byte) Code.Stelem_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_I1 = new OpCode (
				0xff << 0 | 0x9c << 8 | (byte) Code.Stelem_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_I2 = new OpCode (
				0xff << 0 | 0x9d << 8 | (byte) Code.Stelem_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_I4 = new OpCode (
				0xff << 0 | 0x9e << 8 | (byte) Code.Stelem_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_I8 = new OpCode (
				0xff << 0 | 0x9f << 8 | (byte) Code.Stelem_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popi8 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_R4 = new OpCode (
				0xff << 0 | 0xa0 << 8 | (byte) Code.Stelem_R4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popr4 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_R8 = new OpCode (
				0xff << 0 | 0xa1 << 8 | (byte) Code.Stelem_R8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popr8 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stelem_Ref = new OpCode (
				0xff << 0 | 0xa2 << 8 | (byte) Code.Stelem_Ref << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popref_popi_popref << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldelem_Any = new OpCode (
				0xff << 0 | 0xa3 << 8 | (byte) Code.Ldelem_Any << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref_popi << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Stelem_Any = new OpCode (
				0xff << 0 | 0xa4 << 8 | (byte) Code.Stelem_Any << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref_popi_popref << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Unbox_Any = new OpCode (
				0xff << 0 | 0xa5 << 8 | (byte) Code.Unbox_Any << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Conv_Ovf_I1 = new OpCode (
				0xff << 0 | 0xb3 << 8 | (byte) Code.Conv_Ovf_I1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U1 = new OpCode (
				0xff << 0 | 0xb4 << 8 | (byte) Code.Conv_Ovf_U1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I2 = new OpCode (
				0xff << 0 | 0xb5 << 8 | (byte) Code.Conv_Ovf_I2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U2 = new OpCode (
				0xff << 0 | 0xb6 << 8 | (byte) Code.Conv_Ovf_U2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I4 = new OpCode (
				0xff << 0 | 0xb7 << 8 | (byte) Code.Conv_Ovf_I4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U4 = new OpCode (
				0xff << 0 | 0xb8 << 8 | (byte) Code.Conv_Ovf_U4 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I8 = new OpCode (
				0xff << 0 | 0xb9 << 8 | (byte) Code.Conv_Ovf_I8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Conv_Ovf_U8 = new OpCode (
				0xff << 0 | 0xba << 8 | (byte) Code.Conv_Ovf_U8 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi8 << 24);

			public static readonly OpCode Refanyval = new OpCode (
				0xff << 0 | 0xc2 << 8 | (byte) Code.Refanyval << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ckfinite = new OpCode (
				0xff << 0 | 0xc3 << 8 | (byte) Code.Ckfinite << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushr8 << 24);

			public static readonly OpCode Mkrefany = new OpCode (
				0xff << 0 | 0xc6 << 8 | (byte) Code.Mkrefany << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldtoken = new OpCode (
				0xff << 0 | 0xd0 << 8 | (byte) Code.Ldtoken << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineTok << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_U2 = new OpCode (
				0xff << 0 | 0xd1 << 8 | (byte) Code.Conv_U2 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_U1 = new OpCode (
				0xff << 0 | 0xd2 << 8 | (byte) Code.Conv_U1 << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_I = new OpCode (
				0xff << 0 | 0xd3 << 8 | (byte) Code.Conv_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_I = new OpCode (
				0xff << 0 | 0xd4 << 8 | (byte) Code.Conv_Ovf_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Conv_Ovf_U = new OpCode (
				0xff << 0 | 0xd5 << 8 | (byte) Code.Conv_Ovf_U << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Add_Ovf = new OpCode (
				0xff << 0 | 0xd6 << 8 | (byte) Code.Add_Ovf << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Add_Ovf_Un = new OpCode (
				0xff << 0 | 0xd7 << 8 | (byte) Code.Add_Ovf_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Mul_Ovf = new OpCode (
				0xff << 0 | 0xd8 << 8 | (byte) Code.Mul_Ovf << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Mul_Ovf_Un = new OpCode (
				0xff << 0 | 0xd9 << 8 | (byte) Code.Mul_Ovf_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Sub_Ovf = new OpCode (
				0xff << 0 | 0xda << 8 | (byte) Code.Sub_Ovf << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Sub_Ovf_Un = new OpCode (
				0xff << 0 | 0xdb << 8 | (byte) Code.Sub_Ovf_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Endfinally = new OpCode (
				0xff << 0 | 0xdc << 8 | (byte) Code.Endfinally << 16 | (byte) FlowControl.Return << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Leave = new OpCode (
				0xff << 0 | 0xdd << 8 | (byte) Code.Leave << 16 | (byte) FlowControl.Branch << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineBrTarget << 8 | (byte) StackBehaviour.PopAll << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Leave_S = new OpCode (
				0xff << 0 | 0xde << 8 | (byte) Code.Leave_S << 16 | (byte) FlowControl.Branch << 24,
				(byte) OpCodeType.Macro << 0 | (byte) OperandType.ShortInlineBrTarget << 8 | (byte) StackBehaviour.PopAll << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Stind_I = new OpCode (
				0xff << 0 | 0xdf << 8 | (byte) Code.Stind_I << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Conv_U = new OpCode (
				0xff << 0 | 0xe0 << 8 | (byte) Code.Conv_U << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Arglist = new OpCode (
				0xfe << 0 | 0x00 << 8 | (byte) Code.Arglist << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ceq = new OpCode (
				0xfe << 0 | 0x01 << 8 | (byte) Code.Ceq << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Cgt = new OpCode (
				0xfe << 0 | 0x02 << 8 | (byte) Code.Cgt << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Cgt_Un = new OpCode (
				0xfe << 0 | 0x03 << 8 | (byte) Code.Cgt_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Clt = new OpCode (
				0xfe << 0 | 0x04 << 8 | (byte) Code.Clt << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Clt_Un = new OpCode (
				0xfe << 0 | 0x05 << 8 | (byte) Code.Clt_Un << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1_pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldftn = new OpCode (
				0xfe << 0 | 0x06 << 8 | (byte) Code.Ldftn << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldvirtftn = new OpCode (
				0xfe << 0 | 0x07 << 8 | (byte) Code.Ldvirtftn << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineMethod << 8 | (byte) StackBehaviour.Popref << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Ldarg = new OpCode (
				0xfe << 0 | 0x09 << 8 | (byte) Code.Ldarg << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineArg << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldarga = new OpCode (
				0xfe << 0 | 0x0a << 8 | (byte) Code.Ldarga << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineArg << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Starg = new OpCode (
				0xfe << 0 | 0x0b << 8 | (byte) Code.Starg << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineArg << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Ldloc = new OpCode (
				0xfe << 0 | 0x0c << 8 | (byte) Code.Ldloc << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineVar << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push1 << 24);

			public static readonly OpCode Ldloca = new OpCode (
				0xfe << 0 | 0x0d << 8 | (byte) Code.Ldloca << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineVar << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Stloc = new OpCode (
				0xfe << 0 | 0x0e << 8 | (byte) Code.Stloc << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineVar << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Localloc = new OpCode (
				0xfe << 0 | 0x0f << 8 | (byte) Code.Localloc << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Endfilter = new OpCode (
				0xfe << 0 | 0x11 << 8 | (byte) Code.Endfilter << 16 | (byte) FlowControl.Return << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Unaligned = new OpCode (
				0xfe << 0 | 0x12 << 8 | (byte) Code.Unaligned << 16 | (byte) FlowControl.Meta << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.ShortInlineI << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Volatile = new OpCode (
				0xfe << 0 | 0x13 << 8 | (byte) Code.Volatile << 16 | (byte) FlowControl.Meta << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Tail = new OpCode (
				0xfe << 0 | 0x14 << 8 | (byte) Code.Tail << 16 | (byte) FlowControl.Meta << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Initobj = new OpCode (
				0xfe << 0 | 0x15 << 8 | (byte) Code.Initobj << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Constrained = new OpCode (
				0xfe << 0 | 0x16 << 8 | (byte) Code.Constrained << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Cpblk = new OpCode (
				0xfe << 0 | 0x17 << 8 | (byte) Code.Cpblk << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Initblk = new OpCode (
				0xfe << 0 | 0x18 << 8 | (byte) Code.Initblk << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Popi_popi_popi << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode No = new OpCode (
				0xfe << 0 | 0x19 << 8 | (byte) Code.No << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.ShortInlineI << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Rethrow = new OpCode (
				0xfe << 0 | 0x1a << 8 | (byte) Code.Rethrow << 16 | (byte) FlowControl.Throw << 24,
				(byte) OpCodeType.Objmodel << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);

			public static readonly OpCode Sizeof = new OpCode (
				0xfe << 0 | 0x1c << 8 | (byte) Code.Sizeof << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineType << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Refanytype = new OpCode (
				0xfe << 0 | 0x1d << 8 | (byte) Code.Refanytype << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Primitive << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop1 << 16 | (byte) StackBehaviour.Pushi << 24);

			public static readonly OpCode Readonly = new OpCode (
				0xfe << 0 | 0x1e << 8 | (byte) Code.Readonly << 16 | (byte) FlowControl.Next << 24,
				(byte) OpCodeType.Prefix << 0 | (byte) OperandType.InlineNone << 8 | (byte) StackBehaviour.Pop0 << 16 | (byte) StackBehaviour.Push0 << 24);
		}
		static class OpCodeNames {

			internal static readonly string [] names;

			static OpCodeNames ()
			{
				var table = new byte [] {
					3, 110, 111, 112,
					5, 98, 114, 101, 97, 107,
					7, 108, 100, 97, 114, 103, 46, 48,
					7, 108, 100, 97, 114, 103, 46, 49,
					7, 108, 100, 97, 114, 103, 46, 50,
					7, 108, 100, 97, 114, 103, 46, 51,
					7, 108, 100, 108, 111, 99, 46, 48,
					7, 108, 100, 108, 111, 99, 46, 49,
					7, 108, 100, 108, 111, 99, 46, 50,
					7, 108, 100, 108, 111, 99, 46, 51,
					7, 115, 116, 108, 111, 99, 46, 48,
					7, 115, 116, 108, 111, 99, 46, 49,
					7, 115, 116, 108, 111, 99, 46, 50,
					7, 115, 116, 108, 111, 99, 46, 51,
					7, 108, 100, 97, 114, 103, 46, 115,
					8, 108, 100, 97, 114, 103, 97, 46, 115,
					7, 115, 116, 97, 114, 103, 46, 115,
					7, 108, 100, 108, 111, 99, 46, 115,
					8, 108, 100, 108, 111, 99, 97, 46, 115,
					7, 115, 116, 108, 111, 99, 46, 115,
					6, 108, 100, 110, 117, 108, 108,
					9, 108, 100, 99, 46, 105, 52, 46, 109, 49,
					8, 108, 100, 99, 46, 105, 52, 46, 48,
					8, 108, 100, 99, 46, 105, 52, 46, 49,
					8, 108, 100, 99, 46, 105, 52, 46, 50,
					8, 108, 100, 99, 46, 105, 52, 46, 51,
					8, 108, 100, 99, 46, 105, 52, 46, 52,
					8, 108, 100, 99, 46, 105, 52, 46, 53,
					8, 108, 100, 99, 46, 105, 52, 46, 54,
					8, 108, 100, 99, 46, 105, 52, 46, 55,
					8, 108, 100, 99, 46, 105, 52, 46, 56,
					8, 108, 100, 99, 46, 105, 52, 46, 115,
					6, 108, 100, 99, 46, 105, 52,
					6, 108, 100, 99, 46, 105, 56,
					6, 108, 100, 99, 46, 114, 52,
					6, 108, 100, 99, 46, 114, 56,
					3, 100, 117, 112,
					3, 112, 111, 112,
					3, 106, 109, 112,
					4, 99, 97, 108, 108,
					5, 99, 97, 108, 108, 105,
					3, 114, 101, 116,
					4, 98, 114, 46, 115,
					9, 98, 114, 102, 97, 108, 115, 101, 46, 115,
					8, 98, 114, 116, 114, 117, 101, 46, 115,
					5, 98, 101, 113, 46, 115,
					5, 98, 103, 101, 46, 115,
					5, 98, 103, 116, 46, 115,
					5, 98, 108, 101, 46, 115,
					5, 98, 108, 116, 46, 115,
					8, 98, 110, 101, 46, 117, 110, 46, 115,
					8, 98, 103, 101, 46, 117, 110, 46, 115,
					8, 98, 103, 116, 46, 117, 110, 46, 115,
					8, 98, 108, 101, 46, 117, 110, 46, 115,
					8, 98, 108, 116, 46, 117, 110, 46, 115,
					2, 98, 114,
					7, 98, 114, 102, 97, 108, 115, 101,
					6, 98, 114, 116, 114, 117, 101,
					3, 98, 101, 113,
					3, 98, 103, 101,
					3, 98, 103, 116,
					3, 98, 108, 101,
					3, 98, 108, 116,
					6, 98, 110, 101, 46, 117, 110,
					6, 98, 103, 101, 46, 117, 110,
					6, 98, 103, 116, 46, 117, 110,
					6, 98, 108, 101, 46, 117, 110,
					6, 98, 108, 116, 46, 117, 110,
					6, 115, 119, 105, 116, 99, 104,
					8, 108, 100, 105, 110, 100, 46, 105, 49,
					8, 108, 100, 105, 110, 100, 46, 117, 49,
					8, 108, 100, 105, 110, 100, 46, 105, 50,
					8, 108, 100, 105, 110, 100, 46, 117, 50,
					8, 108, 100, 105, 110, 100, 46, 105, 52,
					8, 108, 100, 105, 110, 100, 46, 117, 52,
					8, 108, 100, 105, 110, 100, 46, 105, 56,
					7, 108, 100, 105, 110, 100, 46, 105,
					8, 108, 100, 105, 110, 100, 46, 114, 52,
					8, 108, 100, 105, 110, 100, 46, 114, 56,
					9, 108, 100, 105, 110, 100, 46, 114, 101, 102,
					9, 115, 116, 105, 110, 100, 46, 114, 101, 102,
					8, 115, 116, 105, 110, 100, 46, 105, 49,
					8, 115, 116, 105, 110, 100, 46, 105, 50,
					8, 115, 116, 105, 110, 100, 46, 105, 52,
					8, 115, 116, 105, 110, 100, 46, 105, 56,
					8, 115, 116, 105, 110, 100, 46, 114, 52,
					8, 115, 116, 105, 110, 100, 46, 114, 56,
					3, 97, 100, 100,
					3, 115, 117, 98,
					3, 109, 117, 108,
					3, 100, 105, 118,
					6, 100, 105, 118, 46, 117, 110,
					3, 114, 101, 109,
					6, 114, 101, 109, 46, 117, 110,
					3, 97, 110, 100,
					2, 111, 114,
					3, 120, 111, 114,
					3, 115, 104, 108,
					3, 115, 104, 114,
					6, 115, 104, 114, 46, 117, 110,
					3, 110, 101, 103,
					3, 110, 111, 116,
					7, 99, 111, 110, 118, 46, 105, 49,
					7, 99, 111, 110, 118, 46, 105, 50,
					7, 99, 111, 110, 118, 46, 105, 52,
					7, 99, 111, 110, 118, 46, 105, 56,
					7, 99, 111, 110, 118, 46, 114, 52,
					7, 99, 111, 110, 118, 46, 114, 56,
					7, 99, 111, 110, 118, 46, 117, 52,
					7, 99, 111, 110, 118, 46, 117, 56,
					8, 99, 97, 108, 108, 118, 105, 114, 116,
					5, 99, 112, 111, 98, 106,
					5, 108, 100, 111, 98, 106,
					5, 108, 100, 115, 116, 114,
					6, 110, 101, 119, 111, 98, 106,
					9, 99, 97, 115, 116, 99, 108, 97, 115, 115,
					6, 105, 115, 105, 110, 115, 116,
					9, 99, 111, 110, 118, 46, 114, 46, 117, 110,
					5, 117, 110, 98, 111, 120,
					5, 116, 104, 114, 111, 119,
					5, 108, 100, 102, 108, 100,
					6, 108, 100, 102, 108, 100, 97,
					5, 115, 116, 102, 108, 100,
					6, 108, 100, 115, 102, 108, 100,
					7, 108, 100, 115, 102, 108, 100, 97,
					6, 115, 116, 115, 102, 108, 100,
					5, 115, 116, 111, 98, 106,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 49, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 50, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 52, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 56, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 49, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 50, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 52, 46, 117, 110,
					14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 56, 46, 117, 110,
					13, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 46, 117, 110,
					13, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 46, 117, 110,
					3, 98, 111, 120,
					6, 110, 101, 119, 97, 114, 114,
					5, 108, 100, 108, 101, 110,
					7, 108, 100, 101, 108, 101, 109, 97,
					9, 108, 100, 101, 108, 101, 109, 46, 105, 49,
					9, 108, 100, 101, 108, 101, 109, 46, 117, 49,
					9, 108, 100, 101, 108, 101, 109, 46, 105, 50,
					9, 108, 100, 101, 108, 101, 109, 46, 117, 50,
					9, 108, 100, 101, 108, 101, 109, 46, 105, 52,
					9, 108, 100, 101, 108, 101, 109, 46, 117, 52,
					9, 108, 100, 101, 108, 101, 109, 46, 105, 56,
					8, 108, 100, 101, 108, 101, 109, 46, 105,
					9, 108, 100, 101, 108, 101, 109, 46, 114, 52,
					9, 108, 100, 101, 108, 101, 109, 46, 114, 56,
					10, 108, 100, 101, 108, 101, 109, 46, 114, 101, 102,
					8, 115, 116, 101, 108, 101, 109, 46, 105,
					9, 115, 116, 101, 108, 101, 109, 46, 105, 49,
					9, 115, 116, 101, 108, 101, 109, 46, 105, 50,
					9, 115, 116, 101, 108, 101, 109, 46, 105, 52,
					9, 115, 116, 101, 108, 101, 109, 46, 105, 56,
					9, 115, 116, 101, 108, 101, 109, 46, 114, 52,
					9, 115, 116, 101, 108, 101, 109, 46, 114, 56,
					10, 115, 116, 101, 108, 101, 109, 46, 114, 101, 102,
					10, 108, 100, 101, 108, 101, 109, 46, 97, 110, 121,
					10, 115, 116, 101, 108, 101, 109, 46, 97, 110, 121,
					9, 117, 110, 98, 111, 120, 46, 97, 110, 121,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 49,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 49,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 50,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 50,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 52,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 52,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 56,
					11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 56,
					9, 114, 101, 102, 97, 110, 121, 118, 97, 108,
					8, 99, 107, 102, 105, 110, 105, 116, 101,
					8, 109, 107, 114, 101, 102, 97, 110, 121,
					7, 108, 100, 116, 111, 107, 101, 110,
					7, 99, 111, 110, 118, 46, 117, 50,
					7, 99, 111, 110, 118, 46, 117, 49,
					6, 99, 111, 110, 118, 46, 105,
					10, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105,
					10, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117,
					7, 97, 100, 100, 46, 111, 118, 102,
					10, 97, 100, 100, 46, 111, 118, 102, 46, 117, 110,
					7, 109, 117, 108, 46, 111, 118, 102,
					10, 109, 117, 108, 46, 111, 118, 102, 46, 117, 110,
					7, 115, 117, 98, 46, 111, 118, 102,
					10, 115, 117, 98, 46, 111, 118, 102, 46, 117, 110,
					10, 101, 110, 100, 102, 105, 110, 97, 108, 108, 121,
					5, 108, 101, 97, 118, 101,
					7, 108, 101, 97, 118, 101, 46, 115,
					7, 115, 116, 105, 110, 100, 46, 105,
					6, 99, 111, 110, 118, 46, 117,
					7, 97, 114, 103, 108, 105, 115, 116,
					3, 99, 101, 113,
					3, 99, 103, 116,
					6, 99, 103, 116, 46, 117, 110,
					3, 99, 108, 116,
					6, 99, 108, 116, 46, 117, 110,
					5, 108, 100, 102, 116, 110,
					9, 108, 100, 118, 105, 114, 116, 102, 116, 110,
					5, 108, 100, 97, 114, 103,
					6, 108, 100, 97, 114, 103, 97,
					5, 115, 116, 97, 114, 103,
					5, 108, 100, 108, 111, 99,
					6, 108, 100, 108, 111, 99, 97,
					5, 115, 116, 108, 111, 99,
					8, 108, 111, 99, 97, 108, 108, 111, 99,
					9, 101, 110, 100, 102, 105, 108, 116, 101, 114,
					10, 117, 110, 97, 108, 105, 103, 110, 101, 100, 46,
					9, 118, 111, 108, 97, 116, 105, 108, 101, 46,
					5, 116, 97, 105, 108, 46,
					7, 105, 110, 105, 116, 111, 98, 106,
					12, 99, 111, 110, 115, 116, 114, 97, 105, 110, 101, 100, 46,
					5, 99, 112, 98, 108, 107,
					7, 105, 110, 105, 116, 98, 108, 107,
					3, 110, 111, 46,
					7, 114, 101, 116, 104, 114, 111, 119,
					6, 115, 105, 122, 101, 111, 102,
					10, 114, 101, 102, 97, 110, 121, 116, 121, 112, 101,
					9, 114, 101, 97, 100, 111, 110, 108, 121, 46,
				};

				names = new string [219];

				for (int i = 0, p = 0; i < names.Length; i++) {
					var buffer = new char [table [p++]];

					for (int j = 0; j < buffer.Length; j++)
						buffer [j] = (char) table [p++];

					names [i] = new string (buffer);
				}
			}
		}
	}
}

