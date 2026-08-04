using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Variables;

namespace IC10_Extender.Operations
{
    public class LegacyAsIC10OpWrapper : Operation
    {
        public readonly ProgrammableChip._Operation op;

        public LegacyAsIC10OpWrapper(CompileContext ctx, Line line, BoundSignature variables, ChipWrapper chip, ProgrammableChip._Operation op) : base(ctx, line, variables, chip)
        {
            this.op = op;
        }

        public override int Execute(int index)
        {
            return op.Execute(index);
        }
    }
}
