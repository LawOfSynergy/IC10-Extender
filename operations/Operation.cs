using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Wrappers;

namespace IC10_Extender.Operations
{
    //had to separate the Operation from _Operation with a wrapper, because dependent projects
    //did not have visibility into the _Operation class, and so would not correctly register
    //that the Execute method was already overridden.

    //create a publicly accessable class for this, so that add-ons don't have to publicize the core dll
    public abstract class Operation
    {
        public readonly ChipWrapper Chip;
        public readonly int LineNumber;

        protected Operation(ChipWrapper chip, int lineNumber)
        {
            Chip = chip;
            LineNumber = lineNumber;
        }

        public abstract int Execute(int index);

        public static implicit operator ProgrammableChip._Operation(Operation op) => op is ReverseWrapper wrap ? wrap.op : new OperationWrapper(op);
        public static implicit operator Operation(ProgrammableChip._Operation op) => op is OperationWrapper wrap ? wrap.op : new ReverseWrapper(op);

        public static Operation NoOp(ChipWrapper chip, int lineNumber)
        {
            return NoOp(chip.chip, lineNumber);
        }

        public static Operation NoOp(ProgrammableChip chip, int lineNumber)
        {
            return new OperationWrapper(new ProgrammableChip._NOOP_Operation(chip, lineNumber));
        }
    }
}
