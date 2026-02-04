using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Operations
{
    internal class IC10AsLegacyOpWrapper : ProgrammableChip._Operation
    {
        public readonly Operation op;
        public IC10AsLegacyOpWrapper(Operation op) : base(op.Chip.chip, op.LineNumber)
        {
            this.op = op;
        }

        public override int Execute(int index)
        {
            return op.ExecuteLifecycle(index);
        }
    }
}
