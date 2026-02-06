using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Compat;
using IC10_Extender.Operations;
using Objects.Rockets.Scanning;
using System;
using System.Text.RegularExpressions;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Preprocessors
{
    /// <summary>
    /// Since this preprocessor assigns jump labels, this MUST be run after all preprocessors that remove or add lines.
    /// </summary>
    public class LabelPreprocessor: Preprocessor
    {
        public override string SimpleName => "label_preprocessor";
        public override string HelpEntryName => $"<color={Colors.JUMP}>NAME:</color>";
        public override string HelpEntryDescription => "creates a jump label at the named line. These can used in place of line numbers, like in j- and b- commands.";

        public const string Pattern = @"^(?<label>[^:\s]*):(?:\s+(?<remainder>.*))?";

        public override PreprocessorOperation Create(ChipWrapper chip)
        {
            return new Instance(chip);
        }

        public class Instance : PreprocessorOperation
        {
            public Instance(ChipWrapper chip) : base(chip) { }

            public override Line? ProcessLine(Line line)
            {
                bool sharedLines = Plugin.Instance.LabelsCanShareLineEnabled.Accept();
                try
                {
                    if (string.IsNullOrEmpty(line.Raw)) return line;

                    if (sharedLines) { 
                        for (var results = Regex.Match(line.Raw, Pattern); results.Success; )
                        {
                            string label = results.Groups["label"].Value;
                            if (Chip.chip._JumpTags.ContainsKey(label))
                            {
                                throw new ProgrammableChipException(ICExceptionType.JumpTagDuplicate, line.LineNumber);
                            }
                            Chip.chip._JumpTags.Add(label, line.LineNumber);
                            line.Raw = results.Groups["remainder"].Value;
                        }
                    } else
                    {
                        var results = Regex.Match(line.Raw, Pattern);

                        //if no match, or sharing line, not a valid label. return without operating
                        if (!results.Success || results.Groups["remainder"].Length > 0)
                        {
                            return line;
                        }

                        string label = results.Groups["label"].Value;
                        if (Chip.chip._JumpTags.ContainsKey(label))
                        {
                            throw new ProgrammableChipException(ICExceptionType.JumpTagDuplicate, line.LineNumber);
                        }
                        Chip.chip._JumpTags.Add(label, line.LineNumber);
                        line.ForcedOp = new NoOpOperation(Chip, line);
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