namespace IC10_Extender.Preprocessors
{
    public class NoOpProcessorOperation : PreprocessorOperation
    {
        public NoOpProcessorOperation(ChipWrapper chip) : base(chip) { }

        public override Line? ProcessLine(Line line)
        {
            return line; //do nothing
        }
    }
}
