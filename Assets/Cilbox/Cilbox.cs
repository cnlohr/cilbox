using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

#if UNITY_EDITOR
using Unity.Profiling;
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

		static public StackElement nil;

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
			{ "System.Double", StackType.Double },
			{ "object", StackType.Object } };

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
			case bool ta0: i = ((bool)o) ? 1 : 0; type = StackType.Boolean; break;
			default: this.o = o; type = StackType.Object; break;
			}
			return this;
		}

		static public StackElement LoadAsStatic( object o )
		{
			StackElement ret = new StackElement();
			ret.i = 0; ret.o = null;
			switch( o )
			{
			case sbyte t0: ret.i = (sbyte)o;	ret.type = StackType.Sbyte; break;
			case byte  t1: ret.i = (byte)o;		ret.type = StackType.Byte; break;
			case short t2: ret.i = (short)o;	ret.type = StackType.Short; break;
			case ushort t3: ret.i = (ushort)o;	ret.type = StackType.Ushort; break;
			case int t4: ret.i = (int)o;		ret.type = StackType.Int; break;
			case uint t5: ret.u = (uint)o;		ret.type = StackType.Uint; break;
			case long t6: ret.l = (long)o;		ret.type = StackType.Long; break;
			case ulong t7: ret.e = (ulong)o;	ret.type = StackType.Ulong; break;
			case float t8: ret.f = (float)o;	ret.type = StackType.Float; break;
			case double t9: ret.d = (ulong)o;	ret.type = StackType.Double; break;
			case bool ta0: ret.i = ((bool)o) ? 1 : 0; ret.type = StackType.Boolean; break;
			default: ret.o = o; ret.type = StackType.Object; break;
			}
			return ret;
		}

		public StackElement LoadObject( object o ) { this.o = o; type = StackType.Object; return this; }
		public StackElement LoadSByte( sbyte s ) { this.i = (int)s; type = StackType.Sbyte; return this; }
		public StackElement LoadByte( uint u ) { this.u = u; type = StackType.Byte; return this; }
		public StackElement LoadShort( short s ) { this.i = (int)s; type = StackType.Short; return this; }
		public StackElement LoadUshort( ushort u ) { this.u = u; type = StackType.Ushort; return this; }
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
			case StackType.Address: return Dereference();
			default: return o;
			}
		}

		public int AsInt()
		{
			switch( type )
			{
			case StackType.Sbyte:
			case StackType.Byte:
			case StackType.Short:
			case StackType.Ushort:
			case StackType.Int:
			case StackType.Uint:
			case StackType.Long:
			case StackType.Ulong:
				return (int)i;
			case StackType.Float: return (int)f;
			case StackType.Double: return (int)d;
			case StackType.Boolean: return b ? 1 : 0;
			case StackType.Address: return (int)Dereference();
			default: return (int)o;
			}
		}

		public object CoerceToObject( Type t )
		{
			StackType rt = StackTypeFromType( t );
			if( type < StackType.Float ) 
			{
				switch( rt )
				{
				case StackType.Sbyte:   return (sbyte)i;
				case StackType.Byte:    return (byte)u;
				case StackType.Short:   return (short)i;
				case StackType.Ushort:  return (ushort)u;
				case StackType.Int:     return (int)i;
				case StackType.Uint:    return (uint)u;
				case StackType.Long:    return (long)l;
				case StackType.Ulong:   return (ulong)e;
				case StackType.Float:   return (float)e;
				case StackType.Double:  return (double)e;
				case StackType.Boolean: return e != 0;
				default:
					switch( type )
					{
						case StackType.Sbyte: return Convert.ChangeType( (sbyte)i, t );
						case StackType.Byte:  return Convert.ChangeType( (byte)u, t );
						case StackType.Short: return Convert.ChangeType( (short)i, t );
						case StackType.Ushort:return Convert.ChangeType( (ushort)u, t );
						case StackType.Int:   return Convert.ChangeType( (int)i, t );
						case StackType.Uint:  return Convert.ChangeType( (uint)u, t );
						case StackType.Long:  return Convert.ChangeType( (long)e, t );
						case StackType.Ulong: return Convert.ChangeType( (ulong)u, t );
					}
					break;
				}
			}
			else if( type < StackType.Double ) // Float
			{
				switch( rt )
				{
				case StackType.Sbyte:  return (sbyte)f;
				case StackType.Byte:   return (byte)f;
				case StackType.Short:  return (short)f;
				case StackType.Ushort: return (ushort)f;
				case StackType.Int:    return (int)f;
				case StackType.Uint:   return (uint)f;
				case StackType.Long:   return (long)f;
				case StackType.Ulong:  return (ulong)f;
				case StackType.Float:  return (float)f;
				case StackType.Double: return (double)f;
				case StackType.Boolean:  return f != 0.0f;
				default:   return Convert.ChangeType( o, t );
				}
			}
			else if( type < StackType.Object ) // Double
			{
				switch( rt )
				{
				case StackType.Sbyte:   return (sbyte)d;
				case StackType.Byte:    return (byte)d;
				case StackType.Short:   return (short)d;
				case StackType.Ushort:  return (ushort)d;
				case StackType.Int:     return (int)d;
				case StackType.Uint:    return (uint)d;
				case StackType.Long:    return (long)d;
				case StackType.Ulong:   return (ulong)d;
				case StackType.Float:   return (float)d;
				case StackType.Double:  return (double)d;
				case StackType.Boolean: return d != 0;
				default:        return Convert.ChangeType( o, t );
				}
			}
			else if( type == StackType.Object )
			{
				return Convert.ChangeType( o, t );
			}

			throw new Exception( "Erorr invalid type conversion from " + type + " to " + t );
		}

		public object Dereference()
		{
			if( o.GetType() == typeof(StackElement[]) )
				return ((StackElement[])o)[i].AsObject();
			else
				return ((Array)o).GetValue(i);
		}

		// Mostly like a Dereference.
		static public StackElement ResolveToStackElement( StackElement tr )
		{
			if( tr.type == StackType.Address )
			{
				if( tr.o.GetType() == typeof(StackElement[]) )
					return ResolveToStackElement( ((StackElement[])tr.o)[tr.i] );
				else
					return ResolveToStackElement( StackElement.LoadAsStatic(((Array)tr.o).GetValue(tr.i)) );
			}
			else
			{
				return tr;
			}
		}

		// XXX RISKY - generally copy this in-place.
		public void DereferenceLoad( object overwrite )
		{
			if( o.GetType() == typeof(StackElement[]) )
				((StackElement[])o)[i].Load( overwrite );
			else
				((Array)o).SetValue(overwrite, i);
		}

		static public StackElement CreateReference( Array array, uint index )
		{
			StackElement ret = new StackElement();
			ret.type = StackType.Address;
			ret.u = index;
			ret.o = array;
			return ret;
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
			typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof( int ),
			typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(object),
			typeof(void) /*Tricky, pointer*/ };

		public static StackType StackTypeFromType( Type t )
		{
			switch( t )
			{
				case Type _ when t == typeof(sbyte): return StackType.Sbyte;
				case Type _ when t == typeof(byte): return StackType.Byte;
				case Type _ when t == typeof(short): return StackType.Short;
				case Type _ when t == typeof(ushort): return StackType.Ushort;
				case Type _ when t == typeof(int): return StackType.Int;
				case Type _ when t == typeof(uint): return StackType.Uint;
				case Type _ when t == typeof(long): return StackType.Long;
				case Type _ when t == typeof(ulong): return StackType.Ulong;
				case Type _ when t == typeof(float): return StackType.Float;
				case Type _ when t == typeof(double): return StackType.Double;
				case Type _ when t == typeof(bool): return StackType.Boolean;
				default: return StackType.Object;
			}
		}

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
		public bool isVoid;
		public String[] signatureParameters;
		public String[] signatureParameterTypes;
#if UNITY_EDITOR
		ProfilerMarker perfMarkerInterpret;
#endif

		public void Load( CilboxClass cclass, String name, Serializee payload )
		{
			methodName = name;
			parentClass = cclass;
			Dictionary<String, Serializee> methodProps = payload.AsMap();

			Serializee [] vl = methodProps["locals"].AsArray();
			methodLocals = new String[vl.Length];
			methodLocalTypes = new String[vl.Length];
			int iid = 0;
			for( int i = 0; i < vl.Length; i++ )
			{
				Dictionary< String, String > local = vl[i].AsStringMap();
				methodLocals[iid] = local["name"];
				methodLocalTypes[iid] = local["type"];
			}

			var pl = methodProps["body"].AsString();
			int bl = pl.Length/2;
			byteCode = new byte[bl];
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

			MaxStackSize = Convert.ToInt32((methodProps["maxStack"].AsString()));
			isVoid = Convert.ToInt32((methodProps["isVoid"].AsString())) != 0;
			isStatic = Convert.ToInt32((methodProps["isStatic"].AsString())) != 0;
			fullSignature = methodProps["fullSignature"].AsString();

			Serializee [] od = methodProps["parameters"].AsArray();
			signatureParameters = new String[od.Length];
			signatureParameterTypes = new String[od.Length];
			int sn = 0;
			for( int p = 0; p < od.Length; p ++ )
			{
				Dictionary< String, Serializee > thisp = od[p].AsMap();
				signatureParameters[sn] = thisp["name"].AsString();
				signatureParameterTypes[sn] = thisp["type"].AsString();
				sn++;
			}
#if UNITY_EDITOR
			perfMarkerInterpret = new ProfilerMarker(parentClass.className + ":" + fullSignature);
#endif

		}

		public void Breakwarn( String message, int bytecodeplace )
		{
			Debug.LogError( $"Breakwarn: {message} Class: {parentClass.className}, Function: {methodName}, Bytecode: {bytecodeplace}" );
		}

		public object Interpret( CilboxProxy ths, object [] parametersIn )
		{
			//StackElement [] parameters, int parameterPlace, int stackHead
			StackElement [] stackBuffer = new StackElement[Cilbox.defaultStackSize];

			int plen = 0;
			if( parametersIn != null )
			{
				plen = parametersIn.Length;
			}

			if( isStatic )
			{
				for( int p = 0; p < plen; p++ )
					stackBuffer[p].Load( parametersIn[p] );
			}
			else
			{
				stackBuffer[0].Load( ths );
				for( int p = 0; p < plen; p++ )
					stackBuffer[p+1].Load( parametersIn[p] );
				plen++;
			}

			return InterpretInner( stackBuffer, 0, plen ).AsObject();
		}


		public StackElement InterpretInner( StackElement [] stackBuffer, int parameterStart, int stackHead )
		{
			if( byteCode == null || byteCode.Length == 0 || disabled )
			{
				return StackElement.nil;
			}

#if UNITY_EDITOR
			perfMarkerInterpret.Begin();
#endif

			Cilbox box = parentClass.box;
	
			if( box.nestingDepth == 0 )
			{
				box.stepsThisInvoke = 0;
				box.startTime = System.Diagnostics.Stopwatch.GetTimestamp();
			}
			box.nestingDepth++;

			int localVarsHead = stackHead + MaxStackSize;
			int stackContinues = localVarsHead + methodLocals.Length;

			// Add a little buffer for possible function calls.
			if( stackContinues + 16 >= stackBuffer.Length ) throw new Exception( "Error: Interpreted stack overflow in " + methodName );

//			bool bDeepDebug = false;
			// Uncomment for debugging.
/*
			if( fullSignature.Contains( "Start" ) )
			{
				bDeepDebug = true;
				String parmSt = ""; for( int sk = parameterStart; sk < stackHead; sk++ ) {
					parmSt += "/"; parmSt += stackBuffer[sk].AsObject() + "+" + stackBuffer[sk].type;
				}
				Debug.Log( "***** FUNCTION ENTRY " + parentClass.className + " " + methodName + " " + parameterStart + "-" + stackHead + " PARM:" + parmSt);
				bDeepDebug = true;
			}
*/

			int sp = stackHead-1;
			bool cont = true;
			int ctr = 0;
			int pc = 0;
			try
			{
				do
				{
					byte b = byteCode[pc];

//					// Uncomment for debugging.
//					if( bDeepDebug )
//					{
//						String stackSt = ""; for( int sk = stackHead; sk < localVarsHead; sk++ ) { stackSt += "/"; if( sk == sp ) stackSt += ">"; stackSt += stackBuffer[sk].AsObject() + "+" + stackBuffer[sk].type; if( sk == sp ) stackSt += "<"; }
//						int icopy = pc; CilboxUtil.OpCodes.OpCode opc = CilboxUtil.OpCodes.ReadOpCode ( byteCode, ref icopy );
//						Debug.Log( "Bytecode " + opc + " (" + b.ToString("X2") + ") @ " + pc + "/" + byteCode.Length + " " + stackSt);
//					}
// For itty bitty profiling.
//int xicopy = pc; CilboxUtil.OpCodes.OpCode opcx = CilboxUtil.OpCodes.ReadOpCode ( byteCode, ref xicopy );
//ProfilerMarker pfm = new ProfilerMarker(opcx.ToString());
//pfm.Begin();

					pc++;
					switch( b )
					{
					case 0x00: break; // nop
					case 0x01: cont = false; Breakwarn( "Debug Break", pc ); break; // break
					case 0x02: stackBuffer[++sp] = stackBuffer[parameterStart+0]; break; //ldarg.0
					case 0x03: stackBuffer[++sp] = stackBuffer[parameterStart+1]; break; //ldarg.1
					case 0x04: stackBuffer[++sp] = stackBuffer[parameterStart+2]; break; //ldarg.2
					case 0x05: stackBuffer[++sp] = stackBuffer[parameterStart+3]; break; //ldarg.3
					case 0x06: stackBuffer[++sp] = stackBuffer[localVarsHead+0]; break; //ldloc.0
					case 0x07: stackBuffer[++sp] = stackBuffer[localVarsHead+1]; break; //ldloc.1
					case 0x08: stackBuffer[++sp] = stackBuffer[localVarsHead+2]; break; //ldloc.2
					case 0x09: stackBuffer[++sp] = stackBuffer[localVarsHead+3]; break; //ldloc.3
					case 0x0a: stackBuffer[localVarsHead+0] = stackBuffer[sp--]; break; //stloc.0
					case 0x0b: stackBuffer[localVarsHead+1] = stackBuffer[sp--]; break; //stloc.1
					case 0x0c: stackBuffer[localVarsHead+2] = stackBuffer[sp--]; break; //stloc.2
					case 0x0d: stackBuffer[localVarsHead+3] = stackBuffer[sp--]; break; //stloc.3
					case 0x0e: stackBuffer[++sp] = stackBuffer[parameterStart+byteCode[pc++]]; break; // ldarg.s <uint8 (argNum)>
					case 0x0f: stackBuffer[++sp] = StackElement.CreateReference( stackBuffer, (uint)parameterStart+byteCode[pc++] ); break; // ldarga.s <uint8 (argNum)>
					case 0x11: stackBuffer[++sp] = stackBuffer[localVarsHead+byteCode[pc++]]; break; //ldloc.s
					case 0x12:
					{
						uint whichLocal = byteCode[pc++];
						stackBuffer[++sp] = StackElement.CreateReference( stackBuffer, (uint)(localVarsHead+whichLocal) );
						break; //ldloca.s // Load address of local variable.
					}
					case 0x13: stackBuffer[localVarsHead+byteCode[pc++]] = stackBuffer[sp--]; break; //stloc.s
					case 0x14: stackBuffer[++sp].LoadObject( null ); break; // ldnull
					case 0x15: stackBuffer[++sp].LoadInt( -1 ); break; // ldc.i4.m1
					case 0x16: stackBuffer[++sp].LoadInt( 0 ); break; // ldc.i4.0
					case 0x17: stackBuffer[++sp].LoadInt( 1 ); break; // ldc.i4.1
					case 0x18: stackBuffer[++sp].LoadInt( 2 ); break; // ldc.i4.2
					case 0x19: stackBuffer[++sp].LoadInt( 3 ); break; // ldc.i4.3
					case 0x1a: stackBuffer[++sp].LoadInt( 4 ); break; // ldc.i4.4
					case 0x1b: stackBuffer[++sp].LoadInt( 5 ); break; // ldc.i4.5
					case 0x1c: stackBuffer[++sp].LoadInt( 6 ); break; // ldc.i4.6
					case 0x1d: stackBuffer[++sp].LoadInt( 7 ); break; // ldc.i4.7
					case 0x1e: stackBuffer[++sp].LoadInt( 8 ); break; // ldc.i4.8

					case 0x1f: stackBuffer[++sp].LoadInt( (sbyte)byteCode[pc++] ); break; // ldc.i4.s <int8>
					case 0x20: stackBuffer[++sp].LoadInt( (int)BytecodeAsU32( ref pc ) ); break; // ldc.i4 <int32>
					case 0x21: stackBuffer[++sp].LoadLong( (long)BytecodeAs64( ref pc ) ); break; // ldc.i8 <int64>
					case 0x22: stackBuffer[++sp].LoadFloat( CilboxUtil.IntFloatConverter.ConvertUtoF(BytecodeAsU32( ref pc ) ) ); break; // ldc.r4 <float32 (num)>
					case 0x23: stackBuffer[++sp].LoadDouble( CilboxUtil.IntFloatConverter.ConvertEtoD(BytecodeAs64( ref pc ) ) ); break; // ldc.r8 <float64 (num)>
					// 0x24 does not exist.
					case 0x25: stackBuffer[sp+1] = stackBuffer[sp]; sp++; break; // dup TODO: Does dup potentially duplicate objects somehow?
					case 0x26: sp--; break; // pop

					case 0x27: //jmp
					case 0x28: //call
					case 0x29: //calli
					case 0x73: //newobj
					case 0x6F: //callvirt
					{
						uint bc = (b == 0x29) ? stackBuffer[sp--].u : BytecodeAsU32( ref pc );
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
							isVoid = targetMethod.isVoid;
							if( targetMethod == null )
							{
								throw( new Exception( $"Function {dt.Name} not found" ) );
							}
							int staticOffset = (targetMethod.isStatic?0:1);
							int numParams = targetMethod.signatureParameters.Length;

							// TODO: Remove this somehow?
							//StackElement [] cparameters = new StackElement[numParams + staticOffset];

							int nextParameterStart = stackContinues;
							int nextStackHead = nextParameterStart + numParams + staticOffset;

							//Debug.Log( $"Getting values {numParams} {sp} {targetMethod.isStatic}" );
							for( int i = numParams - 1; i >= 0; i-- )
								stackBuffer[nextParameterStart+i+staticOffset] = stackBuffer[sp--];

							if( !targetMethod.isStatic )
								stackBuffer[nextParameterStart] = stackBuffer[sp--];

							if( !isVoid )
								stackBuffer[++sp] = targetMethod.InterpretInner( stackBuffer, nextParameterStart, nextStackHead );
							else
								targetMethod.InterpretInner( stackBuffer, nextParameterStart, nextStackHead );

							if( b == 0x27 )
							{
								// This is returning from a jump, so immediately abort.
								if( isVoid ) stackBuffer[++sp] = StackElement.nil; /// ?? Please check me! If wrong, fix above, too.
								cont = false;
							}

						}
						else
						{
							//st = dt.assembly.ManifestModule.ResolveMethod((int)dt.nativeToken);
							st = dt.nativeMethod;
							if( st is MethodInfo )
								isVoid = ((MethodInfo)st).ReturnType == typeof(void);

							ParameterInfo [] pa = st.GetParameters();
							int numFields = pa.Length;
							object callthis = null;
							object [] callpar = new object[numFields];
							StackElement callthis_se = new StackElement{};
							StackElement [] callpar_se = new StackElement[numFields];
							int ik;
							for( ik = 0; ik < numFields; ik++ )
							{
								StackElement se = stackBuffer[sp--];
								callpar_se[numFields-ik-1] = se;
								object o = se.AsObject();
								Type t = pa[ik].ParameterType;

								// XXX TODO: Copy mechanism below from ResolveToStackElement and Coerce
								if( se.type < StackType.Object )
								{
									if( o != null && t.IsValueType && o.GetType() != t )
									{
										//o = Convert.ChangeType( o, t );
										o = se.CoerceToObject( t );
									}
								}
								callpar[numFields-ik-1] = o;
							}
							if( st.IsConstructor )
							{
								// XXX TRICKY TRICKY: This is kinda cheating.  We
								// Only really get here by constructor code, when we
								// were already constructed.
								if( st.DeclaringType == typeof( MonoBehaviour ) )
									callthis = this;
								else
									callthis = Activator.CreateInstance(st.DeclaringType);

								// XXX TRICKY TRICKY: This is kinda cheating. 
								// See above comment
								if( st.DeclaringType == typeof( MonoBehaviour ) )
									iko = this;
								else
									iko = ((ConstructorInfo)st).Invoke( callpar );
							}
							else if( !st.IsStatic )
							{
								MethodInfo mi = (MethodInfo)st;
								StackElement seorig = stackBuffer[sp--];
								StackElement se = StackElement.ResolveToStackElement( seorig );
								Type t = mi.DeclaringType;

								if( t.IsValueType && se.type < StackType.Object )
								{
									// Try to coerce types.
									callthis = se.CoerceToObject( t );
								}
								else
								{
									callthis = se.o;
								}

								iko = st.Invoke( callthis, callpar );
								if( seorig.type == StackType.Address )
								{
									seorig.DereferenceLoad( callthis );
								}
							}
							else
							{
								iko = st.Invoke( null, callpar );
							}

							// Possibly copy back any references.
							for( ik = 0; ik < numFields; ik++ )
							{
								StackElement se = callpar_se[ik];
								if( se.type == StackType.Address )
								{
									callpar_se[ik].DereferenceLoad( callpar[ik] );
									//if( se.o.GetType() == typeof(StackElement[]) )
									//	((StackElement[])se.o)[se.i].Load( callpar[ik] );
									//else
									//	((Array)se.o).SetValue(callpar[ik], se.i);
								}
							}

							if( !isVoid )
							{
								stackBuffer[++sp].Load( iko );
							}
							if( b == 0x27 )
							{
								// This is returning from a jump, so immediately abort.
								if( isVoid ) stackBuffer[++sp] = StackElement.nil; /// ?? Please check me! If wrong, fix above, too.
								cont = false;
							}
						}

						break;
					}
					case 0x2a: cont = false; break; // ret

					case 0x2b: pc += (sbyte)byteCode[pc] + 1; break; //br.s
					case 0x38: { int ofs = (int)BytecodeAsU32( ref pc ); pc += ofs; break; } // br

					case 0x2c: case 0x39: // brfalse.s, brnull.s, brzero.s - is it zero, null or  / brfalse
					case 0x2d: case 0x3a: // brinst.s, brtrue.s / btrue
					{
						StackElement s = stackBuffer[sp--];
						int iop = b - 0x2c;
						if( b >= 0x38 ) iop -= 0xd;
						int offset = (b >= 0x38) ? (int)BytecodeAsU32( ref pc ) : (sbyte)byteCode[pc++];

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
						StackElement sb = stackBuffer[sp--]; StackElement sa = stackBuffer[sp--];
						int iop = b - 0x2e;
						if( b >= 0x38 ) iop -= 0xd;
						int joffset = (b >= 0x38) ? (int)BytecodeAsU32( ref pc ) : (sbyte)byteCode[pc++];

						StackType promoted = StackElement.StackTypeMaxPromote( sa.type, sb.type );

						switch( promoted )
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
					case 0x45: // Switch
					{
						int nsw = (int)BytecodeAsU32( ref pc );
						int startpc = pc;
						pc += nsw * 4;
						StackElement s = stackBuffer[sp--];
						if( s.type > StackType.Ulong )
							throw new ( "Stack type invalid for switch statement" );

						if( s.u < nsw )
						{
							int smatch = (int)(s.u * 4 + startpc);
							int ofs = (int)BytecodeAsU32( ref smatch );
							pc += ofs;
						}
						// Otherwise fall through
						break;
					}

					case 0x58: case 0x59: case 0x5A: case 0x5B: case 0x5C: case 0x5D:
					case 0x5E: case 0x5F: case 0x60: case 0x61: case 0x62: case 0x63:
					case 0x64:
					{
						StackElement sb = stackBuffer[sp--];
						StackElement sa = stackBuffer[sp];
						StackType promoted = StackElement.StackTypeMaxPromote( sa.type, sb.type );

						switch( b-0x58 )
						{
							case 0: // Add
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i + sb.i ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u + sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadLong( sa.l + sb.l ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e + sb.e ); break;
									case StackType.Float:	stackBuffer[sp].LoadFloat( sa.f + sb.f ); break;
									case StackType.Double:	stackBuffer[sp].LoadDouble( sa.d + sb.d ); break;
								} break;
							case 1: // Sub
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i - sb.i ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u - sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadLong( sa.l - sb.l ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e - sb.e ); break;
									case StackType.Float:	stackBuffer[sp].LoadFloat( sa.f - sb.f ); break;
									case StackType.Double:	stackBuffer[sp].LoadDouble( sa.d - sb.d ); break;
								} break;
							case 2: // Mul
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i * sb.i ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u * sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadLong( sa.l * sb.l ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e * sb.e ); break;
									case StackType.Float:	stackBuffer[sp].LoadFloat( sa.f * sb.f ); break;
									case StackType.Double:	stackBuffer[sp].LoadDouble( sa.d * sb.d ); break;
								} break;
							case 3: // Div
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i / sb.i ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u / sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadLong( sa.l / sb.l ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e / sb.e ); break;
									case StackType.Float:	stackBuffer[sp].LoadFloat( sa.f / sb.f ); break;
									case StackType.Double:	stackBuffer[sp].LoadDouble( sa.d / sb.d ); break;
								} break;
							case 4: // Div.un
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadUint( sa.u / sb.u ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u / sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadUlong( sa.e / sb.e ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e / sb.e ); break;
									default: Breakwarn( "Unexpected div.un instruction behavior", pc); break;
								} break;
							case 5: // rem
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i % sb.i ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u % sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadLong( sa.l % sb.l ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e % sb.e ); break;
									default: Breakwarn( "Unexpected rem instruction behavior", pc); break;
								} break;
							case 6: // rem.un
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadUint( sa.u % sb.u ); break;
									case StackType.Uint:	stackBuffer[sp].LoadUint( sa.u % sb.u ); break;
									case StackType.Long:	stackBuffer[sp].LoadUlong( sa.e % sb.e ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadUlong( sa.e % sb.e ); break;
									default: Breakwarn( "Unexpected rem.un instruction behavior", pc); break;
								} break;
							case 7: stackBuffer[sp].LoadUlongType( sa.e & sb.e, promoted ); break; // and
							case 8: stackBuffer[sp].LoadUlongType( sa.e | sb.e, promoted ); break; // or
							case 9: stackBuffer[sp].LoadUlongType( sa.e ^ sb.e, promoted ); break; // xor
							case 10: stackBuffer[sp].LoadUlongType( sa.e << sb.i, promoted ); break; // shl
							case 11: stackBuffer[sp].LoadLongType( sa.l >> sb.i, promoted ); break; // shr
							case 12: stackBuffer[sp].LoadUlongType( sa.e >> sb.i, promoted ); break; // shr.un
						}
						break;
					}

					case 0x65: stackBuffer[sp].l = -stackBuffer[sp].l; break;
					case 0x66: stackBuffer[sp].e ^= 0xffffffffffffffff; break;

					// XXX TODO: Perf improvement, detect float-to-int conversions and fast-path them.
					// C# Does not want you to blindly interpret these.
					case 0x67: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadSByte( ((se.type < StackType.Float) ? (sbyte)se.u  : (sbyte)se.CoerceToObject(typeof(sbyte)))  ); break; } // conv.i1
					case 0x68: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadShort( ((se.type < StackType.Float) ? (short)se.i  : (short)se.CoerceToObject(typeof(short)))  ); break; } // conv.i2
					case 0x69: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadInt(   ((se.type < StackType.Float) ? (int)se.i    : (int)se.CoerceToObject(typeof(int)))      ); break; } // conv.i4
					case 0x6A: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadLong(  ((se.type < StackType.Float) ? (long)se.l   : (long)se.CoerceToObject(typeof(long)))    ); break; } // conv.i8
					case 0x6B: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadFloat( ((se.type < StackType.Float) ? (float)se.l  : (float)se.CoerceToObject(typeof(float)))  ); break; } // conv.r4
					case 0x6C: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadDouble(((se.type < StackType.Float) ? (double)se.l : (double)se.CoerceToObject(typeof(double)))); break; } // conv.r8
					case 0xD1: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadUshort(((se.type < StackType.Float) ? (ushort)se.u : (ushort)se.CoerceToObject(typeof(ushort)))); break; } // conv.u2
					case 0xD2: { StackElement se = stackBuffer[sp]; stackBuffer[sp].LoadByte(  ((se.type < StackType.Float) ? (byte)se.u   : (byte)se.CoerceToObject(typeof(byte)))    ); break; } // conv.u1

					case 0x72:
					{
						uint bc = BytecodeAsU32( ref pc );
						stackBuffer[++sp].Load( box.metadatas[bc].Name );
						break; //ldstr
					}

					case 0x7b: 
					{
						uint bc = BytecodeAsU32( ref pc );

						StackElement se = stackBuffer[sp--];

						if( se.o is CilboxProxy )
							stackBuffer[++sp] = ((CilboxProxy)se.o).fields[box.metadatas[bc].fieldIndex];
						else
							throw new Exception( "Unimplemented.  Attempting to get field on non-cilbox object" );
						break; //ldfld
					}
					case 0x7c:
					{
						uint bc = BytecodeAsU32( ref pc );
						StackElement se = stackBuffer[sp--];

						if( se.o is CilboxProxy )
							stackBuffer[++sp] = StackElement.CreateReference( (Array)(((CilboxProxy)se.o).fields), (uint)box.metadatas[bc].fieldIndex );
						else
							throw new Exception( "Unimplemented.  Attempting to get field on non-cilbox object" );
						break;// ldflda
					}
					case 0x7d:
					{
						uint bc = BytecodeAsU32( ref pc );
						StackElement se = stackBuffer[sp--];
						object opths = stackBuffer[sp--].AsObject();
						if( opths is CilboxProxy )
						{
							((CilboxProxy)opths).fields[box.metadatas[bc].fieldIndex] = se;
							//Debug.Log( "Type: " + ((CilboxProxy)opths).fields[box.metadatas[bc].fieldIndex].type );
						}
						else
							throw new Exception( "Unimplemented.  Attempting to set field on non-cilbox object" );
						break; //stfld
					}
					case 0x7e: 
					{
						uint bc = BytecodeAsU32( ref pc );
						stackBuffer[++sp].Load( parentClass.staticFields[box.metadatas[bc].fieldIndex] );
						break; //ldsfld
					}
					case 0x80:
					{
						uint bc = BytecodeAsU32( ref pc );
						parentClass.staticFields[box.metadatas[bc].fieldIndex] = stackBuffer[++sp].AsObject();
						break; //stsfld
					}
					case 0x8C: // box (This pulls off a type)
					{
						uint otyp = BytecodeAsU32( ref pc );
						stackBuffer[sp].LoadObject( stackBuffer[sp].AsObject() );//(metaType.nativeType)stackBuffer[sp-1].AsObject();
						break; 
					}
					case 0x8d:
					{
						uint otyp = BytecodeAsU32( ref pc );
						if( stackBuffer[sp].type > StackType.Ulong )
							throw new Exception( "Invalid type, processing new array" );
						int size = stackBuffer[sp].i;
						Type t = box.metadatas[otyp].nativeType;
						stackBuffer[sp].LoadObject( Array.CreateInstance( t, size ) );
						//newarr <etype>
						break;
					}
					case 0x8e:
					{
						stackBuffer[sp].LoadInt( ((Array)(stackBuffer[sp].o)).Length );
						break; //ldlen
					}
					case 0x8f:
					{
						/*uint whichClass = */BytecodeAsU32( ref pc ); // (For now, ignored)
						uint index = stackBuffer[sp--].u;
						Array a = (Array)(stackBuffer[sp--].AsObject());
						stackBuffer[++sp] = StackElement.CreateReference( a, index );
						break; //ldlema
					}
					case 0x90: case 0x91: case 0x92: case 0x93: case 0x94:
					case 0x95: case 0x96: case 0x97: case 0x98: case 0x99:
					{
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" + stackBuffer[sp].type + " " + stackBuffer[sp].o );
						int index = stackBuffer[sp--].i;
//						Array a = ((Array)(stackBuffer[sp].o));
						switch( b - 0x90 )
						{
/*
// Old way
						case 0: stackBuffer[sp].LoadSByte( (sbyte)(a.GetValue( index )) ); break; // ldelem.i1
						case 1: stackBuffer[sp].LoadByte( (byte)(a.GetValue( index )) ); break; // ldelem.u1
						case 2: stackBuffer[sp].LoadShort( (short)(a.GetValue( index )) ); break; // ldelem.i2
						case 3: stackBuffer[sp].LoadUshort( (ushort)(a.GetValue( index )) ); break; // ldelem.u2
						case 4: stackBuffer[sp].LoadInt( (int)(a.GetValue( index )) ); break; // ldelem.i4
						case 5: stackBuffer[sp].LoadUint( (uint)(a.GetValue( index )) ); break; // ldelem.u4
						case 6: stackBuffer[sp].LoadUlong( (ulong)(a.GetValue( index )) ); break; // ldelem.u8 / ldelem.i8
						case 7: stackBuffer[sp].LoadInt( (int)(a.GetValue( index )) ); break; // ldelem.i
						case 8: stackBuffer[sp].LoadFloat( (float)(a.GetValue( index )) ); break; // ldelem.r4
						case 9: stackBuffer[sp].LoadDouble( (double)(a.GetValue( index )) ); break; // ldelem.r8
*/
						// Does this way work universally?  Can we assume the compiler knows what it's doing?
						case 0: stackBuffer[sp].LoadSByte( (sbyte)(((sbyte[])stackBuffer[sp].o)[index]) ); break; // ldelem.i1
						case 1: stackBuffer[sp].LoadByte( (byte)(((byte[])stackBuffer[sp].o)[index]) ); break; // ldelem.u1
						case 2: stackBuffer[sp].LoadShort( (short)(((short[])stackBuffer[sp].o)[index]) ); break; // ldelem.i2
						case 3: stackBuffer[sp].LoadUshort( (ushort)(((ushort[])stackBuffer[sp].o)[index]) ); break; // ldelem.u2
						case 4: stackBuffer[sp].LoadInt( (int)(((int[])stackBuffer[sp].o)[index]) ); break; // ldelem.i4
						case 5: stackBuffer[sp].LoadUint( (uint)(((uint[])stackBuffer[sp].o)[index]) ); break; // ldelem.u4
						case 6: stackBuffer[sp].LoadUlong( (ulong)(((ulong[])stackBuffer[sp].o)[index]) ); break; // ldelem.u8 / ldelem.i8
						case 7: stackBuffer[sp].LoadInt( (int)(((int[])stackBuffer[sp].o)[index]) ); break; // ldelem.i
						case 8: stackBuffer[sp].LoadFloat( (float)(((float[])stackBuffer[sp].o)[index]) ); break; // ldelem.r4
						case 9: stackBuffer[sp].LoadDouble( (double)(((double[])stackBuffer[sp].o)[index]) ); break; // ldelem.r8

						}
						break;
					}
					case 0x9a:
					{
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" + stackBuffer[sp].type + " " + stackBuffer[sp].o );
						int index = stackBuffer[sp--].i;
						Array a = ((Array)(stackBuffer[sp--].o));
						stackBuffer[++sp].LoadObject( a.GetValue(index) );
						break; //Ldelem_Ref
					}
					case 0x9c:
					{
						SByte val = (SByte)stackBuffer[sp--].i;
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" + stackBuffer[sp].type + " " + stackBuffer[sp].o );
						int index = stackBuffer[sp--].i;
						((Array)(stackBuffer[sp--].o)).SetValue( (byte)val, index );
						break; // stelem.i1
					}
					case 0xa0:
					{
						float val;
						val = stackBuffer[sp--].f;
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" + stackBuffer[sp].type + " " + stackBuffer[sp].o );
						int index = stackBuffer[sp--].i;
						float [] array = (float[])stackBuffer[sp--].AsObject();
						array[index] = val;
						break; // stelem.r4
					}
					case 0xa2:
					{
						object val = stackBuffer[sp--].AsObject();
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" );
						int index = stackBuffer[sp--].i;
						object [] array = (object[])stackBuffer[sp--].AsObject();
						array[index] = val;
						break; // stelem.ref
					}
					case 0xa4:
					{
						uint otyp = BytecodeAsU32( ref pc );
						object val = stackBuffer[sp--].AsObject();
						if( stackBuffer[sp].type > StackType.Uint ) throw new Exception( "Invalid index type" );
						int index = stackBuffer[sp--].i;
						object [] array = (object[])stackBuffer[sp--].AsObject();
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
							stackBuffer[sp].Unbox( stackBuffer[sp].AsObject(), metaType.nativeTypeStackType );
						}
						else
						{
							Breakwarn( "Scary Unbox (that we don't have code for) from " + otyp + " ORIG " + metaType.ToString(), pc );
							disabled = true; cont = false;
						}
						break; // unbox.any
					}
					case 0xD0:
					{
						uint md = BytecodeAsU32( ref pc ); // Let's hope that somehow this isn't needed?
						CilMetadataTokenInfo mi = box.metadatas[md];
						object loadedObject = null;
						switch( mi.type )
						{
						case MetaTokenType.mtField: // Get type of field.
							loadedObject = mi.fieldIsStatic ?
								parentClass.staticFieldTypes[mi.fieldIndex] :
								parentClass.instanceFieldTypes[mi.fieldIndex];
							break;
						case MetaTokenType.mtArrayInitializer: // Get type of field.
							loadedObject = mi.arrayInitializerData;
							break;
						default: throw new Exception( "Error: opcode 0xD0 called on token ID " + md.ToString( "X8" ) + " Which is not currently handled." );
						}

						stackBuffer[++sp].LoadObject( loadedObject );

						break; // ldtoken <token>
					}

					case 0xfe: // Extended opcodes
						b = byteCode[pc++];
						switch( b )
						{
						case 0x01:
						case 0x02:
						case 0x03:
						case 0x04:
						case 0x05:
						{
							StackElement sb = stackBuffer[sp--];
							StackElement sa = stackBuffer[sp];
							StackType promoted = StackElement.StackTypeMaxPromote( sa.type, sb.type );
							switch( b )
							{
							case 0x01: // CEQ
								switch( promoted )
								{
									case StackType.Boolean: stackBuffer[sp].LoadInt( sa.i == sb.i ? 1 : 0 ); break;
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i == sb.i ? 1 : 0 ); break;
									case StackType.Uint:	stackBuffer[sp].LoadInt( sa.i == sb.i ? 1 : 0 ); break;
									case StackType.Long:	stackBuffer[sp].LoadInt( sa.l == sb.l ? 1 : 0 ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadInt( sa.l == sb.l ? 1 : 0 ); break;
									case StackType.Float:	stackBuffer[sp].LoadInt( sa.f == sb.f ? 1 : 0 ); break;
									case StackType.Double:	stackBuffer[sp].LoadInt( sa.d == sb.d ? 1 : 0 ); break;
									default: throw new Exception( $"CEQ Unimplemented type promotion ({promoted})" );
								} break;
							case 0x02: // CGT
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i > sb.i ? 1 : 0 ); break;
									case StackType.Uint:	stackBuffer[sp].LoadInt( sa.i > sb.i ? 1 : 0 ); break;
									case StackType.Long:	stackBuffer[sp].LoadInt( sa.l > sb.l ? 1 : 0 ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadInt( sa.l > sb.l ? 1 : 0 ); break;
									case StackType.Float:	stackBuffer[sp].LoadInt( sa.f > sb.f ? 1 : 0 ); break;
									case StackType.Double:	stackBuffer[sp].LoadInt( sa.d > sb.d ? 1 : 0 ); break;
									default: throw new Exception( $"CEQ Unimplemented type promotion ({promoted})" );
								} break;
							case 0x03: // CGT.UN
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.u > sb.u ? 1 : 0 ); break;
									case StackType.Uint:	stackBuffer[sp].LoadInt( sa.u > sb.u ? 1 : 0 ); break;
									case StackType.Long:	stackBuffer[sp].LoadInt( sa.e > sb.e ? 1 : 0 ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadInt( sa.e > sb.e ? 1 : 0 ); break;
									case StackType.Float:	stackBuffer[sp].LoadInt( sa.f > sb.f ? 1 : 0 ); break;
									case StackType.Double:	stackBuffer[sp].LoadInt( sa.d > sb.d ? 1 : 0 ); break;
									default: throw new Exception( $"CEQ Unimplemented type promotion ({promoted})" );
								} break;
							case 0x04: // CLT
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.i < sb.i ? 1 : 0 ); break;
									case StackType.Uint:	stackBuffer[sp].LoadInt( sa.i < sb.i ? 1 : 0 ); break;
									case StackType.Long:	stackBuffer[sp].LoadInt( sa.l < sb.l ? 1 : 0 ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadInt( sa.l < sb.l ? 1 : 0 ); break;
									case StackType.Float:	stackBuffer[sp].LoadInt( sa.f < sb.f ? 1 : 0 ); break;
									case StackType.Double:	stackBuffer[sp].LoadInt( sa.d < sb.d ? 1 : 0 ); break;
									default: throw new Exception( $"CEQ Unimplemented type promotion ({promoted})" );
								} break;
							case 0x05: // CLT.UN
								switch( promoted )
								{
									case StackType.Int:		stackBuffer[sp].LoadInt( sa.u < sb.u ? 1 : 0 ); break;
									case StackType.Uint:	stackBuffer[sp].LoadInt( sa.u < sb.u ? 1 : 0 ); break;
									case StackType.Long:	stackBuffer[sp].LoadInt( sa.e < sb.e ? 1 : 0 ); break;
									case StackType.Ulong:	stackBuffer[sp].LoadInt( sa.e < sb.e ? 1 : 0 ); break;
									case StackType.Float:	stackBuffer[sp].LoadInt( sa.f < sb.f ? 1 : 0 ); break;
									case StackType.Double:	stackBuffer[sp].LoadInt( sa.d < sb.d ? 1 : 0 ); break;
									default: throw new Exception( $"CEQ Unimplemented type promotion ({promoted})" );
								} break;
							}
							break;
						}

						default:
							throw new Exception( $"Opcode 0xfe 0x{b.ToString("X2")} unimplemented" );
						}
						break;


					default: Breakwarn( $"Opcode 0x{b.ToString("X2")} unimplemented", pc ); disabled = true; cont = false; break;
					}

					ctr++;
					box.stepsThisInvoke++;

					if( ( box.stepsThisInvoke & 0x3f ) == 0 )
					{
						// Only check every 64.
						long elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - box.startTime);
						if( elapsed > Cilbox.timeoutLengthTicks )
						{
							throw new Exception( "Infinite Loop @ " + pc + " In " + methodName + " (Timeout ticks: " + elapsed + "/" + Cilbox.timeoutLengthTicks + " )" );
						}
					}
//pfm.End();
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
					Breakwarn( e.ToString(), pc );
					throw;
				}
			}
			//if( box.nestingDepth == 1 ) Debug.Log( "This invoke took: " + box.stepsThisInvoke );
			box.nestingDepth--;
#if UNITY_EDITOR
			perfMarkerInterpret.End();
#endif
			return ( sp == -1 ) ? StackElement.nil : stackBuffer[sp--];
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

		public CilboxClass( Cilbox box, String className, Serializee classData )
		{
			this.box = box;
			this.className = className;

			Dictionary<String, Serializee> classProps = classData.AsMap();

			uint id = 0;
			Serializee [] staticFields = classProps["staticFields"].AsArray();
			int sfnum = staticFields.Length;
			this.staticFields = new object[sfnum];
			staticFieldNames = new String[sfnum];
			staticFieldTypes = new Type[sfnum];
			for( int k = 0; k < sfnum; k++ )
			{
				Dictionary< String, String > field = staticFields[k].AsStringMap();
				String fieldName = staticFieldNames[id] = field["name"];
				Type t = staticFieldTypes[id] = Cilbox.GetNativeTypeFromName( field["type"] );

				//staticFieldIDs[id] = Cilbox.FindInternalMetadataID( className, 4, fieldName );
				this.staticFields[id] = CilboxUtil.DeserializeDataForProxyField( t, "" );
				id++;
			}

			Serializee [] instanceFields = classProps["instanceFields"].AsArray();
			int ifnum = instanceFields.Length;
			instanceFieldNames = new String[ifnum];
			instanceFieldTypes = new Type[ifnum];

			id = 0;
			for( int k = 0; k < ifnum; k++ )
			{
				Dictionary< String, String > field = instanceFields[k].AsStringMap();
				String fieldName = instanceFieldNames[id] = field["name"];
				instanceFieldTypes[id] = Cilbox.GetNativeTypeFromName( field["type"] );
				id++;
			}

			id = 0;
			Dictionary< String, Serializee > deserMethods = classProps["methods"].AsMap();
			int mnum = deserMethods.Count;
			methods = new CilboxMethod[mnum];
			methodNameToIndex = new Dictionary< String, uint >();
			methodFullSignatureToIndex = new Dictionary< String, uint >();
			foreach( var k in deserMethods )
			{
				methods[id] = new CilboxMethod();
				methods[id].Load( this, k.Key, k.Value );
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
			}
		}
	}

	public class CilboxPublicUtils
	{
		public static void InitializeArray(Array arr, byte[] initializer)
		{
			if (initializer == null || arr == null)
			{
				throw new Exception( "Error, array or initializer are null" );
			}
			if (initializer.Length != System.Runtime.InteropServices.Marshal.SizeOf(arr.GetType().GetElementType()) * arr.Length) {
				throw new Exception( "InitializeArray requires identical array byte length " + initializer.Length );
			}
			Buffer.BlockCopy(initializer, 0, arr, 0, initializer.Length);
		}
	}

	public class CilMetadataTokenInfo
	{
		public CilMetadataTokenInfo( MetaTokenType type ) { this.type = type; }
		public MetaTokenType type;
		public bool isValid;
		public int fieldIndex; // Only used for fields.
		public bool fieldIsStatic;

		public Type nativeType; // Used for types.
		public bool nativeTypeIsStackType;
		public StackType nativeTypeStackType;

		public byte[] arrayInitializerData;

		// Todo handle interpreted types.
		public bool isNative;
		public MethodBase nativeMethod;
		public int interpretiveMethod; // If nativeToken is 0, then it's a interpreted call.
		public int interpretiveMethodClass; // If nativeToken is 0, then it's a interpreted call class

		// For string, type = 0x70, string is in fields[0] (escaped) and Name, unescaped.
		// For methods, type = 10, Declaring Type is in fields[0], Method is in fields[1], Full name is in fields[2] assembly name is in fields[3]
		// For fields, type = 4, Declaring Type is in fields[0], Name is in fields[1], Type is in fields[2]
		//public String [] fields;

		public String Name;
		public String declaringTypeName;
		//public String ToString() { return Name; }
	}

	public enum MetaTokenType
	{
		mtType = 1,
		mtField = 4,
		mtString = 0x70,
		mtMethod = 10,
		mtArrayInitializer = 13, // Made-up type. 13 is unused in HandleKind.
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
		public static readonly long timeoutLengthTicks = 500000000; // 5000ms
		public static readonly int defaultStackSize = 1024;

		public bool showFunctionProfiling;


		Cilbox()
		{
			initialized = false;
		}

		public void ForceReinit()
		{
			initialized = false;
		}

		public static Type GetNativeTypeFromName( String typeName )
		{
			// XXX SECURITY: DECIDE HERE IF THIS TYPE IS OKAY FOR THE CLIENT TO HAVE.
			if (typeName.Equals(typeof(System.Runtime.CompilerServices.RuntimeHelpers).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() class name.
				// This probably should move somewhere else if we add sandboxing.
				typeName = typeof(CilboxPublicUtils).FullName;
			}
			if (typeName.Equals(typeof(System.RuntimeFieldHandle).FullName)) {
				// Rewrite RuntimeHelpers.InitializeArray() second argument.
				// This probably should move somewhere else if we add sandboxing.
				typeName = typeof(byte[]).FullName;
			}
			Type ret = Type.GetType( typeName );
			if( ret != null ) return ret;

			System.Reflection.Assembly [] assys = AppDomain.CurrentDomain.GetAssemblies();
			foreach( System.Reflection.Assembly a in assys )
			{
				ret = a.GetType( typeName );
				if( ret != null ) return ret;
			}
			return null;
		}

		public static Type[] TypeNamesToArrayOfNativeTypes( String [] parameterNames )
		{
			// XXX SECURITY: DECIDE HERE IF A GIVEN NATIVE TYPE GROUP

			if( parameterNames == null ) return null;
			Type[] ret = new Type[parameterNames.Length];
			for( int i = 0; i < parameterNames.Length; i++ )
			{
				Type pt = ret[i] = GetNativeTypeFromName(  parameterNames[i]  );
					//GetNativeTypeFromName( assemblyAndTypeName[0], assemblyAndTypeName[1] );
			}
			return ret;
		}

		MethodBase GetNativeMethodFromTypeAndName( Type declaringType, String name, Type [] parameters, String [] genericArguments, String fullSignature )
		{
			// XXX SECURITY: DECIDE HERE IF A GIVEN METHOD IS OK


			// XXX Can we combine Constructor + Method?
			MethodBase m = declaringType.GetMethod(
				name,
				genericArguments.Length,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
				null,
				CallingConventions.Any,
				parameters,
				null ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."
/* Can't seem to do this??
			if( m == null )
			{
				m = declaringType.GetConstructor(
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
					null,
					CallingConventions.Any,
					parameters,
					modifiers ); // TODO I don't ... think? we need parameter modifiers? "To be only used when calling through COM interop, and only parameters that are passed by reference are handled. The default binder does not process this parameter."
			}
*/
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
		    	m = ((MethodInfo)m).MakeGenericMethod( TypeNamesToArrayOfNativeTypes( genericArguments ) );
			}

			return m;
		}

		public void BoxInitialize()
		{
#if UNITY_EDITOR
			new ProfilerMarker( "Initialize Cilbox" ).Auto();
#endif

			if( initialized ) return;
			initialized = true;
			Debug.Log( "Cilbox Initialize Metadata:" + assemblyData.Length );

			Dictionary< String, Serializee > assemblyRoot = Serializee.CreateFromBlob( Convert.FromBase64String( assemblyData ) ).AsMap();
			Dictionary< String, Serializee > classData = assemblyRoot["classes"].AsMap();
			Dictionary< String, Serializee > metaData = assemblyRoot["metadata"].AsMap();

			metadatas = new CilMetadataTokenInfo[metaData.Count+1]; // element 0 is invalid.
			metadatas[0] = new CilMetadataTokenInfo( 0 );
			metadatas[0].Name = "<INVALID>";

			int clsid = 0;
			classes = new Dictionary< String, int >();
			classesList = new CilboxClass[classData.Count];
			foreach( var v in classData )
			{
				CilboxClass cls = new CilboxClass( this, v.Key, v.Value );
				classesList[clsid] = cls;
				classes[(String)v.Key] = clsid;
				clsid++;
			}

			foreach( var v in metaData )
			{
				int mid = Convert.ToInt32((String)v.Key);
				Dictionary< String, Serializee > st = v.Value.AsMap();
				MetaTokenType metatype = (MetaTokenType)Convert.ToInt32(st["mt"].AsString());
				CilMetadataTokenInfo t = metadatas[mid] = new CilMetadataTokenInfo( metatype );

				t.type = metatype;
				t.Name = "<UNKNOWN>";

				switch( metatype )
				{
				case MetaTokenType.mtString:
					t.Name = st["s"].AsString();
					break;
				case MetaTokenType.mtArrayInitializer:
					t.arrayInitializerData = Convert.FromBase64String(st["data"].AsString());
					break;
				case MetaTokenType.mtField:
					// The type has been "sealed" so-to-speak. In that we have an index for it.

					t.fieldIndex = Convert.ToInt32(st["index"].AsString());
					t.fieldIsStatic = Convert.ToInt32(st["isStatic"].AsString()) != 0;
					t.Name = st["name"].AsString();
					t.declaringTypeName = st["dt"].AsString();

					t.isValid = true;
					break;
				case MetaTokenType.mtType:
				{
					String hostTypeName = st["name"].AsString();
					String useAssembly = st["assy"].AsString();
					StackType nst;
					t.nativeType = GetNativeTypeFromName( hostTypeName );

					if( StackElement.TypeToStackType.TryGetValue( hostTypeName, out nst ) )
					{
						t.nativeTypeIsStackType = true;
						t.nativeTypeStackType = nst;
					}
					else
					{
						t.isValid = t.nativeType != null;

						if( !t.isValid )
						{
							Debug.LogError( $"Error: Could not find type: {hostTypeName}" );
						}
						else
						{
							t.Name = "Type: " + hostTypeName;
						}
					}
					break;
				}
				case MetaTokenType.mtMethod:
				{
					String declaringTypeName = st["dt"].AsString();
					String [] parameterNames = null;
					String name = st["name"].AsString();
					String fullSignature = st["fullSignature"].AsString();
					String useAssembly = st["assembly"].AsString();
					String [] genericArguments = null;
					t.Name = "Method: " + name;
					t.declaringTypeName = st["dt"].AsString();

					//Possibly get genericArguments
					Serializee temp;
					if( st.TryGetValue( "ga", out temp ) )
						genericArguments = temp.AsStringArray();
					else
						genericArguments = new String[0];

					if( st.TryGetValue( "parameters", out temp ) )
						parameterNames = temp.AsStringArray();
					else
						parameterNames = new String[0];


					// First, see if this is to a class we are responsible for. Like does it come from _this_ class?
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
						Type declaringType = GetNativeTypeFromName( declaringTypeName );
						if( declaringType == null )
						{
							throw new Exception( $"Error: Could not find referenced type {useAssembly}/{declaringTypeName}/" );
						}

						Type [] parameters = TypeNamesToArrayOfNativeTypes( parameterNames );
						MethodBase m = GetNativeMethodFromTypeAndName( declaringType, name, parameters, genericArguments, fullSignature );

						if( m != null )
						{
							t.nativeMethod = m;
							t.isNative = true;
							t.isValid = true;
						} else if( !t.isNative )
						{
							throw new Exception( "Error: Could not find reference to: [" + useAssembly + "][" + declaringType.FullName + "][" + fullSignature + "] Type from:" + declaringTypeName );
						}
					}
					break;
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

			object ret = cls.methods[index].Interpret( ths, parameters );

			// For profiling
			if( showFunctionProfiling )
			{
				long elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - startTime);
				Debug.Log( $"{stepsThisInvoke} in {elapsed/10}us or {stepsThisInvoke*10.0/(double)elapsed}MHz" );
			}
			return ret;
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
			//Debug.Log( "IProcessSceneWithReport" );
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

			ProfilerMarker perf = new ProfilerMarker("Initial Setup"); perf.Begin();

			MonoBehaviour [] allBehavioursThatNeedCilboxing = CilboxUtil.GetAllBehavioursThatNeedCilboxing();

			Debug.Log( $"Postprocessing scene. Cilbox scripts to do: {allBehavioursThatNeedCilboxing.Length}" );
			if( allBehavioursThatNeedCilboxing.Length == 0 ) return;

			Assembly proxyAssembly = typeof(CilboxProxy).Assembly;

			Dictionary< String, Serializee > assemblyMetadata = new Dictionary< String, Serializee >();
			Dictionary< uint, String > originalMetaToFriendlyName = new Dictionary< uint, String >();
			Dictionary< int, uint> assemblyMetadataReverseOriginal = new Dictionary< int, uint >();

			uint mdcount = 1; // token 0 is invalid.
			int bytecodeLength = 0;
			Dictionary< String, Serializee > classes = new Dictionary<String, Serializee>();
			Dictionary< String, Serializee > allClassMethods = new Dictionary< String, Serializee >();

			StreamWriter CLog = File.CreateText( Application.dataPath + "/CilboxLog.txt" );
			String typeLog = "";

			perf.End(); perf = new ProfilerMarker( "Main Getting Types" ); perf.Begin();

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				ProfilerMarker perfType = new ProfilerMarker(type.ToString()); perfType.Begin();

				Dictionary< String, Serializee > methods = new Dictionary< String, Serializee >();

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

						ProfilerMarker perfMethod = new ProfilerMarker(m.ToString()); perfMethod.Begin();

						String methodName = m.Name;
						Dictionary< String, Serializee > MethodProps = new Dictionary< String, Serializee >();
						//CLog.WriteLine( type + " / " + m.Name );
						MethodBody mb = m.GetMethodBody();
						if( mb == null )
						{
							Debug.Log( $"NOTE: {m.Name} does not have a body" );
							// Things like MemberwiseClone, etc.
							perfMethod.End();
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
									CilboxUtil.OpCodes.OpCode oc;
									try {
										oc = CilboxUtil.OpCodes.ReadOpCode( byteCode, ref i );
									} catch( Exception e )
									{
										Debug.LogError( e );
										Debug.LogError( "Exception decoding opcode at address " + i + " in " + m.Name + "\n" + asm );
										throw;
									}
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

									CilboxUtil.OpCodes.OperandType ot = oc.OperandType;

									if( ot == CilboxUtil.OpCodes.OperandType.InlineTok )
									{
										// Cheating: Just convert it to whatever we think it is.
										switch( operand>>24 )
										{
										case 0x04: // Special case handling for constant initializers.
											if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
											{
												writebackToken = mdcount;
												// Special <PrivateImplementationDetails>+__StaticArrayInitTypeSize=24 instance.
												FieldInfo rf = proxyAssembly.ManifestModule.ResolveField( (int)operand );
												// Extract raw bytes from initializer type
												byte[] bytes = new byte[System.Runtime.InteropServices.Marshal.SizeOf(rf.FieldType)];
												GCHandle h = GCHandle.Alloc(rf.GetValue(null), GCHandleType.Pinned);
												Marshal.Copy(h.AddrOfPinnedObject(), bytes, 0, bytes.Length);
												h.Free();
												// Now, encode our array initializer to base64.
												Dictionary< String, String > thisMeta = new Dictionary< String, String >();
												thisMeta["mt"] = ((int)MetaTokenType.mtArrayInitializer).ToString();
												thisMeta["data"] = Convert.ToBase64String(bytes);
												originalMetaToFriendlyName[mdcount] = rf.Name;
												assemblyMetadata[(mdcount++).ToString()] = new Serializee( thisMeta );
											}
											asm += "\t" + originalMetaToFriendlyName[writebackToken];
											break;
										default:
											throw new Exception( "Exception decoding opcode at address (confusing meta " + operand.ToString("X8") + ") " + i + " in " + m.Name + "\n" + asm );
										}
									}
									else if( ot == CilboxUtil.OpCodes.OperandType.InlineSwitch )
									{
										asm += $" Switch {operand} cases";
										int oin;
										for( oin = 0; oin < operand; oin++ )
										{
											int sws = (int)(uint)CilboxUtil.BytecodePullLiteral( byteCode, ref i, 4 );
											asm += " " + sws;
										}
										changeOperand = false;
									}
									else if( ot == CilboxUtil.OpCodes.OperandType.InlineString )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											Dictionary< String, String > thisMeta = new Dictionary< String, String >();
											String st = ((int)MetaTokenType.mtString).ToString();
											thisMeta["mt"] = st;
											thisMeta["s"] = proxyAssembly.ManifestModule.ResolveString( (int)operand );
											originalMetaToFriendlyName[mdcount] = st;
											assemblyMetadata[(mdcount++).ToString()] = new Serializee( thisMeta );
										}
										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( ot == CilboxUtil.OpCodes.OperandType.InlineMethod )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											MethodBase tmb = proxyAssembly.ManifestModule.ResolveMethod( (int)operand );

											Dictionary<String, Serializee> methodProps = new Dictionary<String, Serializee>();

											// "Generic constructors are not supported in the .NET Framework version 2.0"
											if( !tmb.IsConstructor )
											{
												Type[] templateArguments = tmb.GetGenericArguments();
												if( templateArguments.Length > 0 )
												{
													String [] argtypes = new String[templateArguments.Length];
													for( int a = 0; a < templateArguments.Length; a++ )
														argtypes[a] = templateArguments[a].ToString();  //Was FullName
													methodProps["ga"] = new Serializee( argtypes );
												}
											}

											methodProps["dt"] = new Serializee( tmb.DeclaringType.ToString() ); // Was FullName
											methodProps["name"] = new Serializee( tmb.Name );

											System.Reflection.ParameterInfo[] parameterInfos = tmb.GetParameters();
											if( parameterInfos.Length > 0 )
											{
												String [] sParameters = new String[parameterInfos.Length];
												for( var j = 0; j < parameterInfos.Length; j++ )
												{
													Type ty = parameterInfos[j].ParameterType;
													sParameters[j] = ty.ToString(); //Was FullName;
												}
												methodProps["parameters"] = new Serializee( sParameters );
											}
											methodProps["fullSignature"] = new Serializee( tmb.ToString() );
											methodProps["assembly"] = new Serializee( tmb.DeclaringType.Assembly.GetName().Name );
											methodProps["mt"] = new Serializee(((int)MetaTokenType.mtMethod).ToString());
											originalMetaToFriendlyName[writebackToken] = tmb.DeclaringType.ToString() + "." + tmb.ToString();
											assemblyMetadata[(mdcount++).ToString()] = new Serializee( methodProps );
										}

										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( ot == CilboxUtil.OpCodes.OperandType.InlineField )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											FieldInfo rf = proxyAssembly.ManifestModule.ResolveField( (int)operand );

											Dictionary<String, String> fieldProps = new Dictionary<String, String>();
											fieldProps["mt"] = ((int)MetaTokenType.mtField).ToString();
											fieldProps["dt"] = rf.DeclaringType.ToString();
											fieldProps["name"] = rf.Name;
											fieldProps["fullName"] = rf.FieldType.FullName;
											fieldProps["isStatic"] = (rf.IsStatic?1:0).ToString();
											originalMetaToFriendlyName[writebackToken] = rf.Name;
											assemblyMetadata[(mdcount++).ToString()] = new Serializee(fieldProps);
										}
										asm += "\t" + originalMetaToFriendlyName[writebackToken];
									}
									else if( ot == CilboxUtil.OpCodes.OperandType.InlineType )
									{
										if( !assemblyMetadataReverseOriginal.TryGetValue( (int)operand, out writebackToken ) )
										{
											writebackToken = mdcount;
											Type ty = proxyAssembly.ManifestModule.ResolveType( (int)operand );

											Dictionary<String, String> fieldProps = new Dictionary<String, String>();
											fieldProps["mt"] = ((int)MetaTokenType.mtType).ToString();
											fieldProps["assy"] = ty.Assembly.GetName().Name;
											fieldProps["name"] = ty.ToString();
											assemblyMetadata[(mdcount++).ToString()] = new Serializee( fieldProps );
											typeLog += ty.ToString() + "\n";
											originalMetaToFriendlyName[writebackToken] = ty.FullName;
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
						MethodProps["body"] = new Serializee(byteCodeStr);

						List< Serializee > localVars = new List< Serializee >();
						foreach (LocalVariableInfo lvi in mb.LocalVariables)
						{
							Dictionary< String, String > local = new Dictionary< String, String >();
							local["name"] = lvi.ToString();
							local["type"] = lvi.GetType().ToString();
							localVars.Add( new Serializee(local));
						}
						MethodProps["locals"] = new Serializee( localVars.ToArray() );

						ParameterInfo [] parameters = m.GetParameters();

						Serializee [] parameterList = new Serializee[parameters.Length];
						for( int i = 0; i < parameters.Length; i++ )
						{
							Dictionary< String, String > tpi = new Dictionary< String, String >();
							tpi["name"] = parameters[i].Name;
							tpi["type"] = parameters[i].ParameterType.ToString();
							parameterList[i] = new Serializee( tpi );
						}
						MethodProps["parameters"] = new Serializee( parameterList );
						MethodProps["maxStack"] = new Serializee( mb.MaxStackSize.ToString() );
						MethodProps["isVoid"] = new Serializee( (m is MethodInfo)?(((MethodInfo)m).ReturnType == typeof(void) ? "1" : "0" ): "0" );
						MethodProps["isStatic"] = new Serializee( m.IsStatic ? "1" : "0" );
						MethodProps["fullSignature"] = new Serializee( m.ToString() );

						methods[methodName] = new Serializee( MethodProps );
						perfMethod.End();
					}
				}

				allClassMethods[type.FullName] = new Serializee( methods );
				perfType.End();
			}

			perf.End(); perf = new ProfilerMarker( "Secondary Getting Types" ); perf.Begin();

			CLog.WriteLine( typeLog );

			// Now that we've iterated through all classes, and collected all possible uses of field IDs,
			// go through the classes again, collecting the fields themselves.

			foreach (Type type in proxyAssembly.GetTypes())
			{
				if( type.GetCustomAttributes(typeof(CilboxableAttribute), true).Length <= 0 )
					continue;

				ProfilerMarker perfType = new ProfilerMarker(type.ToString()); perfType.Begin();


				// This portion extracts the index information from the current type, and
				// Writes it back in where it was needed above in the Method call.
				//
				// XXX TODO REFACTOR ME.

				List< Serializee > staticFields = new List< Serializee >();
				int sfid = 0;
				FieldInfo[] fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static );
				foreach( var f in fi )
				{
					Dictionary< String, String > dictField = new Dictionary< String, String >();
					dictField["name"] = f.Name;
					dictField["type"] = f.FieldType.FullName;
					staticFields.Add( new Serializee( dictField ) );

					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
					{
						// XXX TODO: Clean me up.
						//Debug.Log( "RESEALING: " + mdid.ToString() );
						Serializee sOpen = assemblyMetadata[mdid.ToString()];
						//Debug.Log( "SOPEN: " + sOpen.DumpAsMemory() );
						Dictionary< String, Serializee > m = sOpen.AsMap();
						//Debug.Log( "GOT IT! " + m.Count );
						m["index"] = new Serializee( sfid.ToString() );
						assemblyMetadata[mdid.ToString()] = new Serializee( m );
						//Debug.Log( "SOPEN: " + assemblyMetadata[mdid.ToString()].DumpAsMemory() );
					}
					sfid++;
				}

				List< Serializee > instanceFields = new List< Serializee >();
				fi = type.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				int ifid = 0;
				foreach( var f in fi )
				{
					Dictionary< String, Serializee > thisInstanceField = new Dictionary< String, Serializee >();
					thisInstanceField["name"] = new Serializee( f.Name );
					thisInstanceField["type"] = new Serializee( f.FieldType.FullName );
					// Fill in our metadata with a class-specific field ID, if this field ID was used in code anywhere.
					uint mdid;
					if( assemblyMetadataReverseOriginal.TryGetValue(f.MetadataToken, out mdid) )
					{
						//Debug.Log( "RESEALING: B " + mdid.ToString() );
						// XXX TODO: Clean me up.
						Serializee sOpen = assemblyMetadata[mdid.ToString()];
						Dictionary< String, Serializee > m = sOpen.AsMap();
						m["index"] = new Serializee( ifid.ToString() );
						assemblyMetadata[mdid.ToString()] = new Serializee( m );
					}
					instanceFields.Add( new Serializee( thisInstanceField ) );
					ifid++;
				}

				Dictionary< String, Serializee > classProps = new Dictionary< String, Serializee >();
				classProps["methods"] = allClassMethods[type.FullName];
				classProps["staticFields"] = new Serializee( staticFields.ToArray() );
				classProps["instanceFields"] = new Serializee( instanceFields.ToArray() );
				classes[type.FullName] = new Serializee( classProps );
				perfType.End();
			}

			perf.End(); perf = new ProfilerMarker( "Assembling" ); perf.Begin();

			Dictionary< String, Serializee > assemblyRoot = new Dictionary< String, Serializee >();
			assemblyRoot["classes"] = new Serializee( classes );
			assemblyRoot["metadata"] = new Serializee( assemblyMetadata );

			perf.End(); perf = new ProfilerMarker( "Logging Entries" ); perf.Begin();

			foreach( var v in assemblyMetadata )
			{
				Dictionary< String, Serializee > fields = v.Value.AsMap();
				String sf = v.Key;
				foreach( var f in fields )
					sf += " " + f.ToString();
				CLog.WriteLine( sf );
			}

			perf.End(); perf = new ProfilerMarker( "Serializing" ); perf.Begin();

			String sAllAssemblyData = Convert.ToBase64String(new Serializee( assemblyRoot ).DumpAsMemory().ToArray() );

			perf.End(); perf = new ProfilerMarker( "Checking If Assembly Changed" ); perf.Begin();

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

			perf.End(); perf = new ProfilerMarker( "Applying Assembly" ); perf.Begin();

			if( bytecodeLength == 0 )
			{
				Debug.Log( "No bytecode available in this build. Falling back to last build." );
			}
			else
			{
				tac.assemblyData = sAllAssemblyData;
				tac.ForceReinit();
				//Debug.Log( "Outputting Assembly Data: " + sAllAssemblyData + " byteCode: " + bytecodeLength + " bytes " );
				CLog.WriteLine( "ByteCode: " + sAllAssemblyData.Length + " bytes " );
			}

			Dictionary< MonoBehaviour, CilboxProxy > refToProxyMap = new Dictionary< MonoBehaviour, CilboxProxy >();
			List< MonoBehaviour > refProxiesOrig = new List< MonoBehaviour >();
			List< CilboxProxy > refProxies = new List< CilboxProxy >();

			perf.End(); perf = new ProfilerMarker( "Updating Game Objects" ); perf.Begin();

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
			perf.End(); perf = new ProfilerMarker( "Setting Up Proxies" ); perf.Begin();

			var cnt = refProxies.Count;
			for( var i = 0; i < cnt; i++ )
			{
				CilboxProxy p = refProxies[i];
				MonoBehaviour m = refProxiesOrig[i];

				p.SetupProxy( tac, m, refToProxyMap );
			}

			perf.End(); perf = new ProfilerMarker( "Destroying Silboxable Scripts" ); perf.Begin();
			// re-attach the refrences to 
			foreach (MonoBehaviour m in allBehavioursThatNeedCilboxing)
			{
				UnityEngine.Object.DestroyImmediate( m );
			}
			perf.End();
		}
	}
	#endif

	public enum ImportFunctionID
	{
		dotCtor, // Must be at index 0.
		FixedUpdate,
		Update,
		Start,
		Awake,
	}
}

