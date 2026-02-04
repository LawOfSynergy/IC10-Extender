using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Operations
{
    public class LegacyAsIC10OpWrapper : Operation
    {
        public readonly ProgrammableChip._Operation op;

        public LegacyAsIC10OpWrapper(ProgrammableChip._Operation op, Line line) : base(op._Chip.Wrap(), line)
        {
            this.op = op;
        }

        public override int Execute(int index)
        {
            return op.Execute(index);
        }
    }
}
