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

		public enum MyEnum
		{
			Value1,
			Value2,
			Value3 = 30,
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

			MyEnumMethod(MyEnum.Value1);
			MyEnumMethod(myEnumField);
			TestEnumMethod(TestEnum.FirstValue);
			TestEnumMethod(testEnumField);

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

			behaviour2.Behaviour2Test();
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
	}

	[Cilboxable]
	public class TestCilboxBehaviour3 : MonoBehaviour
	{
	}
}

