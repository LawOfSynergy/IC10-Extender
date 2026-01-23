using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Text.RegularExpressions;
using static Assets.Scripts.Localization;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    public class RawBinaryLiteralPreprocessor : Preprocessor
    {
        public override string SimpleName => "raw_binary_literal_preprocessor";

        public override string HelpEntryName => $"<color={Color}>%%</color>";

        public override string HelpEntryDescription => "any valid binary numbers (0 or 1) will be parsed together as a binary value. You can use underscores to help with readability, but they have no functional use. As an example, %1111 or %11_11 will parse as 15. The original text is not maintained when compiled, and will be replaced with the raw computed value.";

        public static readonly Regex Regex = new Regex("(?<=$|\\s)\\%\\%([01_]+)");
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
                    RegexResult binaryPreprocessing = Regex.GetMatches(line.Raw);
                    for (int index = 0; index < binaryPreprocessing.Count(); ++index)
                    {
                        string str = binaryPreprocessing.GetName(index).Replace("_", "");
                        line.Raw = line.Raw.Replace(binaryPreprocessing.GetFull(index), Convert.ToInt64(str, 2).ToString());
                        line.Display = line.Raw;
                    }
                }
                catch (Exception)
                {
                    throw new ProgrammableChipException(ICExceptionType.InvalidProcessBinary, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
