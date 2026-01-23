using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using static Assets.Scripts.Localization;

namespace IC10_Extender.Preprocessors
{
    public class RawStringPreprocessor : Preprocessor
    {
        public override string SimpleName => "raw_string_preprocessor";
        public override string HelpEntryName => $"<color={Color}>RSTR(</color><color={InnerColor}>\"...\"</color><color={Color}>)</color>";
        public override string HelpEntryDescription => "any text inside will be packed to a double before processing takes place. Use this to pass a string literal, such as for use in text displays. The original text is not maintained when compiled, and will be replaced with the raw computed value.";

        public static readonly Regex Regex = new Regex("(?<=$|\\s)RSTR\\(\"([^\"]+)\"\\)");
        public const string Color = "colormacro";
        public const string InnerColor = "colorstring";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public override SyntaxHighlighter Highlighter()
        {
            return new RegexGroupHighlighter(Regex, Color, InnerColor);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    RegexResult stringPreprocessing = Regex.GetMatches(line.Raw);
                    for (int index = 0; index < stringPreprocessing.Count(); ++index)
                    {
                        line.Raw = line.Raw.Replace(stringPreprocessing.GetFull(index), ProgrammableChip.PackAscii6(stringPreprocessing.GetName(index), line.OriginatingLineNumber).ToString(CultureInfo.InvariantCulture));
                        line.Display = line.Raw;
                    }
                }
                catch (Exception ex)
                {
                    throw ex.Wrap(line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
