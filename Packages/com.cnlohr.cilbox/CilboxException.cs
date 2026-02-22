using System;

namespace Cilbox
{
	public class CilboxException : Exception
	{
		public CilboxException(string msg) : base(msg)
		{
		}

		public CilboxException(string msg, Exception inner) : base(msg, inner)
		{
		}
	}

	public class CilboxInterpreterRuntimeException : CilboxException
	{
		public readonly string ClassName;
		public readonly string MethodName;
		public readonly int PC;

		public CilboxInterpreterRuntimeException(string msg, string className, string methodName, int pc)
			: base($"{msg} @ ({className}.{methodName}, PC: {pc})")
		{
			ClassName = className;
			MethodName = methodName;
			PC = pc;
		}

		public CilboxInterpreterRuntimeException(string msg, Exception inner, string className, string methodName,
			int pc)
			: base($"{msg} @ ({className}.{methodName}, PC: {pc})", inner)
		{
			ClassName = className;
			MethodName = methodName;
			PC = pc;
		}
	}

	public class CilboxInterpreterTimeoutException : CilboxInterpreterRuntimeException
	{
		public CilboxInterpreterTimeoutException(string msg, string className, string methodName, int pc)
			: base(msg, className, methodName, pc)
		{
		}
	}
}
