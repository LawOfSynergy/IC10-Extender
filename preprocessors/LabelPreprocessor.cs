using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Operations;
using System;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    /// <summary>
    /// Since this preprocessor assigns jump labels, this MUST be run after all preprocessors that remove or add lines.
    /// 
    /// TODO: add feature flag to allow jump labels to be followed by an opcode on the same line (e.g. "tmp: j 10") instead of forcing noop
    /// </summary>
    public class LabelPreprocessor: Preprocessor
    {
        public override string SimpleName => "label_preprocessor";
        public override string HelpEntryName => $"<color={Colors.JUMP}>NAME:</color>";
        public override string HelpEntryDescription => "creates a jump label at the named line. j- and b- commands can use these labels in place of line numbers.";

        public const string Regex = @"^(?<label>[a-zA-Z][a-zA-Z0-9_]*):(?:\s+(?<remainder>.*))?";

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
                        line.ForcedOp = new NoOpOperation(Chip, line);
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
}
