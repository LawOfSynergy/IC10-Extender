using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Text.RegularExpressions;
using static Assets.Scripts.Localization;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    public class RawHexLiteralPreprocessor : Preprocessor
    {
        public override string SimpleName => "raw_hex_literal_preprocessor";

        public override string HelpEntryName => $"<color={Colors.NUMBER}>$$</color>";

        public override string HelpEntryDescription => "any valid hex characters after this will be parsed together as a hex value. You can use underscores to help with readability, but they have no functional use. As an example, $F will parse as 15. The original text is not maintained when compiled, and will be replaced with the raw computed value.";

        public static readonly Regex Regex = new Regex("(?<=$|\\s)\\$\\$([0-9A-Fa-f_]+)+");

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public override SyntaxHighlighter Highlighter()
        {
            return new RegexHighlighter(Regex, Colors.NUMBER);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    RegexResult hexPreprocessing = Regex.GetMatches(line.Raw);
                    for (int index = 0; index < hexPreprocessing.Count(); ++index)
                    {
                        string str = hexPreprocessing.GetName(index).Replace("_", "");
                        line.Raw = line.Raw.Replace(hexPreprocessing.GetFull(index), Convert.ToInt64(str, 16 /*0x10*/).ToString());
                        line.Display = line.Raw;
                    }
                }
                catch (Exception)
                {
                    throw new ProgrammableChipException(ICExceptionType.InvalidPreprocessHex, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
