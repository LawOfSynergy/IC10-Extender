using System.Text.RegularExpressions;

namespace IC10_Extender
{
    public class RegexGroupHighlighter : RegexHighlighter
    {
        protected readonly string[] colors;

        public RegexGroupHighlighter(Regex regex, string color, params string[] innerColors) : base(regex, color)
        {
            this.colors = innerColors;
        }

        public override string HighlightLine(string line)
        {
            
            return regex.Replace(line, match =>
            {
                GroupCollection groups = match.Groups;
                string matchResult = groups[0].Value;
                string nonGrouped = matchResult;
                for (int i = 1; i < groups.Count; i++)
                {
                    foreach (Capture capture in groups[i].Captures)
                    {
                        nonGrouped = nonGrouped.Replace(capture.Value, "|");
                        matchResult = matchResult.Replace(capture.Value, $"<color={colors[(i - 1)]}>{capture.Value}</color>");
                    }
                }
                foreach (string token in nonGrouped.Split('|', System.StringSplitOptions.RemoveEmptyEntries))
                {
                    matchResult = matchResult.Replace(token, $"<color={color}>{token}</color>");
                }
                return matchResult;
            });
        }
    }
}
