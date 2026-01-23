using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Operations
{
    public class OperationWrapper : ProgrammableChip._Operation
    {
        public readonly Operation op;
        public OperationWrapper(Operation op) : base(op.Chip.chip, op.LineNumber)
        {
            this.op = op;
        }

        public override int Execute(int index)
        {
            return op.Execute(index);
        }
    }
}
