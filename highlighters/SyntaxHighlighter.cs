using System.Linq;

namespace IC10_Extender
{
    public class SyntaxHighlighter
    {
        public virtual string Highlight(string masterString) { return string.Join("\n", masterString.Split('\n').Select(HighlightLine)); }
        public virtual string HighlightLine(string line) { return line; }
    }
}
