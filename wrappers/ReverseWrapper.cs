using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Operations;

namespace IC10_Extender.Wrappers
{
    public class ReverseWrapper : Operation
    {
        public readonly ProgrammableChip._Operation op;

        public ReverseWrapper(ProgrammableChip._Operation op) : base(op._Chip.Wrap(), op._LineNumber)
        {
            this.op = op;
        }

        public override int Execute(int index)
        {
            return op.Execute(index);
        }
    }
}
