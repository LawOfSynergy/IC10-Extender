namespace IC10_Extender.Variables
{
    public struct Binding
    {
        public ChipWrapper chip;
        public int lineNumber;
        public string token;
        
        public Binding(ChipWrapper chip, int lineNumber, string token)
        {
            this.chip = chip;
            this.lineNumber = lineNumber;
            this.token = token;
        }
    }
}
