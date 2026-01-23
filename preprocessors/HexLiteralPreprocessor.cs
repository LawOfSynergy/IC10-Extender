using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Text.RegularExpressions;
using static Assets.Scripts.Localization;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    public class HexLiteralPreprocessor : Preprocessor
    {
        public override string SimpleName => "hex_literal_preprocessor";

        public override string HelpEntryName => $"<color={Color}>$</color>";

        public override string HelpEntryDescription => "any valid hex characters after this will be parsed together as a hex value. You can use underscores to help with readability, but they have no functional use. As an example, $F will parse as 15.";

        public static readonly Regex Regex = new Regex("(?<=$|\\s)\\$([0-9A-Fa-f_]+)+");
        public const string Color = "#20B2AA";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public override SyntaxHighlighter Highlighter()
        {
            return new RegexHighlighter(Regex, Color);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    RegexResult hexPreprocessing = GetMatchesForHexPreprocessing(ref line.Raw);
                    for (int index = 0; index < hexPreprocessing.Count(); ++index)
                    {
                        string str = hexPreprocessing.GetName(index).Replace("_", "");
                        line.Raw = line.Raw.Replace(hexPreprocessing.GetFull(index), Convert.ToInt64(str, 16 /*0x10*/).ToString());
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
