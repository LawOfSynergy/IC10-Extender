using Assets.Scripts.Objects.Electrical;
using System;
using System.Linq;

namespace IC10_Extender
{
    public class ExtendedPCException : ProgrammableChipException
    {
        public static readonly byte EXCEPTION_TYPE_CUSTOM = (byte)(Enum.GetValues(typeof(ICExceptionType)).Cast<byte>().Max() + 1);

        public new string Message { get; private set; }

        public ExtendedPCException(ushort lineNumber, string message) : base((ICExceptionType)EXCEPTION_TYPE_CUSTOM, lineNumber)
        {
            Message = message;
        }

        public ExtendedPCException(int lineNumber, string message) : base((ICExceptionType)EXCEPTION_TYPE_CUSTOM, lineNumber)
        {
            Message = message;
        }

        public override string ToString()
        {
            return $"Error <color={Colors.ERROR}>{GetType().Name} at line {LineNumber}: {Message}</color>";
        }
    }
}
