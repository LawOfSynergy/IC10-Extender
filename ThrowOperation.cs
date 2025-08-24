using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;
using static IC10_Extender.HelpString;

namespace IC10_Extender
{
    public class ThrowOperation : ExtendedOpCode
    {
        protected ManualLogSource Logger;

        private static readonly HelpString[] Args = {(REGISTER+INTEGER).Var("errCode").Optional() };

        public ThrowOperation() : base("error")
        {
            Logger = Plugin.GetLogger();
        }

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
            return Args;
        }

        public override string Description()
        {
            return "Forces the chip to stop with an error. May optionally receive a value to set the containing device's <color=orange>Setting</color> to.";
        }

        public class Instance : Operation
        {
            protected readonly DoubleValueVariable ErrorCode;

            public Instance(ChipWrapper chip, int lineNumber, string[] source) : base(chip, lineNumber)
            {
                if (source.Length == 2)
                {
                    ErrorCode = new DoubleValueVariable(chip.chip, lineNumber, source[1], InstructionInclude.MaskDoubleValue, false);
                }
            }

            public override int Execute(int index)
            {
                if (ErrorCode != null)
                {
                    DeviceIndexVariable deviceIndex = new DeviceIndexVariable(Chip.chip, LineNumber, "db", InstructionInclude.MaskDeviceIndex, false);
                    int selfIndex = deviceIndex.GetVariableIndex(AliasTarget.Device, true);
                    ILogicable self = Chip.CircuitHousing.GetLogicableFromIndex(selfIndex, deviceIndex.GetNetworkIndex());
                    double errorCode = ErrorCode.GetVariableValue(AliasTarget.Register);
                    if (!self.CanLogicWrite(LogicType.Setting))
                    {
                        throw new ProgrammableChipException(ICExceptionType.IncorrectLogicType, LineNumber);
                    }
                    self.SetLogicValue(LogicType.Setting, errorCode);
                }
                throw new ProgrammableChipException(ICExceptionType.Unknown, LineNumber);
            }
        }
    }
}
