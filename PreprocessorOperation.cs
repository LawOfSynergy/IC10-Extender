using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.OpenNat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.ImGuiUi.Debug;
using static IC10_Extender.PreprocessorOperation;

namespace IC10_Extender
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
            return fullScript.Select((Line line, int index) => { line.LineNumber = (ushort)index; return line; });
        }
    }

    public struct Line
    {
        public string Raw;
        public readonly ushort OriginatingLineNumber;
        public ushort LineNumber;
        public Operation ForcedOp;
        public PreExecute PreExecute;
        public PostExecute PostExecute;

        public Line(string raw, ushort originatingLineNumber, Operation forcedOp = null)
        {
            Raw = raw;
            OriginatingLineNumber = originatingLineNumber;
            LineNumber = originatingLineNumber;
            ForcedOp = forcedOp;
            PreExecute = IC10Extender.NoOpPreExecute;
            PostExecute = IC10Extender.NoOpPostExecute;
        }

        public new string ToString()
        {
            return $"{OriginatingLineNumber}: {Raw}";
        }
    }
}
