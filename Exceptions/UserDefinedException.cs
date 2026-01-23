namespace IC10_Extender.Exceptions
{
    public class UserDefinedException : ExtendedPCException
    {
        public UserDefinedException(ushort lineNumber, int errorCode) : base(lineNumber, $"{errorCode}")
        {
        }

        public UserDefinedException(int lineNumber, int errorCode) : base(lineNumber, $"{errorCode}")
        {
        }
    }
}
