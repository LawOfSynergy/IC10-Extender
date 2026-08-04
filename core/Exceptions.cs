using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlight;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    public class ExtendedPCException : ProgrammableChipException
    {
        public static readonly byte EXCEPTION_TYPE_CUSTOM = (byte)(Enum.GetValues(typeof(ICExceptionType)).Cast<byte>().Max() + 1);

        public new string Message { get; private set; }
        private readonly IStyledString styled;

        public ExtendedPCException(ICExceptionType type, int lineNumber, string message, Theme theme = null) : base(type, lineNumber)
        {
            theme = theme ?? Theme.Default;
            Message = message;
            styled = new HelpString(
                theme, 
                new Tuple<string, Style?>("Error ", null), 
                new Tuple<string, Style?>($"{GetType().Name} at line {LineNumber}: {Message}", theme.Error));
        }

        public ExtendedPCException(int lineNumber, string message, Theme theme = null) : this((ICExceptionType)EXCEPTION_TYPE_CUSTOM, lineNumber, message, theme) { }

        public override string ToString()
        {
            return styled.ToString(HelpString.VanillaFormat);
        }

        public IStyledString ToStyledString()
        {
            return styled;
        }
    }

    public class ArgumentCountException : ExtendedPCException
    {
        public ArgumentCountException(int lineNumber, string message, Theme theme = null)
            : base(ICExceptionType.IncorrectArgumentCount, lineNumber, message, theme) { }
    }

    public class WrappedException : ExtendedPCException
    {
        public Exception Cause { get; private set; }

        public WrappedException(int lineNumber, Exception ex, Theme theme = null) : base(lineNumber, $"Caused by {ex.GetType().Name} at {ex.StackTrace.Split('\n')[0]}", theme)
        {
            Cause = ex;
        }
    }

    public class LabelSharedLineException : ExtendedPCException
    {
        public LabelSharedLineException(int lineNumber, Theme theme = null) : base(lineNumber, "A single label must be the only statement on a line when LabelsCanShareLine is false", theme) { }
    }

    public class UserDefinedException : ExtendedPCException
    {
        public UserDefinedException(int lineNumber, int errorCode, Theme theme = null) : base(lineNumber, $"{errorCode}", theme)
        {
        }
    }
}
