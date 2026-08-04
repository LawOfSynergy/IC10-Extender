using System;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using IC10_Extender.Highlight;
using IC10_Extender.Variables;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;
using static Assets.Scripts.Objects.Motherboards.LogicType;
using static IC10_Extender.Variables.Variables;


namespace IC10_Extender.Operations
{
    public class ThrowOperation : ExtendedOpCode
    {
        public ThrowOperation() : base(
            new Signature("error").Optional(Int.Named("errCode")),
            $"Forces the chip to stop with an error. May optionally receive an error code to set the containing device's <color={Theme.Default.Enum.Foreground}>Setting</color> to."
        ) {}

        public override Operation Create(CompileContext ctx, Line line, BoundSignature signature, ChipWrapper chip)
        {
            return new Instance(ctx, line, signature, chip);
        }

        public class Instance : Operation
        {
            public Instance(CompileContext ctx, Line line, BoundSignature signature, ChipWrapper chip) : base(ctx, line, signature, chip) { }

            public override int Execute(int index)
            {
                var errorCode = Variables.Get<int>(Chip);

                var drCode = new DRCode("db");
                ILogicable self = Chip.CircuitHousing.GetLogicableFromIndex(drCode.Index, drCode.Network);
                
                if (!self.CanLogicWrite(Setting))
                {
                    throw new ProgrammableChipException(ICExceptionType.IncorrectLogicType, LineNumber);
                }
                self.SetLogicValue(Setting, errorCode);

                throw new UserDefinedException(LineNumber, errorCode);
            }
        }
    }
}
