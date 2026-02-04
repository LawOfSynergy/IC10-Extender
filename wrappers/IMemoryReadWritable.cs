using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;

namespace IC10_Extender.Wrappers
{
    public interface IMemoryReadWritable : IMemoryReadable, IMemoryWritable
    {
    }

    public class MemoryReadWritableWrapper : IMemoryReadWritable
    {
        private readonly IMemoryReadable readable;
        private readonly IMemoryWritable writable;
        public MemoryReadWritableWrapper(ILogicable logicable, int lineNumber)
        {
            readable = logicable as IMemoryReadable ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, lineNumber);
            writable = logicable as IMemoryWritable ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, lineNumber);
        }

        public void ClearMemory()
        {
            writable.ClearMemory();
        }

        public int GetStackSize()
        {
            return writable.GetStackSize();
        }

        public void WriteMemory(int address, double value)
        {
            writable.WriteMemory(address, value);
        }

        public double ReadMemory(int address)
        {
            return readable.ReadMemory(address);
        }
    }
}
