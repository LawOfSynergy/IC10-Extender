using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    public static class DefaultPreprocessors
    {
        public static void Register()
        {
            IC10Extender.Register(new CommentPreprocessor());
            if (
                typeof(ProgrammableChip)
                .GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                != null)
            {
                IC10Extender.Register(new StringPreprocessor());
            }

            IC10Extender.Register(new HashPreprocessor());
            IC10Extender.Register(new BinaryLiteralPreprocessor());
            IC10Extender.Register(new HexLiteralPreprocessor());
            IC10Extender.Register(new LabelPreprocessor());
        }
    }

    public class StringPreprocessor : Preprocessor
    {
        public override string SimpleName => "string_preprocessor";
        public override string HelpEntryName => "<color=#A0A0A0>STR(</color><color=white>\"...\"</color><color=#A0A0A0>)</color>";
        public override string HelpEntryDescription => "any text inside will be packed to a double before processing takes place. Use this to pass a string literal, such as for use in text displays.";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    Localization.RegexResult stringPreprocessing = Localization.GetMatchesForStringPreprocessing(ref line.Raw);
                    for (int index = 0; index < stringPreprocessing.Count(); ++index)
                    {
                        line.Raw = line.Raw.Replace(stringPreprocessing.GetFull(index), ProgrammableChip.PackAscii6(stringPreprocessing.GetName(index), line.OriginatingLineNumber).ToString(CultureInfo.InvariantCulture));
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

    public class CommentPreprocessor : Preprocessor
    {
        public override string SimpleName => "comment_preprocessor";

        public override string HelpEntryName => "#";

        public override string HelpEntryDescription => "any characters on line after this ignored";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                line.Raw = line.Raw.Contains("#") ? line.Raw.Substring(0, line.Raw.IndexOf("#")) : line.Raw;
                return line;
            }
        }
    }

    /// <summary>
    /// Since this preprocessor assigns jump labels, this MUST be run after all preprocessors that remove or add lines.
    /// </summary>
    public class LabelPreprocessor: Preprocessor
    {
        public override string SimpleName => "label_preprocessor";
        public override string HelpEntryName => "<color=purple>NAME:</color>";
        public override string HelpEntryDescription => "creates a jump label at the named line. j- and b- commands can use these labels in place of line numbers.";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                try
                {
                    if (string.IsNullOrEmpty(line.Raw)) return line;

                    var tokens = line.Raw.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 1 && tokens[0].Length > 2 && tokens[0][tokens[0].Length - 1] == ':')
                    {
                        line.Op = new ProgrammableChip._NOOP_Operation(Chip.chip, line.OriginatingLineNumber);
                        string key = tokens[0].Substring(0, tokens[0].Length - 1);
                        if (Chip.chip._JumpTags.ContainsKey(key))
                        {
                            throw new ProgrammableChipException(ICExceptionType.JumpTagDuplicate, line.OriginatingLineNumber);
                        }
                        Chip.chip._JumpTags.Add(key, line.OriginatingLineNumber);
                    }
                    return line;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError(ex);
                    throw ex;
                }
            }
        }
    }

    public class HexLiteralPreprocessor : Preprocessor
    {
        public override string SimpleName => "hex_literal_preprocessor";

        public override string HelpEntryName => "<color=#20B2AA>$</color>";

        public override string HelpEntryDescription => "any valid hex characters after this will be parsed together as a hex value. You can use underscores to help with readability, but they have no functional use. As an example, $F will parse as 15.";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    Localization.RegexResult hexPreprocessing = Localization.GetMatchesForHexPreprocessing(ref line.Raw);
                    for (int index = 0; index < hexPreprocessing.Count(); ++index)
                    {
                        string str = hexPreprocessing.GetName(index).Replace("_", "");
                        line.Raw = line.Raw.Replace(hexPreprocessing.GetFull(index), Convert.ToInt64(str, 16 /*0x10*/).ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new ProgrammableChipException(ICExceptionType.InvalidPreprocessHex, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }

    public class BinaryLiteralPreprocessor : Preprocessor
    {
        public override string SimpleName => "binary_literal_preprocessor";

        public override string HelpEntryName => "<color=#20B2AA>%</color>";

        public override string HelpEntryDescription => "any valid binary numbers (0 or 1) will be parsed together as a binary value. You can use underscores to help with readability, but they have no functional use. As an example, %1111 or %11_11 will parse as 15.";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try
                {
                    Localization.RegexResult binaryPreprocessing = Localization.GetMatchesForBinaryPreprocessing(ref line.Raw);
                    for (int index = 0; index < binaryPreprocessing.Count(); ++index)
                    {
                        string str = binaryPreprocessing.GetName(index).Replace("_", "");
                        line.Raw = line.Raw.Replace(binaryPreprocessing.GetFull(index), Convert.ToInt64(str, 2).ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new ProgrammableChipException(ICExceptionType.InvalidProcessBinary, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }

    public class HashPreprocessor : Preprocessor
    {
        public override string SimpleName => "hash_preprocessor";

        public override string HelpEntryName => "<color=#A0A0A0>HASH(</color><color=white>\"...\"</color><color=#A0A0A0>)</color>";

        public override string HelpEntryDescription => "any text inside will be hashed to an integer before processing takes place. Use this to generate integer values for use wherever hashes are required.";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                if (string.IsNullOrEmpty(line.Raw)) return line;
                try {
                    Localization.RegexResult hashPreprocessing = Localization.GetMatchesForHashPreprocessing(ref line.Raw);
                    for (int index = 0; index < hashPreprocessing.Count(); ++index)
                    {
                        line.Raw = line.Raw.Replace(hashPreprocessing.GetFull(index), Animator.StringToHash(hashPreprocessing.GetName(index)).ToString());
                    }
                } catch (Exception ex) {
                    throw new ProgrammableChipException(ICExceptionType.InvalidPreprocessHash, line.OriginatingLineNumber);
                }

                return line;
            }
        }
    }
}
