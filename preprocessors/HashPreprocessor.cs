using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using static Assets.Scripts.Localization;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    public class HashPreprocessor : Preprocessor
    {
        public override string SimpleName => "hash_preprocessor";

        public override string HelpEntryName => $"<color={Colors.MACRO}>HASH(</color><color={Colors.STRING}>\"...\"</color><color={Colors.MACRO}>)</color>";

        public override string HelpEntryDescription => "any text inside will be hashed to an integer before processing takes place. Use this to generate integer values for use wherever hashes are required.";
        
        public static readonly Regex Regex = new Regex("(?<=$|\\s)HASH\\(\"([^\"]+)\"\\)");

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
                try {
                    RegexResult hashPreprocessing = GetMatchesForHashPreprocessing(ref line.Raw);
                    for (int index = 0; index < hashPreprocessing.Count(); ++index)
                    {
                        line.Raw = line.Raw.Replace(hashPreprocessing.GetFull(index), Animator.StringToHash(hashPreprocessing.GetName(index)).ToString());
                    }
                } catch (Exception) {
                    throw new ProgrammableChipException(ICExceptionType.InvalidPreprocessHash, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
