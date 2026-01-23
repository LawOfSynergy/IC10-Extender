namespace IC10_Extender.Operations
{
    public class OpContext : Operation
    {
        public readonly Operation op;
        private PreExecute PreExecute;
        private PostExecute PostExecute;


        public OpContext(Operation op, Line line) : base(op.Chip, op.LineNumber)
        {
            this.op = op;

            PreExecute = line.PreExecute;
            PostExecute = line.PostExecute;
        }

        public override int Execute(int index)
        {
            var preExec = IC10Extender.PreExecute + Chip.PreExecute + PreExecute;
            preExec(this);
            var result = op.Execute(index);
            var postExec = IC10Extender.PostExecute + Chip.PostExecute + PostExecute;
            postExec(this, ref result);

            return result;
        }
    }
}
