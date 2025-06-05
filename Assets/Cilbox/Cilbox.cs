using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.IO;
#endif

// To add [Cilboxable] to your classes that you want exported.
public class CilboxableAttribute : Attribute { }

namespace Cilbox
{

	public enum StackType
	{
		Boolean,
		Sbyte,
		Byte,
		Short,
		Ushort,
		Int,
		Uint,
		Long,
		Ulong,
		Float,
		Double,
		Object,
		Address,
	}

	public class CilboxMethod
	{
		public bool disabled;
		public CilboxClass parentClass;
		public int MaxStackSize;
		public String methodName;
		public String fullSignature;
		public String[] methodLocals;
		public String[] methodLocalTypes;
		public byte[] byteCode;
		public bool isStatic;
		public String[] signatureParameters;
		public String[] signatureParameterTypes;

		public void Load( CilboxClass cclass, String name, String payload )
		{
			methodName = name;
			parentClass = cclass;
			OrderedDictionary methodProps = CilboxUtil.DeserializeDict( payload );

			var vl = CilboxUtil.DeserializeDict( (String)methodProps["locals"] );
			methodLocals = new String[vl.Count];
			methodLocalTypes = new String[vl.Count];
			int iid = 0;
			foreach( DictionaryEntry ln in vl )
			{
				methodLocals[iid] = (String)ln.Key;
				methodLocalTypes[iid] = (String)ln.Key;
			}

			var pl = (String)methodProps["body"];
			int bl = pl.Length/2;
			byteCode = new byte[bl];

			//Debug.Log( methodName + " " + pl );
			for( int i = 0; i < bl; i++ )
			{
				int v = CilboxUtil.IntFromHexChar( pl[i*2+0] );
				if( v < 0 ) break;
				byte b = (byte)v;
				v = CilboxUtil.IntFromHexChar( pl[i*2+1] );
				if( v < 0 ) break;
				b = (byte)(v | (b<<4));
				byteCode[i] = b;
			}

			MaxStackSize = Convert.ToInt32(((String)methodProps["maxStack"]));
			isStatic = Convert.ToInt32(((String)methodProps["isStatic"])) != 0;
			fullSignature = (String)methodProps["fullSignature"];
			OrderedDictionary od = CilboxUtil.DeserializeDict( (String)methodProps["parameters"] );
			signatureParameters = new String[od.Count];
			signatureParameterTypes = new String[od.Count];
			int sn = 0;
			foreach( DictionaryEntry v in od )
			{
				signatureParameters[sn] = (String)v.Key;
				signatureParameterTypes[sn] = (String)v.Value;
				sn++;
			}
		}
		public void Breakwarn( String message, int bytecodeplace )
		{
			// TODO: Add debugging info.
			Debug.LogWarning( $"Breakwarn: {message} Class: {parentClass.className}, Function: {methodName}, Bytecode: {bytecodeplace}" );
		}

		// This logic is probably incorrect.
		static public StackType StackTypeMaxPromote( StackType a, StackType b )
		{
			if( a < StackType.Int ) a = StackType.Int;
			if( b < StackType.Int ) b = StackType.Int;
			StackType ret = a;
			if( ret < b ) ret = b;

			// Could be Int, Uint, Long, Ulong, Float Double or Object.  But if non-integer must be same type to prompte.
			// I think?
			if( ret >= StackType.Float && a != b )
				throw new Exception( $"Invalid stack conversion from {a} to {b}" );

			return a;
		}

		public static readonly Type [] TypeFromStackType = new Type[] {
			typeof(sbyte), typeof(byte), typeof(short), typeof( ushort), typeof( int ),
			typeof(uint), typeof(long), typeof(ulong), typeof(bool), typeof(object) };

		[StructLayout(LayoutKind.Explicit)]
		public struct StackElement
		{
			[FieldOffset(0)]public StackType type;
			[FieldOffset(8)]public Boolean b;
			[FieldOffset(8)]public float f;
			[FieldOffset(8)]public double d;
			[FieldOffset(8)]public int i;
			[FieldOffset(8)]public uint u;
			[FieldOffset(8)]public long l;
			[FieldOffset(8)]public ulong e;
			[FieldOffset(16)]public object o;

			public StackElement Load( object o )
			{
				switch( o )
				{
				case sbyte t0: i = (sbyte)o;	type = StackType.Sbyte; break;
				case byte  t1: i = (byte)o;		type = StackType.Byte; break;
				case short t2: i = (short)o;	type = StackType.Short; break;
				case ushort t3: i = (ushort)o;	type = StackType.Ushort; break;
				case int t4: i = (int)o;		type = StackType.Int; break;
				case uint t5: u = (uint)o;		type = StackType.Uint; break;
				case long t6: l = (long)o;		type = StackType.Long; break;
				case ulong t7: e = (ulong)o;	type = StackType.Ulong; break;
				case float t8: f = (float)o;	type = StackType.Float; break;
				case double t9: d = (ulong)o;	type = StackType.Double; break;
				case bool ta: i = ((bool)o) ? 1 : 0; type = StackType.Boolean; break;
				default: this.o = o; type = StackType.Object; break;
				}
				return this;
			}

			public StackElement LoadObject( object o ) { this.o = o; type = StackType.Object; return this; }
			public StackElement LoadUshort( uint u ) { this.u = u; type = StackType.Ushort; return this; }
			public StackElement LoadByte( uint u ) { this.u = u; type = StackType.Byte; return this; }
			public StackElement LoadInt( int i ) { this.i = i; type = StackType.Int; return this; }
			public StackElement LoadUint( uint u ) { this.u = u; type = StackType.Uint; return this; }
			public StackElement LoadLong( long l ) { this.l = l; type = StackType.Long; return this; }
			public StackElement LoadUlong( ulong e ) { this.e = e; type = StackType.Ulong; return this; }
			public StackElement LoadFloat( float f ) { this.f = f; type = StackType.Float; return this; }
			public StackElement LoadDouble( double d ) { this.d = d; type = StackType.Double; return this; }

			public StackElement LoadUlongType( ulong e, StackType t ) { this.e = e; type = t; return this; }
			public StackElement LoadLongType( long l, StackType t ) { this.l = l; type = t; return this; }

			public Type GetInnerType()
			{
				if( type == StackType.Object )
					return o.GetType();
				else
					return TypeFromStackType[(int)type];
			}

			public void Unbox( object i, StackType st )
			{
				type = st;
				switch( st )
				{
				case StackType.Sbyte: this.u = (uint)(sbyte)i; break;
				case StackType.Byte: this.u = (uint)(byte)i; break;
				case StackType.Short: this.u = (uint)(short)i; break;
				case StackType.Ushort: this.u = (uint)(ushort)i; break;
				case StackType.Int: this.i = (int)i; break;
				case StackType.Uint: this.u = (uint)i; break;
				case StackType.Long: this.l = (long)i; break;
				case StackType.Ulong: this.e = (ulong)i; break;
				case StackType.Float: this.f = (float)i; break;
				case StackType.Double: this.d = (double)i; break;
				case StackType.Boolean: this.i = ((bool)i)?1:0; break;
				default: this.o = i; break;
				}
			}

			public object AsObject()
			{
				switch( type )
				{
				case StackType.Sbyte: return (sbyte)i;
				case StackType.Byte: return (byte)i;
				case StackType.Short: return (short)i;
				case StackType.Ushort: return (ushort)i;
				case StackType.Int: return (int)i;
				case StackType.Uint: return (uint)u;
				case StackType.Long: return (long)l;
				case StackType.Ulong: return (ulong)e;
				case StackType.Float: return (float)f;
				case StackType.Double: return (double)d;
				case StackType.Boolean: return (bool)b;
				case StackType.Address: return Dereference().o;
				default: return o;
				}
			}

			public StackElement Dereference()
			{
				return ((StackElement[])o)[i];
			}

			static public StackElement CreateReference( StackElement[] array, uint index )
			{
				StackElement ret = new StackElement();
				ret.type = StackType.Address;
				// XXX TODO: Is this correct? Do references to references dereference, or make more references?
				//if( array[index].type == StackType.Address )
				//{
				//	ret.u = array[index].u;
				//	ret.o = array[index].o;
				//}
				//else
				{
					ret.u = index;
					ret.o = array;
				}
				return ret;
			}
		}


		public object Interpret( CilboxProxy ths, object [] parametersIn )
		{
			if( byteCode == null || byteCode.Length == 0 || disabled ) return null;
			Cilbox box = parentClass.box;
	
			if( box.nestingDepth > 1000 ) throw new Exception( "Error: Interpreted stack overflow in " + methodName );
			if( box.nestingDepth == 0 )
			{
				box.stepsThisInvoke = 0;
				box.startTime = System.Diagnostics.Stopwatch.GetTimestamp();
			}
			box.nestingDepth++;

			// Do the magic.

			// Uncomment for debug.
			//String stbc = ""; for( int i = 0; i < byteCode.Length; i++ ) stbc += byteCode[i].ToString("X2") + " "; Debug.Log( "INTERPRETING " + methodName + " VALS:" + stbc + " MAX: " + MaxStackSize );
			//Debug.Log( ths.fields );

			StackElement [] stack = new StackElement[MaxStackSize];
			StackElement [] localVars = new StackElement[methodLocals.Length];

			int sp = 0;

			object [] parameters;
			if( isStatic )
				parameters = parametersIn;
			else
			{
				int plen = 0;
				if( parametersIn != null ) plen = parametersIn.Length;
				parameters = new object[plen+1];
				parameters[0] = ths;
				int p;
				for( p = 0; p < plen; p++ )
					parameters[p+1] = parametersIn[p];
			}

			bool cont = true;
			int ctr = 0;
			int pc = 0;
			try
			{
				do
				{
					//Debug.Log( "PC@"+pc+"/"+byteCode.Length);
					byte b = byteCode[pc];

					// Uncomment for debugging.
					//String stackSt = ""; for( int sk = 0; sk < stack.Length; sk++ ) { stackSt += "/"; if( sk == sp-1 ) stackSt += ">"; stackSt += stack[sk].AsObject(); if( sk == sp-1 ) stackSt += "<"; }
					//int icopy = pc; CilboxUtil.OpCodes.OpCode opc = CilboxUtil.OpCodes.ReadOpCode ( byteCode, ref icopy );
					//Debug.Log( "Bytecode " + opc + " (" + b.ToString("X2") + ") @ " + pc + "/" + byteCode.Length + " " + stackSt);

					pc++;

					switch( b )
					{
					case 0x00: break; // nop
					case 0x01: cont = false; Breakwarn( "Debug Break", pc ); break; // break
					case 0x02: stack[sp++].Load( parameters[0] ); break; //ldarg.0
					case 0x03: stack[sp++].Load( parameters[1] ); break; //ldarg.1
					case 0x04: stack[sp++].Load( parameters[2] ); break; //ldarg.2
					case 0x05: stack[sp++].Load( parameters[3] ); break; //ldarg.3
					case 0x06: stack[sp++] = localVars[0]; break; //ldloc.0
					case 0x07: stack[sp++] = localVars[1]; break; //ldloc.1
					case 0x08: stack[sp++] = localVars[2]; break; //ldloc.2
					case 0x09: stack[sp++] = localVars[3]; break; //ldloc.3
					case 0x0a: localVars[0] = stack[--sp]; break; //stloc.0
					case 0x0b: localVars[1] = stack[--sp]; break; //stloc.1
					case 0x0c: localVars[2] = stack[--sp]; break; //stloc.2
					case 0x0d: localVars[3] = stack[--sp]; break; //stloc.3
					case 0x0f: stack[sp++].Load( parameters[byteCode[pc++]] ); break; // ldarga.s <uint8 (argNum)>
					case 0x11: stack[sp++] = localVars[byteCode[pc++]]; break; //ldloc.s
					case 0x12:
					{
						uint whichLocal = byteCode[pc++];
						//Debug.Log( $"Pushing var {whichLocal}'s address (it is {localVars[whichLocal].type} {localVars[whichLocal].AsObject().GetType()} {localVars[whichLocal].AsObject()}) to stack\n" );
						stack[sp++] = StackElement.CreateReference( localVars, whichLocal );
						break; //ldloca.s // Load address of local variable.
					}
					case 0x13: localVars[byteCode[pc++]] = stack[--sp]; break; //stloc.s
					//case 0x0e: stack[sp++] = parameters[byteCode[pc++]]; break; //ldarg.0
					//case 0x0e: stack[sp++] = parameters[byteCode[pc++]]; break; //ldarg.0
					// Some more...
					case 0x14: stack[sp++].Load( null ); break; // ldnull
					case 0x15: stack[sp++].LoadInt( -1 ); break; // ldc.i4.m1
					case 0x16: stack[sp++].LoadInt( 0 ); break; // ldc.i4.0
					case 0x17: stack[sp++].LoadInt( 1 ); break; // ldc.i4.1
					case 0x18: stack[sp++].LoadInt( 2 ); break; // ldc.i4.2
					case 0x19: stack[sp++].LoadInt( 3 ); break; // ldc.i4.3
					case 0x1a: stack[sp++].LoadInt( 4 ); break; // ldc.i4.4
					case 0x1b: stack[sp++].LoadInt( 5 ); break; // ldc.i4.5
					case 0x1c: stack[sp++].LoadInt( 6 ); break; // ldc.i4.6
					case 0x1d: stack[sp++].LoadInt( 7 ); break; // ldc.i4.7
					case 0x1e: stack[sp++].LoadInt( 8 ); break; // ldc.i4.8

					case 0x1f: stack[sp++].LoadInt( (sbyte)byteCode[pc++] ); break; // ldc.i4.s <int8>
					case 0x20: stack[sp++].LoadInt( (int)BytecodeAsU32( ref pc ) ); break; // ldc.i4 <int32>
					case 0x21: stack[sp++].Load( (long)BytecodeAs64( ref pc ) ); break; // ldc.i8 <int64>
					case 0x22: stack[sp++].LoadFloat( CilboxUtil.IntFloatConverter.ConvertUtoF(BytecodeAsU32( ref pc ) ) ); break; // ldc.r4 <float32 (num)>
					case 0x23: stack[sp++].LoadDouble( CilboxUtil.IntFloatConverter.ConvertEtoD(BytecodeAs64( ref pc ) ) ); break; // ldc.r4 <float32 (num)>
					// 0x24 does not exist.
					case 0x25: stack[sp] = stack[sp-1]; sp++; break; // dup
					case 0x26: sp--; break; // pop

					case 0x27: //jmp
					case 0x28: //call
					case 0x29: //calli
					case 0x73: //newobj
					case 0x6F: //callvirt
					{
						uint bc = (b == 0x29) ? stack[--sp].u : BytecodeAsU32( ref pc );
						object iko = null; // Returned value.
						CilMetadataTokenInfo dt = box.metadatas[bc];

						bool isVoid = false;
						MethodBase st;

						if( !dt.isValid )
						{
							throw new Exception( "Error, function " + dt.Name + " Not found in " + parentClass.className + ":" + fullSignature );
						}

						if( !dt.isNative )
						{
							// Sentinel.  interpretiveMethod will contain what method to interpret.
							// interpretiveMethodClass
							CilboxClass targetClass = box.classesList[dt.interpretiveMethodClass];
							CilboxMethod targetMethod = targetClass.methods[dt.interpretiveMethod];
							if( targetMethod == null )
							{
								throw( new Exception( $"Function {dt.fields[2]} not found" ) );
							}
							int numParams = targetMethod.signatureParameters.Length;
							object [] cparameters = new object[numParams];
							int i;
							for( i = 0; i < numParams; i++ )
							{
								cparameters[i] = stack[--sp].AsObject();
							}
							CilboxProxy callThis = null;
							if( !targetMethod.isStatic )
							{
								callThis = (CilboxProxy)stack[--sp].AsObject();
							}
							iko = targetMethod.Interpret( callThis, cparameters );
							//public object Interpret( CilboxProxy ths, object [] parametersIn )
							//signatureParameters // count
						}
						else
						{
							//st = dt.assembly.ManifestModule.ResolveMethod((int)dt.nativeToken);
							st = dt.nativeMethod;
							if( st is MethodInfo )
								isVoid = ((MethodInfo)st).ReturnType == typeof(void);

							ParameterInfo [] pa = st.GetParameters();
							//MethodInfo mi = (MethodInfo)st;
							int numFields = pa.Length;
							object callthis = null;
							object [] callpar = new object[numFields];
							StackElement callthis_se = new StackElement{};
							//callthis_se.type = StackType.Object; // Default to not a reference.
							StackElement [] callpar_se = new StackElement[numFields];
							int ik;
							for( ik = 0; ik < numFields; ik++ )
								callpar[numFields-ik-1] = (callpar_se[numFields-ik-1] = stack[--sp]).AsObject();
							if( st.IsConstructor )
								callthis = Activator.CreateInstance(st.DeclaringType);
							else if( !st.IsStatic )
								callthis = (callthis_se = stack[--sp]).AsObject();

							if( st.IsConstructor )
								iko = ((ConstructorInfo)st).Invoke( callpar );
							else
								iko = st.Invoke( callthis, callpar );

							// Possibly copy back any references.
							for( ik = 0; ik < numFields; ik++ )
								if( callpar_se[ik].type == StackType.Address )
									callpar_se[ik].Dereference().Load( callpar[numFields-ik-1] );
							if( callthis_se.type == StackType.Address )
								callthis_se.Load( callthis );
						}

						if( !isVoid ) stack[sp++].Load( iko );
						if( b == 0x27 )
						{
							// This is returning from a jump, so immediatelb abort.
							if( isVoid ) stack[sp++].Load( null );
							cont = false;
						}
						break;
					}
					case 0x2a: cont = false; break; // ret

					case 0x2b: pc += (sbyte)byteCode[pc] + 1; break; //br.s
					case 0x38: pc += (int)BytecodeAsU32( ref pc ) + 1; break; // br

					case 0x2c: case 0x39: // brfalse.s, brnull.s, brzero.s - is it zero, null or  / brfalse
					case 0x2d: case 0x3a: // brinst.s, brtrue.s / btrue
					{
						StackElement s = stack[--sp];
						int iop = b - 0x2c;
						if( iop >= 0x38 ) iop -= 0xd;
						int offset = (b >= 0x38) ? (int)BytecodeAsU32( ref pc ) : (sbyte)byteCode[pc];
						pc++;
						switch( iop )
						{
							case 0: if( ( s.type == StackType.Object && s.o == null ) || s.i == 0 ) pc += offset; break;
							case 1: if( ( s.type == StackType.Object && s.o != null ) || s.i != 0 ) pc += offset; break;
						}
						break;
					}
					case 0x2e: case 0x3b: // beq.s / beq
					case 0x2f: case 0x3c: // bge.s
					case 0x30: case 0x3d: // bgt.s
					case 0x31: case 0x3e: // ble.s
					case 0x32: case 0x3f: // blt.s
					case 0x33: case 0x40: // bne.un.s
					case 0x34: case 0x41: // bge.un.s
					case 0x35: case 0x42: // bgt.un.s
					case 0x36: case 0x43: // ble.un.s
					case 0x37: case 0x44: // blt.un.s
					{
						StackElement sb = stack[--sp]; StackElement sa = stack[--sp];
						int iop = b - 0x2e;
						if( iop >= 0x38 ) iop -= 0xd;
						int joffset = (b >= 0x38) ? (int)BytecodeAsU32( ref pc ) : (sbyte)byteCode[pc];
						pc++;
						switch( sb.type )
						{
						case StackType.Sbyte: case StackType.Short: case StackType.Int:
							switch( iop )
							{
							case 0: if( sa.i == sb.i ) pc += joffset; break;
							case 1: if( sa.i >= sb.i ) pc += joffset; break;
							case 2: if( sa.i >  sb.i ) pc += joffset; break;
							case 3: if( sa.i <= sb.i ) pc += joffset; break;
							case 4: if( sa.i <  sb.i ) pc += joffset; break;
							case 5: if( sa.e != sb.e ) pc += joffset; break;
							case 6: if( sa.e >= sb.e ) pc += joffset; break;
							case 7: if( sa.e >  sb.e ) pc += joffset; break;
							case 8: if( sa.e <= sb.e ) pc += joffset; break;
							case 9: if( sa.e <  sb.e ) pc += joffset; break;
							} break;
						case StackType.Byte: case StackType.Ushort: case StackType.Uint: case StackType.Ulong:
							switch( iop )	{
							case 0: if( sa.e == sb.e ) pc += joffset; break;
							case 1: if( sa.e >= sb.e ) pc += joffset; break;
							case 2: if( sa.e >  sb.e ) pc += joffset; break;
							case 3: if( sa.e <= sb.e ) pc += joffset; break;
							case 4: if( sa.e <  sb.e ) pc += joffset; break;
							case 5: if( sa.e != sb.e ) pc += joffset; break;
							case 6: if( sa.e >= sb.e ) pc += joffset; break;
							case 7: if( sa.e >  sb.e ) pc += joffset; break;
							case 8: if( sa.e <= sb.e ) pc += joffset; break;
							case 9: if( sa.e <  sb.e ) pc += joffset; break;
							} break;
						case StackType.Long:
							switch( iop )	{
							case 0: if( sa.l == sb.l ) pc += joffset; break;
							case 1: if( sa.l >= sb.l ) pc += joffset; break;
							case 2: if( sa.l >  sb.l ) pc += joffset; break;
							case 3: if( sa.l <= sb.l ) pc += joffset; break;
							case 4: if( sa.l <  sb.l ) pc += joffset; break;
							case 5: if( sa.e != sb.e ) pc += joffset; break;
							case 6: if( sa.e >= sb.e ) pc += joffset; break;
							case 7: if( sa.e >  sb.e ) pc += joffset; break;
							case 8: if( sa.e <= sb.e ) pc += joffset; break;
							case 9: if( sa.e <  sb.e ) pc += joffset; break;
							} break;
						case StackType.Float:
							switch( iop )	{
							case 0: if( sa.f == sb.f ) pc += joffset; break;
							case 1: if( sa.f >= sb.f ) pc += joffset; break;
							case 2: if( sa.f >  sb.f ) pc += joffset; break;
							case 3: if( sa.f <= sb.f ) pc += joffset; break;
							case 4: if( sa.f <  sb.f ) pc += joffset; break;
							case 5: if( sa.f != sb.f ) pc += joffset; break;
							case 6: if( sa.f >= sb.f ) pc += joffset; break;
							case 7: if( sa.f >  sb.f ) pc += joffset; break;
							case 8: if( sa.f <= sb.f ) pc += joffset; break;
							case 9: if( sa.f <  sb.f ) pc += joffset; break;
							} break;
						case StackType.Double:
							switch( iop )	{
							case 0: if( sa.d == sb.d ) pc += joffset; break;
							case 1: if( sa.d >= sb.d ) pc += joffset; break;
							case 2: if( sa.d >  sb.d ) pc += joffset; break;
							case 3: if( sa.d <= sb.d ) pc += joffset; break;
							case 4: if( sa.d <  sb.d ) pc += joffset; break;
							case 5: if( sa.d != sb.d ) pc += joffset; break;
							case 6: if( sa.d >= sb.d ) pc += joffset; break;
							case 7: if( sa.d >  sb.d ) pc += joffset; break;
							case 8: if( sa.d <= sb.d ) pc += joffset; break;
							case 9: if( sa.d <  sb.d ) pc += joffset; break;
							} break;
						case StackType.Object:
							switch(iop)
							{
							case 0: if( sa.o == sb.o ) pc += joffset; break;
							case 5: if( sa.o != sb.o ) pc += joffset; break;
							default: throw new( "Invalid object comparison" );
							} break;
						default: 
							throw new( "Invalid comparison" );
						}
						break;
					}


					case 0x58: case 0x59: case 0x5A: case 0x5B: case 0x5C: case 0x5D:
					case 0x5E: case 0x5F: case 0x60: case 0x61: case 0x62: case 0x63:
					case 0x64:
					{
						StackElement sb = stack[--sp];
						StackElement sa = stack[--sp];
						StackType promoted = StackTypeMaxPromote( sa.type, sb.type );

						switch( b )
						{
							case 0x58: // Add
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadInt( sa.i + sb.i ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u + sb.u ); break;
									case StackType.Long:	stack[sp++].LoadLong( sa.l + sb.l ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e + sb.e ); break;
									case StackType.Float:	stack[sp++].LoadFloat( sa.f + sb.f ); break;
									case StackType.Double:	stack[sp++].LoadDouble( sa.d + sb.d ); break;
								} break;
							case 0x59: // Sub
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadInt( sa.i - sb.i ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u - sb.u ); break;
									case StackType.Long:	stack[sp++].LoadLong( sa.l - sb.l ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e - sb.e ); break;
									case StackType.Float:	stack[sp++].LoadFloat( sa.f - sb.f ); break;
									case StackType.Double:	stack[sp++].LoadDouble( sa.d - sb.d ); break;
								} break;
							case 0x5A: // Mul
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadInt( sa.i * sb.i ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u * sb.u ); break;
									case StackType.Long:	stack[sp++].LoadLong( sa.l * sb.l ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e * sb.e ); break;
									case StackType.Float:	stack[sp++].LoadFloat( sa.f * sb.f ); break;
									case StackType.Double:	stack[sp++].LoadDouble( sa.d * sb.d ); break;
								} break;
							case 0x5B: // Div
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadInt( sa.i / sb.i ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u / sb.u ); break;
									case StackType.Long:	stack[sp++].LoadLong( sa.l / sb.l ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e / sb.e ); break;
									case StackType.Float:	stack[sp++].LoadFloat( sa.f / sb.f ); break;
									case StackType.Double:	stack[sp++].LoadDouble( sa.d / sb.d ); break;
								} break;
							case 0x5C: // Div.un
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadUint( sa.u / sb.u ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u / sb.u ); break;
									case StackType.Long:	stack[sp++].LoadUlong( sa.e / sb.e ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e / sb.e ); break;
									default: Breakwarn( "Unexpected div.un instruction behavior", pc); break;
								} break;
							case 0x5D: // rem
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadInt( sa.i % sb.i ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u % sb.u ); break;
									case StackType.Long:	stack[sp++].LoadLong( sa.l % sb.l ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e % sb.e ); break;
									default: Breakwarn( "Unexpected rem instruction behavior", pc); break;
								} break;
							case 0x5E: // rem.un
								switch( promoted )
								{
									case StackType.Int:		stack[sp++].LoadUint( sa.u % sb.u ); break;
									case StackType.Uint:	stack[sp++].LoadUint( sa.u % sb.u ); break;
									case StackType.Long:	stack[sp++].LoadUlong( sa.e % sb.e ); break;
									case StackType.Ulong:	stack[sp++].LoadUlong( sa.e % sb.e ); break;
									default: Breakwarn( "Unexpected rem.un instruction behavior", pc); break;
								} break;
							case 0x5F: stack[sp++].LoadUlongType( sa.e & sb.e, promoted ); break; // and
							case 0x60: stack[sp++].LoadUlongType( sa.e | sb.e, promoted ); break; // or
							case 0x61: stack[sp++].LoadUlongType( sa.e ^ sb.e, promoted ); break; // xor
							case 0x62: stack[sp++].LoadUlongType( sa.e << sb.i, promoted ); break; // shl
							case 0x63: stack[sp++].LoadLongType( sa.l >> sb.i, promoted ); break; // shr
							case 0x64: stack[sp++].LoadUlongType( sa.e >> sb.i, promoted ); break; // shr.un
						}
						break;
					}

					case 0x65: stack[sp].l = -stack[sp].l; break;
					case 0x66: stack[sp].e ^= 0xffffffffffffffff; break;

					// TODO: All these conversions are sus.
					case 0x67: stack[sp-1].LoadByte( Convert.ToUInt32(stack[sp-1].AsObject())&0xff ); break; // conv.i1
					case 0x68: stack[sp-1].LoadUshort( Convert.ToUInt32(stack[sp-1].AsObject())&0xffff ); break; // conv.i2
					case 0x69: stack[sp-1].LoadInt( Convert.ToInt32(stack[sp-1].AsObject()) ); break; // conv.i4
					case 0x6A: stack[sp-1].LoadLong( Convert.ToInt64(stack[sp-1].AsObject()) ); break; // conv.i8
					case 0x6B: stack[sp-1].LoadFloat( Convert.ToSingle(stack[sp-1].AsObject()) ); break; // conv.r4
					case 0x6C: stack[sp-1].LoadDouble( Convert.ToDouble(stack[sp-1].AsObject()) ); break; // conv.r8

					case 0x72:
					{
						uint bc = BytecodeAsU32( ref pc );
						stack[sp++].Load( box.metadatas[bc].fields[0] );
						break; //ldstr
					}

					case 0x7b: 
					{
						uint bc = BytecodeAsU32( ref pc );
						object opths = stack[--sp].AsObject();
						if( opths is CilboxProxy )
							stack[sp++].Load( ((CilboxProxy)opths).fields[box.metadatas[bc].fieldIndex] );
						else
							throw new Exception( "Unimplemented.  Attempting to get field on non-cilbox object" );
						break; //ldfld
					}
					case 0x7d:
					{
						uint bc = BytecodeAsU32( ref pc );
						object o = stack[--sp].AsObject();
						object opths = stack[--sp].AsObject();
						if( opths is CilboxProxy )
							((CilboxProxy)opths).fields[box.metadatas[bc].fieldIndex] = o;
						else
							throw new Exception( "Unimplemented.  Attempting to set field on non-cilbox object" );
						break; //stfld
					}
					case 0x7e: 
					{
						uint bc = BytecodeAsU32( ref pc );
						stack[sp++].Load( parentClass.staticFields[box.metadatas[bc].fieldIndex] );
						break; //ldsfld
					}
					case 0x80:
					{
						uint bc = BytecodeAsU32( ref pc );
						parentClass.staticFields[box.metadatas[bc].fieldIndex] = stack[sp++].AsObject();
						break; //stsfld
					}
					case 0x8C: // box (This pulls off a type)
					{
						uint otyp = BytecodeAsU32( ref pc );
						stack[sp-1].LoadObject( stack[sp-1].AsObject() );//(metaType.nativeType)stack[sp-1].AsObject();
						break; 
					}
					case 0x8d:
					{
						uint otyp = BytecodeAsU32( ref pc );
						if( stack[sp-1].type > StackType.Ulong )
							throw new Exception( "Invalid type, processing new array" );
						int size = stack[sp-1].i;
						Type t = box.metadatas[otyp].nativeType;
						stack[sp-1].LoadObject( Array.CreateInstance( t, size ) );
						//newarr <etype>
						break;
					}
					case 0xa2:
					{
						object val = stack[--sp].AsObject();
						if( stack[sp-1].type > StackType.Uint ) throw new Exception( "Invalid index type" );
						int index = stack[--sp].i;
						object [] array = (object[])stack[--sp].AsObject();
						array[index] = val;
						break; // stelem.ref
					}
					case 0xa4:
					{
						uint otyp = BytecodeAsU32( ref pc );
						object val = stack[--sp].AsObject();
						if( stack[sp-1].type > StackType.Uint ) throw new Exception( "Invalid index type" );
						int index = stack[--sp].i;
						object [] array = (object[])stack[--sp].AsObject();
						Type t = box.metadatas[otyp].nativeType;
						array[index] = Convert.ChangeType( val, t );  // This shouldn't be type changing.s
						break; // stelem
					}
					case 0xA5:
					{
						uint otyp = BytecodeAsU32( ref pc ); // Let's hope that somehow this isn't needed?
						CilMetadataTokenInfo metaType = box.metadatas[otyp];
						if( metaType.nativeTypeIsStackType )
						{
							stack[sp-1].Unbox( stack[sp-1].AsObject(), metaType.nativeTypeStackType );
						}
						else
						{
							Breakwarn( "Scary Unbox (that we don't have code for) from " + otyp + " ORIG " + metaType.ToString(), pc );
							disabled = true; cont = false;
						}
						break; // unbox.any
					}
					default: Breakwarn( $"Opcode 0x{b.ToString("X2")} unimplemented", pc ); disabled = true; cont = false; break;
					}

					ctr++;
					box.stepsThisInvoke++;

					if( ( box.stepsThisInvoke & 0xf ) == 0 )
					{
						// Only check every 16.
						long elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - box.startTime);
						if( elapsed > Cilbox.timeoutLengthTicks )
						{
							throw new Exception( "Infinite Loop @ " + pc + " In " + methodName + " (Timeout ticks: " + elapsed + "/" + Cilbox.timeoutLengthTicks + " )" );
						}
					}
				}
				while( cont );
			}
			catch( Exception e )
			{
				disabled = true;
				if( box.nestingDepth == 1 )
					Breakwarn( e.ToString(), pc );
				else
				{
					box.nestingDepth--;
					throw;
				}
			}
			//if( box.nestingDepth == 1 ) Debug.Log( "This invoke took: " + box.stepsThisInvoke );
			box.nestingDepth--;
			return ( cont || sp == 0 ) ? null : stack[--sp].AsObject();
		}

		uint BytecodeAs16( ref int i )
		{
			return (uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, 2 );
		}
		uint BytecodeAsU32( ref int i )
		{
			return (uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, 4 );
		}
		int BytecodeAsI32( ref int i )
		{
			return (int)CilboxUtil.BytecodePullLiteral( byteCode, ref i, 4 );
		}
		ulong BytecodeAs64( ref int i )
		{
			return CilboxUtil.BytecodePullLiteral( byteCode, ref i, 8 );
		}
	}

	public class CilboxClass
	{
		public Cilbox box;
		public String className;

		public object[] staticFields;
		public String[] staticFieldNames;
		public Type[] staticFieldTypes;

		public String[] instanceFieldNames;
		public Type[] instanceFieldTypes;

		public Dictionary< String, uint > methodNameToIndex;
		public Dictionary< String, uint > methodFullSignatureToIndex;

		public CilboxMethod [] methods;

		public uint [] importFunctionToId; // from ImportFunctionID

		public CilboxClass( Cilbox box, String className, String classData )
		{
			this.box = box;
			this.className = className;
			OrderedDictionary classProps = CilboxUtil.DeserializeDict( classData );

			uint id = 0;
			OrderedDictionary staticFields = CilboxUtil.DeserializeDict( (String)classProps["staticFields"] );
			int sfnum = staticFields.Count;
			this.staticFields = new object[sfnum];
			staticFieldNames = new String[sfnum];
			staticFieldTypes = new Type[sfnum];
			foreach( DictionaryEntry k in staticFields )
			{
				String fieldName = staticFieldNames[id] = (String)k.Key;
				Type t = staticFieldTypes[id] = Type.GetType( (String)k.Value );

				//staticFieldIDs[id] = Cilbox.FindInternalMetadataID( className, 4, fieldName );
				this.staticFields[id] = CilboxUtil.DeserializeDataForProxyField( t, "" );
				id++;
			}

			OrderedDictionary instanceFields = CilboxUtil.DeserializeDict( (String)classProps["instanceFields"] );
			int ifnum = instanceFields.Count;
			instanceFieldNames = new String[ifnum];
			instanceFieldTypes = new Type[ifnum];
			//instanceFieldIDs = new uint[ifnum];
			id = 0;
			foreach( DictionaryEntry k in instanceFields )
			{
				String fieldName = instanceFieldNames[id] = (String)k.Key;
				instanceFieldTypes[id] = Type.GetType( (String)k.Value );
				//instanceFieldIDs[id] = Cilbox.FindInternalMetadataID( className, 4, fieldName );
				id++;
			}

			id = 0;
			OrderedDictionary deserMethods = CilboxUtil.DeserializeDict( (String)classProps["methods"] );
			int mnum = deserMethods.Count;
			methods = new CilboxMethod[mnum];
			methodNameToIndex = new Dictionary< String, uint >();
			methodFullSignatureToIndex = new Dictionary< String, uint >();
			foreach( DictionaryEntry k in deserMethods )
			{
				methods[id] = new CilboxMethod();
				methods[id].Load( this, (String)k.Key, (String)k.Value );
				methodNameToIndex[(String)k.Key] = id;
				methodFullSignatureToIndex[methods[id].fullSignature] = id;
				id++;
			}

			int numImportFunctions = Enum.GetNames(typeof(ImportFunctionID)).Length;
			importFunctionToId = new uint[numImportFunctions];
			for( int i = 0; i < numImportFunctions; i++ )
			{
				String fn = Enum.GetName(typeof(ImportFunctionID), i);
				if( i == 0 ) fn = ".ctor";
				uint idx = 0;
				importFunctionToId[i] = 0xffffffff;
				if( methodNameToIndex.TryGetValue(fn, out idx ) )
				{
					importFunctionToId[i] = idx;
				}
				//Debug.Log( "MATCHING + " + fn + " : " + i + " " + importFunctionToId[i] );
			}
		}
	}

	public class CilMetadataTokenInfo
	{
		public CilMetadataTokenInfo( MetaTokenType type, String [] fields ) { this.type = type; this.fields = fields; }
		public MetaTokenType type;
		public bool isValid;
		public int fieldIndex; // Only used for fields.

		public Type nativeType; // Used for types.
		public bool nativeTypeIsStackType;
		public StackType nativeTypeStackType;

		// Todo handle interpreted types.
		public bool isNative;
		public MethodBase nativeMethod;
		public int interpretiveMethod; // If nativeToken is 0, then it's a interpreted call.
		public int interpretiveMethodClass; // If nativeToken is 0, then it's a interpreted call class

		// For string, type = 7, string is in fields[0]
		// For methods, type = 10, Declaring Type is in fields[0], Method is in fields[1], Full name is in fields[2] assembly name is in fields[3]
		// For fields, type = 4, Declaring Type is in fields[0], Name is in fields[1], Type is in fields[2]
		public String [] fields;

		public String Name;
		//public String ToString() { return Name; }
	}

	public enum MetaTokenType
	{
		mtType = 1,
		mtField = 4,
		mtString = 7,
		mtMethod = 10,
	}

	public class Cilbox : MonoBehaviour
	{
		public Dictionary< String, int > classes;
		public CilboxClass [] classesList;
		public CilMetadataTokenInfo [] metadatas;
		public String assemblyData;
		private bool initialized = false;

		public int stepsThisInvoke;
		public int nestingDepth;

		public long startTime;
		public static readonly long timeoutLengthTicks = 50000000; // 5000ms

		Cilbox()
		{
			initialized = false;
		}

		public void ForceReinit()
		{
			initialized = false;
		}

		public void BoxInitialize()
		{
			Debug.Log( $"Cilbox Initialize called {initialized}" );
			if( initialized ) return;
			initialized = true;
			Debug.Log( "Cilbox Initialize" );
			Debug.Log( "Metadata:" + assemblyData );

			OrderedDictionary assemblyRoot = CilboxUtil.DeserializeDict( assemblyData );
			OrderedDictionary classData = CilboxUtil.DeserializeDict( (String)assemblyRoot["classes"] );
			OrderedDictionary metaData = CilboxUtil.DeserializeDict( (String)assemblyRoot["metadata"] );

			metadatas = new CilMetadataTokenInfo[metaData.Count+1]; // element 0 is invalid.
			metadatas[0] = new CilMetadataTokenInfo( 0, new String[]{ "INVALID METADATA" } );

			int clsid = 0;
			classes = new Dictionary< String, int >();
			classesList = new CilboxClass[classData.Count];
			foreach( DictionaryEntry v in classData )
			{
				CilboxClass cls = new CilboxClass( this, (String)v.Key, (String)v.Value );
				classesList[clsid] = cls;
				classes[(String)v.Key] = clsid;
				clsid++;
			}

			foreach( DictionaryEntry v in metaData )
			{
				int mid = Convert.ToInt32((String)v.Key);
				String [] st = CilboxUtil.DeserializeArray( (String)v.Value );

				//Debug.Log( $"ST {(String)v.Value} => {st.Length} from {(String)v.Key}" );
				if( st.Length < 2 )
				{
					Debug.LogWarning( "Metadata read error. Could not interpret " + (String)v.Value );
					continue;
				}
				String [] fields = new String[st.Length-1];
				Array.Copy( st, 1, fields, 0, st.Length-1 );
				MetaTokenType metatype = (MetaTokenType)Convert.ToInt32(st[0]);
				CilMetadataTokenInfo t = metadatas[mid] = new CilMetadataTokenInfo( metatype, fields );

				t.type = metatype;
				t.Name = "<UNKNOWN>";

				if( metatype == MetaTokenType.mtField && st.Length > 4 )
				{
					// The type has been "sealed" so-to-speak. In that we have an index for it.
					t.fieldIndex = Convert.ToInt32(st[4]);
					t.Name = "Field: " + st[4];
					t.isValid = true;
				}
				if( metatype == MetaTokenType.mtType )
				{
					String hostTypeName = st[1];
					String useAssembly = st[2];
					StackType nst;
					if( CilboxUtil.TypeToStackType.TryGetValue( hostTypeName, out nst ) )
					{
						t.nativeTypeIsStackType = true;
						t.nativeTypeStackType = nst;
					}
					else
					{
						t.nativeType = CilboxUtil.GetNativeTypeFromName( useAssembly, hostTypeName );
						t.isValid = t.nativeType != null;

						if( !t.isValid )
						{
							Debug.LogError( $"Error: Could not find type: {st[1]}" );
						}
						else
						{
							t.Name = "Type: " + hostTypeName;
						}
					}
				}
				if( metatype == MetaTokenType.mtMethod )
				{
					OrderedDictionary methodProps = CilboxUtil.DeserializeDict( st[1] );

					// Function call
					// TODO: Apply security rules here.
					// (Or a please explode, for instance if you violate security rules)
					String declaringTypeName = (String)methodProps["declaringType"];
					String [] parameterNames = CilboxUtil.DeserializeArray((String)methodProps["parameters"]);
					String name = (String)methodProps["name"];
					String fullSignature = (String)methodProps["fullSignature"];
					String useAssembly = (String)methodProps["assembly"];
					String [] genericArguments = CilboxUtil.DeserializeArray( (String)methodProps["genericArguments"] );
					t.Name = "Method: " + name;

					//genericArguments Possibly generate based on this.

					// First, see if this is to a class we are responsible for.
					int classid;
					if( classes.TryGetValue( declaringTypeName, out classid ) )
					{
						CilboxClass matchingClass = classesList[classid];
						uint imid = 0;
						if( matchingClass.methodFullSignatureToIndex.TryGetValue( fullSignature, out imid ) )
						{
							//t.nativeToken = 0; // Sentinel for saying it's a cilbox'd class.
							t.isNative = false;
							t.interpretiveMethod = (int)imid;
							t.interpretiveMethodClass = classid;
							t.isValid = true;
						}
						else
						{
							t.isValid = false;
							throw new Exception( $"Error: Could not find internal method {declaringTypeName}:{fullSignature}" );
						}
					}
					else
					{
						Type declaringType = CilboxUtil.GetNativeTypeFromName( useAssembly, declaringTypeName );
						if( declaringType == null )
						{
							throw new Exception( $"Error: Could not find referenced type {useAssembly} {declaringTypeName}" );
						}

						Type [] parameters = CilboxUtil.TypeNamesToArrayOfNativeTypes( parameterNames );

						// XXX Can we combine these?
						MethodBase m = declaringType.GetMethod(
							name,
							genericArguments.Length,
							BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
							null,
							CallingConventions.Any,
							parameters,
							null ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."

						if( m == null )
						{
							// Can't use GetConstructor, because somethings have .ctor or .cctor
							ConstructorInfo[] cts = declaringType.GetConstructors(
								BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
							int ck;
							for( ck = 0; ck < cts.Length; ck++ )
							{
								//Debug.Log( cts[ck] );
								if( fullSignature == cts[ck].ToString() )
								{
									m = cts[ck];
									break;
								}
							}
						}

						if( m != null && m is MethodInfo && genericArguments.Length > 0 )
						{
					    	m = ((MethodInfo)m).MakeGenericMethod( CilboxUtil.TypeNamesToArrayOfNativeTypes( genericArguments ) );
						}

						if( m != null )
						{
							t.nativeMethod = m;
							t.isNative = true;
							t.isValid = true;
						} else if( !t.isNative )
						{
							throw new Exception( "Error: Could not find reference to: " + useAssembly + "." + fullSignature );
						}
					}
				}
			}
		}

		public CilboxClass GetClass( String className )
		{
			if( className == null ) return null;
			int clsid;
			if( classes.TryGetValue(className, out clsid)) return classesList[clsid];
			return null;
		}

		public object InterpretIID( CilboxClass cls, CilboxProxy ths, ImportFunctionID iid, object [] parameters )
		{
			if( cls == null ) return null;
			uint index = cls.importFunctionToId[(uint)iid];
			if( index == 0xffffffff ) return null;
			return cls.methods[index].Interpret( ths, parameters );
		}
	}



	///////////////////////////////////////////////////////////////////////////
	//  EXPORTING  ////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////

	#if UNITY_EDITOR

	// Trigger the scene recompile.  Uuuughhhh someone who knows what they're doing need to rewrite
	// this part.  Also, see this discussion: https://discussions.unity.com/t/onprocessscene-sometimes-gets-skipped/943573/7
	//
	// IProcessSceneWithReport - runs before scene is compiled, against the play-mode tree
	// OnPostBuildPlayerScriptDLLs - it runs at the right time, in a blank scene, but that scene is not what is used.
	// IPostprocessBuildWithReport - happens after build is complete, but also dumped into a temporary scene.
	// IPreprocessBuildWithReport - Happens on the main scene, and outputs are preserved
	// BuildPlayerProcessor - same as IPreprocessBuildWithReport

	class CilboxCustomBuildProcessor : IProcessSceneWithReport
	{
		public int callbackOrder { get { return 0; } }
		public void OnProcessScene( UnityEngine.SceneManagement.Scene scene, UnityEditor.Build.Reporting.BuildReport report)
		{
			Debug.Log( "IProcessSceneWithReport" );
			CilboxScenePostprocessor.OnPostprocessScene();
		}
	}

	class CilboxCustomBuildProcessor2 : IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }
		public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
		{
/*
			This does not work >:(

			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene() );

			object[] objToCheck = GameObject.FindObjectsByType<GameObject>( FindObjectsSortMode.None );
			foreach (object o in objToCheck)
			{
				GameObject g = (GameObject) o;
				EditorUtility.SetDirty( g );
			}
			UnityEditor.SceneManagement.EditorSceneManager.SaveScene( UnityEngine.SceneManagement.SceneManager.GetActiveScene() );
			AssetDatabase.ImportAsset(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, ImportAssetOptions.ForceUpdate);
*/

			MonoBehaviour [] allBehavioursThatNeedCilboxing = CilboxUtil.GetAllBehavioursThatNeedCilboxing();

			if( allBehavioursThatNeedCilboxing.Length == 0 )
				return;

			Debug.Log( $"Dirtying scene, found {allBehavioursThatNeedCilboxing.Length} cilboxable elements." );

			// PLEASE LET ME KNOW IF YOU KNOW A BETTER WAY https://discussions.unity.com/t/onprocessscene-sometimes-gets-skipped/943573/6
			GameObject dirtier = GameObject.Find( "/CilboxDirtier" );
			if( !dirtier )
				dirtier = new GameObject("CilboxDirtier");
			dirtier.hideFlags = HideFlags.HideInHierarchy;
			dirtier.transform.position = new Vector3(UnityEngine.Random.Range(-100,100),UnityEngine.Random.Range(-100,100),UnityEngine.Random.Range(-100,100));
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene() );
			UnityEditor.SceneManagement.EditorSceneManager.SaveScene( UnityEngine.SceneManagement.SceneManager.GetActiveScene() );
		}
	}
	public class CilboxScenePostprocessor {
		//[PostProcessSceneAttribute (2)] This is actually called by IProcessSceneWithReport
		public static void OnPostprocessScene() {

			MonoBehaviour [] allBehavioursThatNeedCilboxing = CilboxUtil.GetAllBehavioursThatNeedCilboxing();

			Debug.Log( $"Postprocessing scene. Cilbox scripts to do: {allBehavioursThatNeedCilboxing.Length}" );
			if( allBehavioursThatNeedCilboxing.Length == 0 ) return;

			Assembly proxyAssembly = typeof(CilboxProxy).Assembly;

			OrderedDictionary assemblyMetadata = new OrderedDictionary();
			Dictionary< uint, String > originalMetaToFriendlyName = new Dictionary< uint, String >();
			Dictionary< int, uint> assemblyMetadataReverseOriginal = new Dictionary< int, uint >();

			uint mdcount = 1; // token 0 is invalid.
			int bytecodeLength = 0;
			OrderedDictionary classes = new OrderedDictionary();
			Dictionary< String, OrderedDictionary > allClassMethods = new Dictionary< String, OrderedDictionary>();

			StreamWriter CLog = File.CreateText( Application.dataPath + "/CilboxLog.txt" );

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				OrderedDictionary methods = new OrderedDictionary();

				int mtyp; // Which round of methods are we getting.

				// Iterate twice. Once for methods, then for constructors.
				for( mtyp = 0; mtyp < 2; mtyp++ )
				{
					MethodBase[] me;
					if( mtyp == 0 )
						me = type.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
					else
						me = type.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );

					foreach( MethodBase m in me )
					{
						if( m.DeclaringType.Assembly != proxyAssembly )
						{
							// We can't export things that are part of Unity.
							continue;
						}

						String methodName = m.Name;
						OrderedDictionary MethodProps = new OrderedDictionary();
						//CLog.WriteLine( type + " / " + m.Name );
						MethodBody mb = m.GetMethodBody();
						if( mb == null )
						{
							Debug.Log( $"NOTE: {m.Name} does not have a body" );
							// Things like MemberwiseClone, etc.
							continue;
						}

						byte [] byteCodeIn = mb.GetILAsByteArray();
						byte [] byteCode = new byte[byteCodeIn.Length];
						Array.Copy( byteCodeIn, byteCode, byteCodeIn.Length );

						String asm = "";
						//if( !ExtractAndTransformMetas( proxyAssembly, ref ba, ref assemblyMetadata, ref assemblyMetadataReverseOriginal, ref mdcount ) ) continue;
						//static bool ExtractAndTransformMetas( Assembly proxyAssembly, ref byte [] byteCode, ref OrderedDictionary od, ref Dictionary< uint, uint > assemblyMetadataReverseOriginal, ref int mdcount )
						{
							int i = 0;
							i = 0;
							try {
								do
								{
									int starti = i;
									CilboxUtil.OpCodes.OpCode oc = CilboxUtil.OpCodes.ReadOpCode( byteCode, ref i );
									int opLen = CilboxUtil.OpCodes.OperandLength[(int)oc.OperandType];
									int backupi = i;
									uint operand = (uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, opLen );
									bool changeOperand = true;
									uint writebackToken = mdcount;

									asm += "\t" + String.Format("{0,-5}{1,-10}", starti, oc.ToString() );
									if( opLen > 0 ) asm += "\t0x" + operand.ToString("X"+opLen*2);

									// Check to see if this is a meta that we care about.  Then rewrite in a new identifier.
									// ResolveField, ResolveMember, ResolveMethod, ResolveSignature, ResolveString, ResolveType
									// We sort of want to let the other end know what they are. So we mark them with the code
									// from here: https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Metadata/TableHeap.cs#L16

									if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineString )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											String inlineString = ((int)MetaTokenType.mtString) + "\t" + proxyAssembly.ManifestModule.ResolveString( (int)operand );
											originalMetaToFriendlyName[mdcount] = MetaTokenType.mtString.ToString();
											assemblyMetadata[(mdcount++).ToString()] = inlineString;
										}
										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineMethod )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											MethodBase tmb = proxyAssembly.ManifestModule.ResolveMethod( (int)operand );

											OrderedDictionary methodProps = new OrderedDictionary();

											// "Generic constructors are not supported in the .NET Framework version 2.0"
											if( !tmb.IsConstructor )
											{
												Type[] templateArguments = tmb.GetGenericArguments();
												if( templateArguments.Length > 0 )
												{
													String [] argtypes = new String[templateArguments.Length];
													for( int a = 0; a < templateArguments.Length; a++ )
														argtypes[a] = CilboxUtil.SerializeArray( new String[]{
															templateArguments[a].Assembly.GetName().Name, templateArguments[a].FullName } );
													methodProps["genericArguments"] = CilboxUtil.SerializeArray( argtypes );
												}
											}

											methodProps["declaringType"] = tmb.DeclaringType.ToString();
											methodProps["name"] = tmb.Name;

											System.Reflection.ParameterInfo[] parameterInfos = tmb.GetParameters();
											if( parameterInfos.Length > 0 )
											{
												String [] sParameters = new String[parameterInfos.Length];
												for( var j = 0; j < parameterInfos.Length; j++ )
												{
													Type ty = parameterInfos[j].ParameterType;
													sParameters[j] = CilboxUtil.SerializeArray( new String[]{
														ty.Assembly.GetName().Name, ty.FullName });
												}
												methodProps["parameters"] = CilboxUtil.SerializeArray( sParameters );
											}
											methodProps["fullSignature"] = tmb.ToString();
											methodProps["assembly"] = tmb.DeclaringType.Assembly.GetName().Name;

											originalMetaToFriendlyName[mdcount] = tmb.DeclaringType.ToString() + "." + tmb.ToString();
											assemblyMetadata[(mdcount++).ToString()] = CilboxUtil.SerializeArray( new String[]{
												((int)MetaTokenType.mtMethod).ToString(), CilboxUtil.SerializeDict( methodProps ) } );
										}

										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineField )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											FieldInfo rf = proxyAssembly.ManifestModule.ResolveField( (int)operand );
											String fieldInfo = ((int)MetaTokenType.mtField) + "\t" + rf.DeclaringType + "\t" + rf.Name + "\t" + rf.FieldType;
											originalMetaToFriendlyName[mdcount] = rf.Name;
											assemblyMetadata[(mdcount++).ToString()] = fieldInfo;
										}
										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( oc.OperandType == CilboxUtil.OpCodes.OperandType.InlineType )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											Type ty = proxyAssembly.ManifestModule.ResolveType( (int)operand );
											String typeInfo = ((int)MetaTokenType.mtType) + "\t" + ty.FullName + "\t" + ty.Assembly.GetName().Name;
											originalMetaToFriendlyName[mdcount] = ty.FullName;
											assemblyMetadata[(mdcount++).ToString()] = typeInfo;
										}
										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else
										changeOperand = false;

									asm += "\n";

									if( changeOperand )
									{
										i = backupi;
										assemblyMetadataReverseOriginal[(int)operand] = writebackToken;
										CilboxUtil.BytecodeReplaceLiteral( ref byteCode, ref i, opLen, writebackToken );
									}
									if( i >= byteCode.Length ) break;
								} while( true );
							}
							catch( Exception e )
							{
								Debug.LogError( e );
								continue;
							}
						}

						String byteCodeStr = "";
						for( int i = 0; i < byteCode.Length; i++ )
							byteCodeStr += byteCode[i].ToString( "x2" );

						CLog.WriteLine( type.FullName + "." + methodName + " (" + byteCode.Length + ")\n" + asm );

						bytecodeLength += byteCode.Length;
						MethodProps["body"] = byteCodeStr;

						OrderedDictionary localVars = new OrderedDictionary();
						foreach (LocalVariableInfo lvi in mb.LocalVariables)
							localVars[lvi.ToString()] = lvi.GetType().FullName;
						MethodProps["locals"] = CilboxUtil.SerializeDict( localVars );

						ParameterInfo [] parameters = m.GetParameters();

						OrderedDictionary argVars = new OrderedDictionary();
						foreach (ParameterInfo p in parameters)
						{
							argVars[p.Name] = p.ParameterType.ToString();
						}
						MethodProps["parameters"] = CilboxUtil.SerializeDict( argVars );
						MethodProps["maxStack"] = mb.MaxStackSize.ToString();
						MethodProps["isStatic"] = m.IsStatic ? "1" : "0";
						MethodProps["fullSignature"] = m.ToString();

						methods[methodName] = CilboxUtil.SerializeDict( MethodProps );
					}
				}

				allClassMethods[type.FullName] = methods;
			}

			// Now that we've iterated through all classes, and collected all possible uses of field IDs,
			// go through the classes again, collecting the fields themselves.

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				OrderedDictionary staticFields = new OrderedDictionary();
				int sfid = 0;
				FieldInfo[] fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static );
				foreach( var f in fi )
				{
					staticFields[f.Name] = f.FieldType.FullName;

					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
					{
						assemblyMetadata[mdid.ToString()] += "\t" + sfid;
					}
					sfid++;
				}

				OrderedDictionary instanceFields = new OrderedDictionary();
				fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				int ifid = 0;
				foreach( var f in fi )
				{
					instanceFields[f.Name] = f.FieldType.FullName;
					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
						assemblyMetadata[mdid.ToString()] += "\t" + ifid;
					ifid++;
				}

				OrderedDictionary classProps = new OrderedDictionary();
				classProps["methods"] = CilboxUtil.SerializeDict( allClassMethods[type.FullName] );
				classProps["staticFields"] = CilboxUtil.SerializeDict( staticFields );
				classProps["instanceFields"] = CilboxUtil.SerializeDict( instanceFields );
				classes[type.FullName] = CilboxUtil.SerializeDict( classProps );
			}

			OrderedDictionary assemblyRoot = new OrderedDictionary();
			assemblyRoot["classes"] = CilboxUtil.SerializeDict( classes );
			assemblyRoot["metadata"] = CilboxUtil.SerializeDict( assemblyMetadata );

			String sAllAssemblyData = CilboxUtil.SerializeDict( assemblyRoot );

			Cilbox [] se = Resources.FindObjectsOfTypeAll(typeof(Cilbox)) as Cilbox [];

			Cilbox tac;
			if( se.Length != 0 )
			{
				tac = se[0];
				if( tac.assemblyData != sAllAssemblyData ) EditorUtility.SetDirty( tac );
			}
			else
			{
				GameObject cilboxDataObject = new GameObject("CilboxData " + new System.Random().Next(0,10000000));
				//cilboxDataObject.hideFlags = HideFlags.HideInHierarchy;
				tac = cilboxDataObject.AddComponent( typeof(Cilbox) ) as Cilbox;
				EditorUtility.SetDirty( tac );
			}

			if( bytecodeLength == 0 )
			{
				Debug.Log( "No bytecode available in this build. Falling back to last build." );
			}
			else
			{
				tac.assemblyData = sAllAssemblyData;
				tac.ForceReinit();
				Debug.Log( "Outputting Assembly Data: " + sAllAssemblyData + " byteCode: " + bytecodeLength + " bytes " );

				String wordWrapped = "";
				var m = 0;
				foreach( var c in sAllAssemblyData )
				{
					wordWrapped += c;
					if( ++m % 76 == 0 ) wordWrapped += "\n\t";
				}
				CLog.WriteLine( "Outputting Assembly Data:\n" + wordWrapped + "\nbyteCode: " + bytecodeLength + " bytes " );
			}

			Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap = new Dictionary< MonoBehaviour, CilboxProxy >();
			List< MonoBehaviour > refProxiesOrig = new List< MonoBehaviour >();
			List< CilboxProxy > refProxies = new List< CilboxProxy >();

			// Iterate over all GameObjects, and find the ones that have Cilboxable scripts.
			object[] obj = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			foreach (object o in obj)
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

					CilboxProxy p = g.AddComponent<CilboxProxy>();
					refProxies.Add( p );
					refProxiesOrig.Add( m );
					refToProxyMap[m] = p;
				}
			}

			var cnt = refProxies.Count;
			for( var i = 0; i < cnt; i++ )
			{
				CilboxProxy p = refProxies[i];
				MonoBehaviour m = refProxiesOrig[i];

				p.SetupProxy( tac, m, refToProxyMap );
			}

			// re-attach the refrences to 
			foreach (MonoBehaviour m in allBehavioursThatNeedCilboxing)
			{
				UnityEngine.Object.DestroyImmediate( m );
			}
		}
	}
	#endif

	public enum ImportFunctionID
	{
		dotCtor, // Must be at index 0.
		Update,
		Start,
		Awake,
	}
}

