using System.Text.RegularExpressions;

namespace IC10_Extender.Highlighters
{
    public class RegexHighlighter : SyntaxHighlighter
    {
        protected readonly Regex regex;
        protected readonly string colorKey;

        public RegexHighlighter(Regex regex, string colorKey)
        {
            this.regex = regex;
            this.colorKey = colorKey;
        }

        public override void HighlightLine(StyledLine line)
        {
            var result = regex.Match(line.Remainder());

            while(result.Success)
            {
                line.Consume(result.Value, line.Theme[colorKey]);
                result = regex.Match(line.Remainder());
            }
        }
    }
}
