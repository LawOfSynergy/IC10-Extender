using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using static Assets.Scripts.Localization;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    public class RawHashPreprocessor : Preprocessor
    {
        public override string SimpleName => "raw_hash_preprocessor";

        public override string HelpEntryName => $"<color={Colors.MACRO}>RHASH(</color><color={Colors.STRING}>\"...\"</color><color={Colors.MACRO}>)</color>";

        public override string HelpEntryDescription => "any text inside will be hashed to an integer before processing takes place. Use this to generate integer values for use wherever hashes are required. The original text is not maintained when compiled, and will be replaced with the raw computed value.";

        public static readonly Regex Regex = new Regex("(?<=$|\\s)RHASH\\(\"([^\"]+)\"\\)");

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public override SyntaxHighlighter Highlighter()
        {
            return new RegexGroupHighlighter(Regex, Colors.MACRO, Colors.STRING);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    RegexResult hashPreprocessing = Regex.GetMatches(line.Raw);
                    for (int index = 0; index < hashPreprocessing.Count(); ++index)
                    {
                        line.Raw = line.Raw.Replace(hashPreprocessing.GetFull(index), Animator.StringToHash(hashPreprocessing.GetName(index)).ToString());
                        line.Display = line.Raw;
                    }
                }
                catch (Exception)
                {
                    throw new ProgrammableChipException(ICExceptionType.InvalidPreprocessHash, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
