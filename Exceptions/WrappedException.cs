using System;

namespace IC10_Extender.Exceptions
{
    public class WrappedException : ExtendedPCException
    {
        public Exception Cause { get; private set; }

        public WrappedException(ushort lineNumber, Exception ex) : base(lineNumber, $"Caused by {ex.GetType().Name} at {ex.StackTrace.Split('\n')[0]}")
        {
            Cause = ex;
        }

        public WrappedException(int lineNumber, Exception ex) : base(lineNumber, $"Caused by {ex.GetType().Name} at {ex.StackTrace.Split('\n')[0]}")
        {
            Cause = ex;
        }
    }
}
