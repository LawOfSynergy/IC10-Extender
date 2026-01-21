namespace IC10_Extender
{
    public class OpContext : Operation
    {
        private Operation op;
        private string raw;
        private PreExecute PreExecute;
        private PostExecute PostExecute;


        public OpContext(Operation op, Line line) : base(op.Chip, op.LineNumber)
        {
            this.op = op;
            raw = line.Raw;

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
