using IC10_Extender.Highlighters;

namespace IC10_Extender.Preprocessors
{
    public class CommentPreprocessor : Preprocessor
    {
        public override string SimpleName => "comment_preprocessor";

        public override string HelpEntryName => $"<color={Colors.COMMENT}>#</color>";

        public override string HelpEntryDescription => "any following characters on this line are ignored";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public override SyntaxHighlighter Highlighter()
        {
            return new CommentHighlighter();
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                line.Raw = line.Raw.Contains("#") ? line.Raw.Substring(0, line.Raw.IndexOf("#")) : line.Raw;
                return line;
            }
        }

        private class CommentHighlighter : SyntaxHighlighter
        {
            public override string HighlightLine(string line)
            {
                if (string.IsNullOrEmpty(line)) return line;
                return line.Contains("#") ? $"{line.Substring(0, line.IndexOf("#"))}<color={Colors.COMMENT}>{line.Substring(line.IndexOf("#"))}</color>" : line;
            }
        }
    }
}
