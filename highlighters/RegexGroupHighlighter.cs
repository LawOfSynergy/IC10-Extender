using System.Text.RegularExpressions;

namespace IC10_Extender.Highlighters
{
    public class RegexGroupHighlighter : RegexHighlighter
    {
        protected readonly string[] innerColorKeys;

        public RegexGroupHighlighter(Regex regex, string colorKey, params string[] innerColorKeys) : base(regex, colorKey)
        {
            this.innerColorKeys = innerColorKeys;
        }

        public override void HighlightLine(StyledLine line)
        {
            var result = regex.Match(line.Remainder());

            while (result.Success)
            {
                var groups = result.Groups;
                string matchResult = groups[0].Value;
                string nonGrouped = matchResult;

                for (int i = 1; i < groups.Count; i++)
                {
                    foreach (Capture capture in groups[i].Captures)
                    {
                        line.Consume(capture.Value, innerColorKeys[i - 1]);
                        nonGrouped = nonGrouped.Replace(capture.Value, "");
                    }
                }
                line.Consume(nonGrouped, colorKey);
            }
        }
    }
}
