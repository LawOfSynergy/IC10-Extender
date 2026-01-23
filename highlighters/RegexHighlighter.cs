using System.Text.RegularExpressions;

namespace IC10_Extender.Highlighters
{
    public class RegexHighlighter : SyntaxHighlighter
    {
        protected readonly Regex regex;
        protected readonly string color;

        public RegexHighlighter(Regex regex, string color)
        {
            this.regex = regex;
            this.color = color;
        }

        public override string HighlightLine(string line)
        {
            return regex.Replace(line, match =>
            {
                return $"<color={color}>{match.Value}</color>";
            });
        }
    }
}
