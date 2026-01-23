using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using IC10_Extender.Exceptions;
using IC10_Extender.Variables;
using static IC10_Extender.Variables.Variables;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender.Operations
{
    public class ThrowOperation : ExtendedOpCode
    {
        private static readonly Variable<int> ErrorCode = Int.Named("errCode").Optional();

        public ThrowOperation() : base("error") {}

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length > 2) throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
        }

        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new Instance(chip, lineNumber, source);
        }

        public override HelpString[] Params()
        {
            return new HelpString[]{ ErrorCode };
        }

        public override string Description()
        {
            return "Forces the chip to stop with an error. May optionally receive an error code to set the containing device's <color=orange>Setting</color> to.";
        }

        public class Instance : Operation
        {
            protected readonly Getter<int> ErrorCodeGetter;

            public Instance(ChipWrapper chip, int lineNumber, string[] source) : base(chip, lineNumber)
            {
                if (source.Length == 2)
                {
                    ErrorCodeGetter = ErrorCode.Build(new Binding(chip, lineNumber, source[1]));
                }
            }

            public override int Execute(int index)
            {
                var drCode = new DRCode("db");
                ILogicable self = Chip.CircuitHousing.GetLogicableFromIndex(drCode.index, drCode.network);
                if(ErrorCodeGetter(out var errorCode, false))
                {
                    if (!self.CanLogicWrite(LogicType.Setting))
                    {
                        throw new ProgrammableChipException(ICExceptionType.IncorrectLogicType, LineNumber);
                    }
                    self.SetLogicValue(LogicType.Setting, errorCode);
                } else
                {
                    errorCode = 0;
                }
                throw new UserDefinedException(LineNumber, errorCode);
            }
        }
    }
}
