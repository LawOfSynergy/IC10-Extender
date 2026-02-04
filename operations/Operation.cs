using Assets.Scripts.Objects.Electrical;

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

        public PreExecute PreExecute;
        public PostExecute PostExecute;

        protected Operation(ChipWrapper chip, int lineNumber)
        {
            Chip = chip;
            LineNumber = lineNumber;
        }

        protected Operation(ChipWrapper chip, Line line) : this(chip, line.LineNumber)
        {
            PreExecute = line.PreExecute;
            PostExecute = line.PostExecute;
        }

        public abstract int Execute(int index);

        internal int ExecuteLifecycle(int index)
        {
            var preExec = IC10Extender.PreExecute + Chip.PreExecute + PreExecute;
            preExec(this);
            var result = Execute(index);
            var postExec = IC10Extender.PostExecute + Chip.PostExecute + PostExecute;
            postExec(this, ref result);

            return result;
        }
    }
}
