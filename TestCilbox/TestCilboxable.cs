using UnityEngine;
using Cilbox;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using TestCilbox;

namespace TestCilbox
{
	[Cilboxable]
	public class TestCilboxBehaviour : MonoBehaviour
	{
		private int ipriviateinance = 555;
		public int ipublicinstance = 556;
		static private int iprivatestatic = 557;
		static public int ipublicstatic = 558;
		static public int recursive_test_counter = 0;
		public TestCilboxBehaviour2 behaviour2;
		public int[] intArr = new int[] { 1, 2, 3 };
		public TestCilboxBehaviour3[] myBehaviour3Arr;
		public MyEnum myEnumField = MyEnum.Value2;
		public TestEnum testEnumField = TestEnum.SecondValue;
		private TestState testStateField = TestState.Playing;
		private TestPayload testPayloadField = new TestPayload { Score = 123, Lives = 4 };

		public enum MyEnum
		{
			Value1,
			Value2,
			Value3 = 30,
		}

		private enum TestState : byte
		{
			Stopped = 0,
			Playing = 1,
			Paused = 2,
		}

		private struct TestPayload
		{
			public int Score;
			public byte Lives;
		}

		public TestCilboxBehaviour() { }

		public void RecursiveTest(int i)
		{
			recursive_test_counter++;
			if( i <= 0 ) return;
			RecursiveTest( i-1 );
			RecursiveTest( i-1 );
		}

		public void Start()
		{
			Debug.Log( "🔵 TestCilboxBehaviour.Start()" );

			Validator.Set( "Start Test", "OK" );

			int i = (int)(((int)(5+2/3)+50)*2/7.3);
			Validator.Set( "Arithmatic Test", i.ToString() );

			String startMarks = Validator.Get("Start Marks");
			if( startMarks == null ) startMarks = "";
			Validator.Set( "Start Marks", startMarks + "I" );

			Validator.Set( "private instance filed", ipriviateinance.ToString() );
			Validator.Set( "public instance field", ipublicinstance.ToString() );
			Validator.Set( "private static field", iprivatestatic.ToString() );
			Validator.Set( "public static field", ipublicstatic.ToString() );
			iprivatestatic *= 2;
			Validator.Set( "private static field x2", iprivatestatic.ToString() );

			RecursiveTest(8);
			Validator.Set( "recursive function", recursive_test_counter.ToString() );
			Validator.Set( "string concatenation", "it" + " " + "works" );
			Validator.Set( "MathF.Sin", MathF.Sin(3.2f).ToString() );

			using (DisposeTester dt = new DisposeTester())
			{
				Validator.Set("Exited Dispose Tester", "no");
			}
			Validator.Set( "Exited Dispose Tester", "yes" );

			Validator.Set("TryCatch", "did not catch");
			try
			{
				Validator.Set("TryFinally", "try");
				try
				{
					Validator.Set("TryFinally2", "try");
					throw new Exception("Test Exception");
				}
				catch (Exception)
				{
					Validator.Set("TryCatch", "caught");
				}
				finally
				{
					Validator.Set("TryFinally2", "finally");
					Validator.AddCount("TryFinally2");
				}
			}
			finally
			{
				Validator.Set("TryFinally", "finally");
				Validator.AddCount("TryFinally");
			}

			try
			{
				Validator.Set("DivideByZeroException", "try");
				int zero = 0;
				int test = 5 / zero;
				Validator.Set("DivideByZeroException", "didn't throw");
			}
			catch (DivideByZeroException)
			{
				Validator.Set("DivideByZeroException", "caught");
			}

			try
			{
				Validator.Set("TryFinallyNestedTest1", "try");
				Validator.Set("TryFinallyNestedTest2", "top");
				try
				{
					object test = null;
					Validator.Set("NullReferenceException", "try");
					Validator.Set("TryFinally3", "try");
					Validator.Set("NullRefUnreachable", "didn't reach");
					int len = test.ToString().Length;
					Validator.Set("NullRefUnreachable", "reached");
				}
				catch (DivideByZeroException)
				{
					Validator.Set("NullReferenceException", "caught0");
				}
				catch (NullReferenceException)
				{
					Validator.Set("NullReferenceException", "caught1");
				}
				catch (Exception)
				{
					Validator.Set("NullReferenceException", "caught2");
				}
				finally
				{
					Validator.Set("TryFinally3", "finally");
					Validator.AddCount("TryFinally3");
				}

				Validator.Set("TryFinallyNestedTest2", "bottom");
			}
			finally
			{
				Validator.Set("TryFinallyNestedTest1", "finally");
				Validator.AddCount("TryFinallyNestedTest1");
			}

			float[] myArr = new float[] { 1.5f, 2.5f, 3.5f };
			Array.Resize(ref myArr, myArr.Length + 1);
			myArr[^1] = 4.5f;
			Validator.Set("JoinFloatArrayResized", string.Join(", ", myArr) );

			Dictionary<string, string> myDict = new Dictionary<string, string>();
			myDict["key1"] = "value1";
			myDict["key2"] = "value2";
			string[] array = new string[myDict.Count];
			myDict.Keys.CopyTo(array, 0);
			Validator.Set("DictionaryKeys", string.Join(", ", array) );

			Outer<string>.Middle<int, bool>.Inner<char> complex = new();
			Validator.Set("ComplexGenericType", complex.GetTypeNames());

			Vector3 testVec = new Vector3(7, 7, 7);
			testVec.x += 5;
			Validator.Set("TestVec.x", testVec.x.ToString() );
			testVec.y++;
			Validator.Set("TestVec.y", testVec.y.ToString() );

			int myInt = 14;
			ReadInt(ref myInt);
			ReadFloat(ref testVec.y);
			WriteInt(ref myInt, 42);
			WriteFloat(ref testVec.y, 42.0f);
			Validator.Set("New myInt", myInt.ToString() );
			Validator.Set("New testVec.y", testVec.y.ToString() );
			Vector3 testObj = null;
			try
			{
				Debug.Log("testObj ToString: " + testObj.x);
				Validator.Set("FieldAccessNullRef", "try");
			}
			catch (NullReferenceException)
			{
				Validator.Set("FieldAccessNullRef", "caught");
			}

			try
			{
				int idx = -1;
				Debug.Log("Negative index: " + intArr[idx]);
			}
			catch (IndexOutOfRangeException)
			{
				Validator.Set("NegativeIndexAccess", "caught");
			}

			try
			{
				int idx = 999;
				Debug.Log("Positive OOB: " + intArr[idx]);
			}
			catch (IndexOutOfRangeException)
			{
				Validator.Set("PositiveIndexAccess", "caught");
			}

			// stfld on null
			try
			{
				testObj.x = 5.0f;
				Validator.Set("StfldNullRef", "didn't throw");
			}
			catch (NullReferenceException)
			{
				Validator.Set("StfldNullRef", "caught");
			}

			// ldflda on null
			try
			{
				ReadFloat(ref testObj.x);
				Validator.Set("LdfldaNullRef", "didn't throw");
			}
			catch (NullReferenceException)
			{
				Validator.Set("LdfldaNullRef", "caught");
			}

			// ldind/stind for byte (ldind.u1 / stind.i1)
			byte myByte = 200;
			ReadByte(ref myByte);
			WriteByte(ref myByte, 42);
			Validator.Set("New myByte", myByte.ToString() );

			// ldind/stind for short (ldind.i2 / stind.i2)
			short myShort = 1234;
			ReadShort(ref myShort);
			WriteShort(ref myShort, 99);
			Validator.Set("New myShort", myShort.ToString() );

			// ldind/stind for long (ldind.i8 / stind.i8)
			long myLong = 9876543210L;
			ReadLong(ref myLong);
			WriteLong(ref myLong, 42L);
			Validator.Set("New myLong", myLong.ToString() );

			// ldind/stind for double (ldind.r8 / stind.r8)
			double myDouble = 3.14;
			ReadDouble(ref myDouble);
			WriteDouble(ref myDouble, 2.718);
			Validator.Set("New myDouble", myDouble.ToString() );

			// ldind/stind for ref type (ldind.ref / stind.ref)
			string myString = "hello";
			ReadString(ref myString);
			WriteString(ref myString, "world");
			Validator.Set("New myString", myString );

			// ldind.ref / stind.ref for Cilboxable type
			TestCilboxBehaviour2 myRef = behaviour2;
			ReadCilboxable(ref myRef);
			WriteCilboxable(ref myRef, behaviour2);
			Validator.Set("RefCilboxable Same", (myRef == behaviour2).ToString() );

			// NativeHandle through native method ref modification
			Vector3 nativeRefVec = new Vector3(10, 20, 30);
			TestUtil.Increment(ref nativeRefVec.x);
			Validator.Set("NativeRefMethodCall", nativeRefVec.x.ToString() );

			Vector3 checkThis = new Vector3(1, 2, 3);
			Validator.Set("Vector3CheckThis", checkThis.x == checkThis[0] && checkThis.y == checkThis[1] && checkThis.z == checkThis[2] ? "OK" : "Fail" );

			Validator.Set("MyEnum.Value1", MyEnum.Value1.ToString() );
			Validator.Set("MyEnum.Value2", MyEnum.Value2.ToString() );
			Validator.Set("MyEnum.Value3", MyEnum.Value3.ToString() );
			Validator.Set("(int)MyEnum.Value1", ((int)MyEnum.Value1).ToString() );
			Validator.Set("(int)MyEnum.Value2", ((int)MyEnum.Value2).ToString() );
			Validator.Set("(int)MyEnum.Value3", ((int)MyEnum.Value3).ToString() );
			Validator.Set("MyEnum Field", myEnumField.ToString() );
			Validator.Set("(int)MyEnum Field", ((int)myEnumField).ToString() );
			Validator.Set("MyEnum Field == Value1", (myEnumField == MyEnum.Value1).ToString() );
			Validator.Set("MyEnum Field == Value2", (myEnumField == MyEnum.Value2).ToString() );
			Validator.Set("MyEnum Field == Value3", (myEnumField == MyEnum.Value3).ToString() );
			Validator.Set("(int)MyEnum Field == Value1", ((int)myEnumField == (int)MyEnum.Value1).ToString() );
			Validator.Set("(int)MyEnum Field == Value2", ((int)myEnumField == (int)MyEnum.Value2).ToString() );
			Validator.Set("(int)MyEnum Field == Value3", ((int)myEnumField == (int)MyEnum.Value3).ToString() );

			Validator.Set("TestEnum.FirstValue", TestEnum.FirstValue.ToString() );
			Validator.Set("TestEnum.SecondValue", TestEnum.SecondValue.ToString() );
			Validator.Set("TestEnum.ThirdValue", TestEnum.ThirdValue.ToString() );
			Validator.Set("(int)TestEnum.FirstValue", ((int)TestEnum.FirstValue).ToString() );
			Validator.Set("(int)TestEnum.SecondValue", ((int)TestEnum.SecondValue).ToString() );
			Validator.Set("(int)TestEnum.ThirdValue", ((int)TestEnum.ThirdValue).ToString() );
			Validator.Set("TestEnum Field", testEnumField.ToString() );
			Validator.Set("(int)TestEnum Field", ((int)testEnumField).ToString() );
			Validator.Set("TestEnum Field == FirstValue", (testEnumField == TestEnum.FirstValue).ToString() );
			Validator.Set("TestEnum Field == SecondValue", (testEnumField == TestEnum.SecondValue).ToString() );
			Validator.Set("TestEnum Field == ThirdValue", (testEnumField == TestEnum.ThirdValue).ToString() );
			Validator.Set("(int)TestEnum Field == FirstValue", ((int)testEnumField == (int)TestEnum.FirstValue).ToString() );
			Validator.Set("(int)TestEnum Field == SecondValue", ((int)testEnumField == (int)TestEnum.SecondValue).ToString() );
			Validator.Set("(int)TestEnum Field == ThirdValue", ((int)testEnumField == (int)TestEnum.ThirdValue).ToString() );
			Validator.Set("TestEnumNativeEqualsFirstValue", TestUtil.TestEnumNativeEquals(testEnumField, TestEnum.FirstValue).ToString() );
			Validator.Set("TestEnumNativeEqualsSecondValue", TestUtil.TestEnumNativeEquals(testEnumField, TestEnum.SecondValue).ToString() );
			Validator.Set("TestEnumNativeEqualsThirdValue", TestUtil.TestEnumNativeEquals(testEnumField, TestEnum.ThirdValue).ToString() );

			Validator.Set("TestState.Stopped", TestState.Stopped.ToString() );
			Validator.Set("TestState.Playing", TestState.Playing.ToString() );
			Validator.Set("TestState.Paused", TestState.Paused.ToString() );
			Validator.Set("(byte)TestState.Stopped", ((byte)TestState.Stopped).ToString() );
			Validator.Set("(byte)TestState.Playing", ((byte)TestState.Playing).ToString() );
			Validator.Set("(byte)TestState.Paused", ((byte)TestState.Paused).ToString() );
			Validator.Set("TestState Field", testStateField.ToString() );
			Validator.Set("(byte)TestState Field", ((byte)testStateField).ToString() );
			Validator.Set("TestState Field == Stopped", (testStateField == TestState.Stopped).ToString() );
			Validator.Set("TestState Field == Playing", (testStateField == TestState.Playing).ToString() );
			Validator.Set("TestState Field == Paused", (testStateField == TestState.Paused).ToString() );
			Validator.Set("(byte)TestState Field == Stopped", ((byte)testStateField == (byte)TestState.Stopped).ToString() );
			Validator.Set("(byte)TestState Field == Playing", ((byte)testStateField == (byte)TestState.Playing).ToString() );
			Validator.Set("(byte)TestState Field == Paused", ((byte)testStateField == (byte)TestState.Paused).ToString() );

			Validator.Set("TestPayload Field Score", testPayloadField.Score.ToString() );
			Validator.Set("TestPayload Field Lives", testPayloadField.Lives.ToString() );
			TestPayload payload = new TestPayload { Score = 77, Lives = 2 };
			Validator.Set("TestPayload Local Score", payload.Score.ToString() );
			Validator.Set("TestPayload Local Lives", payload.Lives.ToString() );
			payload.Score += 5;
			payload.Lives += 1;
			Validator.Set("TestPayload Local Score Mutated", payload.Score.ToString() );
			Validator.Set("TestPayload Local Lives Mutated", payload.Lives.ToString() );

			MyEnumMethod(MyEnum.Value1);
			MyEnumMethod(myEnumField);
			TestEnumMethod(TestEnum.FirstValue);
			TestEnumMethod(testEnumField);
			TestStateMethod(TestState.Stopped);
			TestStateMethod(testStateField);
			TestPayloadMethod(payload);

			MyEnum[] myEnumArr = new MyEnum[] { MyEnum.Value1, MyEnum.Value2, MyEnum.Value3 };
			for (int j = 0; j < myEnumArr.Length; j++) {
				Validator.Set("MyEnum Array " + j, myEnumArr[j].ToString() );
				Validator.Set("MyEnum Array int value " + j, ((int)myEnumArr[j]).ToString() );
			}

			TestEnum[] enumArr = new TestEnum[] { TestEnum.FirstValue, TestEnum.SecondValue, TestEnum.ThirdValue };
			for (int j = 0; j < enumArr.Length; j++)
			{
				Validator.Set("TestEnum Array " + j, enumArr[j].ToString() );
				Validator.Set("TestEnum Array int value " + j, ((int)enumArr[j]).ToString() );
			}

			TestState[] testStateArr = new TestState[] { TestState.Stopped, TestState.Playing, TestState.Paused };
			for (int j = 0; j < testStateArr.Length; j++)
			{
				Validator.Set("TestState Array " + j, testStateArr[j].ToString() );
				Validator.Set("TestState Array byte value " + j, ((byte)testStateArr[j]).ToString() );
			}

			TestPayload[] payloadArr = new TestPayload[]
			{
				new TestPayload { Score = 10, Lives = 1 },
				new TestPayload { Score = 20, Lives = 3 },
			};
			for (int j = 0; j < payloadArr.Length; j++)
			{
				Validator.Set("TestPayload Array Score " + j, payloadArr[j].Score.ToString() );
				Validator.Set("TestPayload Array Lives " + j, payloadArr[j].Lives.ToString() );
			}
			ushort[] ushortAssigned = new ushort[3];
			ushortAssigned[0] = 7;
			ushortAssigned[1] = 1234;
			ushortAssigned[2] = 65535;
			Validator.Set("Ushort Array Assigned Length", ushortAssigned.Length.ToString() );
			Validator.Set("Ushort Array Assigned 0", ushortAssigned[0].ToString() );
			Validator.Set("Ushort Array Assigned 1", ushortAssigned[1].ToString() );
			Validator.Set("Ushort Array Assigned 2", ushortAssigned[2].ToString() );

			ushort[] ushortWithData = new ushort[] { 42, 512, 60000 };
			Validator.Set("Ushort Array With Data Length", ushortWithData.Length.ToString() );
			Validator.Set("Ushort Array With Data 0", ushortWithData[0].ToString() );
			Validator.Set("Ushort Array With Data 1", ushortWithData[1].ToString() );
			Validator.Set("Ushort Array With Data 2", ushortWithData[2].ToString() );

			uint[] uintAssigned = new uint[3];
			uintAssigned[0] = 7u;
			uintAssigned[1] = 1234u;
			uintAssigned[2] = 4000000000u;
			Validator.Set("Uint Array Assigned Length", uintAssigned.Length.ToString() );
			Validator.Set("Uint Array Assigned 0", uintAssigned[0].ToString() );
			Validator.Set("Uint Array Assigned 1", uintAssigned[1].ToString() );
			Validator.Set("Uint Array Assigned 2", uintAssigned[2].ToString() );

			uint[] uintWithData = new uint[] { 42u, 512u, 3000000000u };
			Validator.Set("Uint Array With Data Length", uintWithData.Length.ToString() );
			Validator.Set("Uint Array With Data 0", uintWithData[0].ToString() );
			Validator.Set("Uint Array With Data 1", uintWithData[1].ToString() );
			Validator.Set("Uint Array With Data 2", uintWithData[2].ToString() );

			nint[] nintAssigned = new nint[3];
			nintAssigned[0] = (nint)7;
			nintAssigned[1] = (nint)1234;
			nintAssigned[2] = (nint)56789;
			Validator.Set("Nint Array Assigned Length", nintAssigned.Length.ToString() );
			Validator.Set("Nint Array Assigned 0", nintAssigned[0].ToString() );
			Validator.Set("Nint Array Assigned 1", nintAssigned[1].ToString() );
			Validator.Set("Nint Array Assigned 2", nintAssigned[2].ToString() );

			nint[] nintWithData = new nint[] { (nint)42, (nint)512, (nint)9000 };
			Validator.Set("Nint Array With Data Length", nintWithData.Length.ToString() );
			Validator.Set("Nint Array With Data 0", nintWithData[0].ToString() );
			Validator.Set("Nint Array With Data 1", nintWithData[1].ToString() );
			Validator.Set("Nint Array With Data 2", nintWithData[2].ToString() );

			byte[] byteAssigned = new byte[3];
			byteAssigned[0] = 7;
			byteAssigned[1] = 123;
			byteAssigned[2] = 255;
			Validator.Set("Byte Array Assigned Length", byteAssigned.Length.ToString() );
			Validator.Set("Byte Array Assigned 0", byteAssigned[0].ToString() );
			Validator.Set("Byte Array Assigned 1", byteAssigned[1].ToString() );
			Validator.Set("Byte Array Assigned 2", byteAssigned[2].ToString() );

			byte[] byteWithData = new byte[] { 42, 64, 255 };
			Validator.Set("Byte Array With Data Length", byteWithData.Length.ToString() );
			Validator.Set("Byte Array With Data 0", byteWithData[0].ToString() );
			Validator.Set("Byte Array With Data 1", byteWithData[1].ToString() );
			Validator.Set("Byte Array With Data 2", byteWithData[2].ToString() );

			float[] floatAssigned = new float[3];
			floatAssigned[0] = 1.5f;
			floatAssigned[1] = 2.25f;
			floatAssigned[2] = 3.75f;
			Validator.Set("Float Array Assigned Length", floatAssigned.Length.ToString() );
			Validator.Set("Float Array Assigned 0", floatAssigned[0].ToString() );
			Validator.Set("Float Array Assigned 1", floatAssigned[1].ToString() );
			Validator.Set("Float Array Assigned 2", floatAssigned[2].ToString() );

			float[] floatWithData = new float[] { 4.5f, 6.25f, 8.75f };
			Validator.Set("Float Array With Data Length", floatWithData.Length.ToString() );
			Validator.Set("Float Array With Data 0", floatWithData[0].ToString() );
			Validator.Set("Float Array With Data 1", floatWithData[1].ToString() );
			Validator.Set("Float Array With Data 2", floatWithData[2].ToString() );

			double[] doubleAssigned = new double[3];
			doubleAssigned[0] = 1.5;
			doubleAssigned[1] = 2.25;
			doubleAssigned[2] = 3.75;
			Validator.Set("Double Array Assigned Length", doubleAssigned.Length.ToString() );
			Validator.Set("Double Array Assigned 0", doubleAssigned[0].ToString() );
			Validator.Set("Double Array Assigned 1", doubleAssigned[1].ToString() );
			Validator.Set("Double Array Assigned 2", doubleAssigned[2].ToString() );

			double[] doubleWithData = new double[] { 4.5, 6.25, 8.75 };
			Validator.Set("Double Array With Data Length", doubleWithData.Length.ToString() );
			Validator.Set("Double Array With Data 0", doubleWithData[0].ToString() );
			Validator.Set("Double Array With Data 1", doubleWithData[1].ToString() );
			Validator.Set("Double Array With Data 2", doubleWithData[2].ToString() );

			object[] objectAssigned = new object[3];
			objectAssigned[0] = "alpha";
			objectAssigned[1] = 42;
			objectAssigned[2] = "gamma";
			Validator.Set("Object Array Assigned Length", objectAssigned.Length.ToString() );
			Validator.Set("Object Array Assigned 0", objectAssigned[0].ToString() );
			Validator.Set("Object Array Assigned 1", objectAssigned[1].ToString() );
			Validator.Set("Object Array Assigned 2", objectAssigned[2].ToString() );

			object[] objectWithData = new object[] { "beta", 64, "delta" };
			Validator.Set("Object Array With Data Length", objectWithData.Length.ToString() );
			Validator.Set("Object Array With Data 0", objectWithData[0].ToString() );
			Validator.Set("Object Array With Data 1", objectWithData[1].ToString() );
			Validator.Set("Object Array With Data 2", objectWithData[2].ToString() );

			object boxedMyEnum = MyEnum.Value2;
			MyEnum castMyEnum = (MyEnum)boxedMyEnum;
			Validator.Set("Boxed MyEnum", castMyEnum.ToString() );

			object boxedTestEnum = testEnumField;
			TestEnum castTestEnum = (TestEnum)boxedTestEnum;
			Validator.Set("Boxed TestEnum", castTestEnum.ToString() );

			Validator.Set("NativeStaticFloat", TestUtil.StaticFloat.ToString());
			TestUtil.StaticFloat *= 2;
			Validator.Set("NativeStaticFloat x2", TestUtil.StaticFloat.ToString());

			ReadFloat(ref TestUtil.StaticFloat);
			WriteFloat(ref TestUtil.StaticFloat, 99.0f);
			Validator.Set("NativeStaticFloat ref written", TestUtil.StaticFloat.ToString());
			ReadInt(ref iprivatestatic);

			TestUtil.GetOutVec3(out Vector3 outVec);
			Validator.Set("NativeOutVec3", outVec.ToString() );
			TestOutVec3(out Vector3 outVec2);
			Validator.Set("CilOutVec3", outVec2.ToString() );
			TestUtil.GetOutInt(out int outInt);
			Validator.Set("NativeOutInt", outInt.ToString() );
			TestOutInt(out int outInt2);
			Validator.Set("CilOutInt", outInt2.ToString() );
			Vector3 alreadyInit = new Vector3(5, 5, 5);
			TestUtil.GetOutVec3(out alreadyInit);
			Validator.Set("NativeOutVec3AlreadyInit", alreadyInit.ToString() );

			behaviour2.Behaviour2Test();
			myBehaviour3Arr = new TestCilboxBehaviour3[] { new TestCilboxBehaviour3(123), new TestCilboxBehaviour3(456)};
			Validator.Set("myBehaviour3Arr Length", myBehaviour3Arr.Length.ToString());
			Validator.Set("myBehaviour3Arr 0", myBehaviour3Arr[0].number.ToString());
			Validator.Set("myBehaviour3Arr 1", myBehaviour3Arr[1].number.ToString());
			myBehaviour3Arr[1].number = 789;
			Validator.Set("myBehaviour3Arr 1 changed", myBehaviour3Arr[1].number.ToString());

			try
			{
				Validator.Set("ThrowFromOtherBehaviour1", "try");
				behaviour2.ThrowExceptionTest();
			}
			catch (Exception)
			{
				Validator.Set("ThrowFromOtherBehaviour1", "caught");
			}

			try
			{
				Validator.Set("ThrowFromOtherBehaviour2", "try");
				behaviour2.ThrowNativeExceptionTest();
			}
			catch (IndexOutOfRangeException)
			{
				Validator.Set("ThrowFromOtherBehaviour2", "caught");
			}
			finally
			{
				Validator.Set("ThrowFromOtherBehaviour2Finally", "finally");
			}

			try
			{
				Validator.Set("ThrowFromOtherConstructor", "try");
				TestCilboxExceptConstructor exceptConstructor = new TestCilboxExceptConstructor();
			}
			catch (Exception)
			{
				Validator.Set("ThrowFromOtherConstructor", "caught");
			}
		}

		public void Update()
		{
			Validator.Set( "Update", "called" );
			Validator.Set( "Overtime", "timed out" );
			double result = 1.3;
			for( int i = 0; i < 10000000; i++ ) result = System.Math.Sin( result ) * 10.0;
			Validator.Set( "Throwaway", result.ToString() );
			Validator.Set( "Overtime", "did not timed out" );
		}


		public void FixedUpdate()
		{
			Validator.Set( "Execution after timeout", "enabled" );
			Validator.Set( "Manual Recover After Timeout", "recovered" );
			Validator.Set( "FixedUpdate", "called" );
		}

		public void ReadInt(ref int field)
		{
			int current = field;
			Validator.AddCount("ReadInt");
			Validator.Set("ReadInt_" + Validator.GetCount("ReadInt"), current.ToString() );
		}

		public void WriteInt(ref int field, int value)
		{
			field = value;
			Validator.AddCount("WriteInt");
			Validator.Set("WriteInt_" + Validator.GetCount("WriteInt"), value.ToString() );
		}

		public void ReadFloat(ref float field)
		{
			float current = field;
			Validator.AddCount("ReadFloat");
			Validator.Set("ReadFloat_" + Validator.GetCount("ReadFloat"), current.ToString() );
		}

		public void WriteFloat(ref float field, float value)
		{
			field = value;
			Validator.AddCount("WriteFloat");
			Validator.Set("WriteFloat_" + Validator.GetCount("WriteFloat"), value.ToString() );
		}

		public void ReadByte(ref byte field)
		{
			byte current = field;
			Validator.AddCount("ReadByte");
			Validator.Set("ReadByte_" + Validator.GetCount("ReadByte"), current.ToString() );
		}

		public void WriteByte(ref byte field, byte value)
		{
			field = value;
			Validator.AddCount("WriteByte");
			Validator.Set("WriteByte_" + Validator.GetCount("WriteByte"), value.ToString() );
		}

		public void ReadShort(ref short field)
		{
			short current = field;
			Validator.AddCount("ReadShort");
			Validator.Set("ReadShort_" + Validator.GetCount("ReadShort"), current.ToString() );
		}

		public void WriteShort(ref short field, short value)
		{
			field = value;
			Validator.AddCount("WriteShort");
			Validator.Set("WriteShort_" + Validator.GetCount("WriteShort"), value.ToString() );
		}

		public void ReadLong(ref long field)
		{
			long current = field;
			Validator.AddCount("ReadLong");
			Validator.Set("ReadLong_" + Validator.GetCount("ReadLong"), current.ToString() );
		}

		public void WriteLong(ref long field, long value)
		{
			field = value;
			Validator.AddCount("WriteLong");
			Validator.Set("WriteLong_" + Validator.GetCount("WriteLong"), value.ToString() );
		}

		public void ReadDouble(ref double field)
		{
			double current = field;
			Validator.AddCount("ReadDouble");
			Validator.Set("ReadDouble_" + Validator.GetCount("ReadDouble"), current.ToString() );
		}

		public void WriteDouble(ref double field, double value)
		{
			field = value;
			Validator.AddCount("WriteDouble");
			Validator.Set("WriteDouble_" + Validator.GetCount("WriteDouble"), value.ToString() );
		}

		public void ReadString(ref string field)
		{
			string current = field;
			Validator.AddCount("ReadString");
			Validator.Set("ReadString_" + Validator.GetCount("ReadString"), current );
		}

		public void WriteString(ref string field, string value)
		{
			field = value;
			Validator.AddCount("WriteString");
			Validator.Set("WriteString_" + Validator.GetCount("WriteString"), value );
		}

		public void ReadCilboxable(ref TestCilboxBehaviour2 field)
		{
			TestCilboxBehaviour2 current = field;
			Validator.Set("ReadCilboxable", current.pubsettee.ToString() );
		}

		public void WriteCilboxable(ref TestCilboxBehaviour2 field, TestCilboxBehaviour2 value)
		{
			field = value;
			Validator.Set("WriteCilboxable", value.pubsettee.ToString() );
		}

		public void MyEnumMethod(MyEnum e)
		{
			Validator.AddCount("MyEnumMethod");
			Validator.Set("MyEnumMethod_" + Validator.GetCount("MyEnumMethod"), e.ToString() );
		}

		public void TestEnumMethod(TestEnum e)
		{
			Validator.AddCount("TestEnumMethod");
			Validator.Set("TestEnumMethod_" + Validator.GetCount("TestEnumMethod"), e.ToString() );
		}

		private void TestStateMethod(TestState state)
		{
			Validator.AddCount("TestStateMethod");
			Validator.Set("TestStateMethod_" + Validator.GetCount("TestStateMethod"), state.ToString() );
		}

		private void TestPayloadMethod(TestPayload payload)
		{
			Validator.Set("TestPayloadMethod Score", payload.Score.ToString() );
			Validator.Set("TestPayloadMethod Lives", payload.Lives.ToString() );
		}

		public void TestOutVec3(out Vector3 vec)
		{
			vec = new Vector3(1, 2, 3);
		}

		public void TestOutInt(out int i)
		{
			i = 22;
		}
	}


	[Cilboxable]
	public class TestCilboxBehaviour2 : MonoBehaviour
	{
		public int pubsettee = 35254;
		public void Behaviour2Test()
		{
			Validator.Set( "Method Called On Peer", "OK" );
			Validator.Set( "Public Field Change In Editor", pubsettee.ToString() ); // Should not be 35254
		}

		public void ThrowExceptionTest()
		{
			throw new Exception("Test Exception from Behaviour2");
		}

		public int ThrowNativeExceptionTest()
		{
			int[] emptyArr = new int[0];
			return emptyArr[0];
		}
	}

	[Cilboxable]
	public class TestCilboxBehaviour3
	{
		public TestCilboxBehaviour3(int value) {this.number = value;}
		public int number = 123;
	}

	[Cilboxable]
	public class TestCilboxExceptConstructor
	{
		public TestCilboxExceptConstructor() { throw new Exception("Constructor Exception"); }
	}
	[Cilboxable]
	public class PerfUtility
	{
		public static long StopwatchToUs(System.Diagnostics.Stopwatch sw)
		{
			long us = (long)sw.Elapsed.TotalMicroseconds;
			if (sw.ElapsedTicks > 0 && us == 0) us = 1;
			return us;
		}
	}
	[Cilboxable]
	public class PerfPeerBehaviour : MonoBehaviour
	{
		public const string ClassName = "TestCilbox.PerfPeerBehaviour";
		private const int InnerIterations = 64;
		private long totalUs = 0;
		private System.Diagnostics.Stopwatch runningTimer;


		public void BeginPerfTiming()
		{
			runningTimer = System.Diagnostics.Stopwatch.StartNew();
		}

		public float ComputeKernel(float seed, int rounds)
		{
			float value = seed;
			for (int i = 0; i < rounds; i++)
			{
				float scaled = value + i * 0.03125f;
				value = MathF.Sin(scaled) * 0.7f + MathF.Cos(scaled * 1.7f) * 0.3f;
			}
			for (int i = 0; i < InnerIterations; i++)
			{
				float scaled = value + i * 0.015625f;
				value += MathF.Sin(scaled) * MathF.Cos(scaled * 0.5f);
			}
			return value;
		}

		public void CommitPerfMetrics()
		{
			if (runningTimer != null)
			{
				runningTimer.Stop();
				totalUs += PerfUtility.StopwatchToUs(runningTimer);
				runningTimer = null;
			}
			Validator.Set($"Perf.{ClassName}.TotalUs", totalUs.ToString());
		}
	}

	[Cilboxable]
	public class PerfRootBehaviour : MonoBehaviour
	{
		public const string ClassName = "TestCilbox.PerfRootBehaviour";
		private const float TwoPi = 6.283185307179586f;
		private const int RecursiveDepth = 12;
		private const int DftSize = 96;
		private const int DftRepeats = 3;
		private const int TrigIterations = 350000;
		private const int MatrixSize = 20;
		private const int MatrixRepeats = 8;
		private const int PeerCallCount = 2500;

		public PerfPeerBehaviour peer;

		public void Start()
		{
			System.Diagnostics.Stopwatch totalSw = System.Diagnostics.Stopwatch.StartNew();

			long recursiveUs = RunRecursiveTask();
			long dftUs = RunDftTask();
			long trigUs = RunTrigTask();
			long matrixUs = RunMatrixTask();
			long peerUs = RunPeerTask();

			totalSw.Stop();
			Validator.Set($"Perf.{ClassName}.RecursiveUs", recursiveUs.ToString());
			Validator.Set($"Perf.{ClassName}.FourierUs", dftUs.ToString());
			Validator.Set($"Perf.{ClassName}.TrigUs", trigUs.ToString());
			Validator.Set($"Perf.{ClassName}.MatrixUs", matrixUs.ToString());
			Validator.Set($"Perf.{ClassName}.PeerCallsUs", peerUs.ToString());
			Validator.Set($"Perf.{ClassName}.TotalUs", PerfUtility.StopwatchToUs(totalSw).ToString());
		}

		private long RunRecursiveTask()
		{
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			int recursiveResult = RecursivePerf(RecursiveDepth);
			sw.Stop();
			Validator.Set($"Perf.{ClassName}.RecursiveResult", recursiveResult.ToString());
			return PerfUtility.StopwatchToUs(sw);
		}

		private int RecursivePerf(int depth)
		{
			if (depth <= 0) return 1;
			return RecursivePerf(depth - 1) + RecursivePerf(depth - 1);
		}

		private long RunDftTask()
		{
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			float checksum = RunDiscreteFourier(DftSize, DftRepeats);
			sw.Stop();
			Validator.Set($"Perf.{ClassName}.FourierChecksum", checksum.ToString());
			return PerfUtility.StopwatchToUs(sw);
		}

		private float RunDiscreteFourier(int sampleCount, int repeats)
		{
			float[] signal = new float[sampleCount];
			float[] real = new float[sampleCount];
			float[] imag = new float[sampleCount];

			for (int i = 0; i < sampleCount; i++)
			{
				float x = i * 0.09f;
				signal[i] = MathF.Sin(x) + MathF.Cos(x * 0.7f);
			}

			float checksum = 0.0f;
			for (int rep = 0; rep < repeats; rep++)
			{
				for (int k = 0; k < sampleCount; k++)
				{
					float sumReal = 0.0f;
					float sumImag = 0.0f;
					for (int n = 0; n < sampleCount; n++)
					{
						float angle = TwoPi * k * n / sampleCount;
						float sample = signal[n];
						sumReal += sample * MathF.Cos(angle);
						sumImag -= sample * MathF.Sin(angle);
					}
					real[k] = sumReal;
					imag[k] = sumImag;
				}

				for (int i = 0; i < sampleCount; i++)
				{
					checksum += MathF.Abs(real[i]) + MathF.Abs(imag[i]);
					signal[i] = real[i] * 0.001f + imag[i] * 0.0005f;
				}
			}

			return checksum;
		}

		private long RunTrigTask()
		{
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			float trigAccum = 0.0f;
			for (int i = 0; i < TrigIterations; i++)
			{
				float x = i * 0.0025f;
				trigAccum += MathF.Sin(x) * MathF.Cos(x * 1.3f) + MathF.Sin(x * 0.2f);
			}
			sw.Stop();
			Validator.Set($"Perf.{ClassName}.TrigAccum", trigAccum.ToString());
			return PerfUtility.StopwatchToUs(sw);
		}

		private long RunMatrixTask()
		{
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			float checksum = MatrixMultiplyWork(MatrixSize, MatrixRepeats);
			sw.Stop();
			Validator.Set($"Perf.{ClassName}.MatrixChecksum", checksum.ToString());
			return PerfUtility.StopwatchToUs(sw);
		}

		private float MatrixMultiplyWork(int size, int repeats)
		{
			int len = size * size;
			float[] a = new float[len];
			float[] b = new float[len];
			float[] c = new float[len];
			float[] temp;

			for (int i = 0; i < len; i++)
			{
				a[i] = 0.1f + i * 0.001f;
				b[i] = 0.2f + i * 0.0007f;
			}

			for (int rep = 0; rep < repeats; rep++)
			{
				for (int row = 0; row < size; row++)
				{
					int rowStart = row * size;
					for (int col = 0; col < size; col++)
					{
						float sum = 0.0f;
						for (int k = 0; k < size; k++)
						{
							sum += a[rowStart + k] * b[k * size + col];
						}
						c[rowStart + col] = sum;
					}
				}

				temp = a;
				a = b;
				b = c;
				c = temp;
			}

			return b[0] + b[len - 1];
		}

		private long RunPeerTask()
		{
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			float value = 0.125f;
			if (peer != null)
			{
				peer.BeginPerfTiming();
				for (int i = 0; i < PeerCallCount; i++)
				{
					value = peer.ComputeKernel(value + i * 0.0001f, 6);
				}
				peer.CommitPerfMetrics();
			}
			sw.Stop();
			Validator.Set($"Perf.{ClassName}.PeerChecksum", value.ToString());
			return PerfUtility.StopwatchToUs(sw);
		}
	}
}
