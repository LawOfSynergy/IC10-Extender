using IC10_Extender.Variables;

namespace IC10_Extender.Operations
{
    //had to separate the Operation from _Operation with a wrapper, because dependent projects
    //did not have visibility into the _Operation class, and so would not correctly register
    //that the Execute method was already overridden.

    //create a publicly accessable class for this, so that add-ons don't have to publicize the core dll
    public abstract class Operation
    {
        public readonly CompileContext Context;
        public readonly int LineNumber;
        public readonly BoundSignature Variables;
        public readonly ChipWrapper Chip;

        public PreExecute PreExecute;
        public PostExecute PostExecute;

        protected Operation(CompileContext ctx, Line line, BoundSignature variables, ChipWrapper chip)
        {
            Context = ctx;
            LineNumber = line.LineNumber;
            Variables = variables;
            Chip = chip;
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
