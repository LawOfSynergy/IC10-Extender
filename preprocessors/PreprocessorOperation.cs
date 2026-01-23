using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IC10_Extender.Preprocessors
{
    public abstract class PreprocessorOperation
    {
        protected readonly ChipWrapper Chip;

        protected PreprocessorOperation(ChipWrapper chip)
        {
            Chip = chip;
        }

        public virtual IEnumerable<Line> Process(IEnumerable<Line> fullScript)
        {
            return DoPass(fullScript); // default to a single-pass preprocessor
        }

        public virtual IEnumerable<Line> DoPass(IEnumerable<Line> fullScript)
        {
            return fullScript
                .Select(line => 
                {
                    try
                    {
                        return ProcessLine(line);
                    }
                    catch (Exception e)
                    {
                        if (!(e is ProgrammableChipException)) Plugin.Logger.LogError(e);
                        throw e.Wrap(line.OriginatingLineNumber);
                    }
                })
                .Where(line  => line != null)
                .Select(line => (Line)line);
        }

        public abstract Line? ProcessLine(Line line);

        public virtual IEnumerable<Line> ReNumber(IEnumerable<Line> fullScript)
        {
            return fullScript.Select((line, index) => { line.LineNumber = (ushort)index; return line; });
        }

        public static PreprocessorOperation NoOp(ChipWrapper chip)
        {
            return new NoOpProcessorOperation(chip);
        }   
    }
}
