using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Wrappers.Variable
{
    public class DeviceIndexVariable : ProgrammableChip._Operation.DeviceIndexVariable
    {
        public DeviceIndexVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
            : base(chip, lineNumber, code, propertiesToUse, throwException) { }

        protected int GetRegisterIndex(string rCode, out int recurseCount, bool throwException = true)
        {
            return _GetRegisterIndex(rCode, out recurseCount, throwException);
        }

        protected int GetDeviceIndex(string dCode, out int recurseCount, bool throwException = true)
        {
            return _GetDeviceIndex(dCode, out recurseCount, throwException);
        }

        protected int GetNetworkIndex(string dCode, bool throwException = true)
        {
            return _GetNetworkIndex(dCode, throwException);
        }

        public new bool TryParseAliasAsJumpTagValue(out int value, bool throwException = true)
        {
            return base.TryParseAliasAsJumpTagValue(out value, throwException);
        }

        protected bool TryParseAliasAsIndex(AliasTarget type, out int index)
        {
            return TryParseAliasAsIndex((ProgrammableChip._AliasTarget)type, out index);
        }

        protected new bool TryParseRegisterIndexAsIndex(out int index, bool throwException = true)
        {
            return base.TryParseRegisterIndexAsIndex(out index, throwException);
        }

        public int GetVariableIndex(AliasTarget type, bool throwError = true)
        {
            return GetVariableIndex((ProgrammableChip._AliasTarget)type, throwError);
        }

        public new int GetNetworkIndex() => base.GetNetworkIndex();

        protected new bool TryParseDeviceIndexAsIndex(out int index, bool throwException = true)
        {
            return base.TryParseDeviceIndexAsIndex(out index, throwException);
        }
    }
}
