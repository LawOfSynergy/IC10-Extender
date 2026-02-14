using IC10_Extender.Highlighters;

namespace IC10_Extender.Variables
{
    public struct Binding
    {
        public readonly ChipWrapper Chip;
        public readonly int LineNumber;
        public readonly string OriginalToken;
        public readonly StyledLine Line;
        public string Token;
        
        public Binding(ChipWrapper chip, int lineNumber, StyledLine line, string token)
        {
            Chip = chip;
            LineNumber = lineNumber;
            Line = line;
            OriginalToken = token;
            Token = token;
        }
    }
}
