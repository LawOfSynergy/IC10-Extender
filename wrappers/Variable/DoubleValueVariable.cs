using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Wrappers.Variable
{
    public class DoubleValueVariable : ProgrammableChip._Operation.DoubleValueVariable
    {
        public DoubleValueVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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

        protected bool TryParseAliasAsValue(AliasTarget type, out double value, bool throwException = true)
        {
            return TryParseAliasAsValue((ProgrammableChip._AliasTarget)type, out value, throwException);
        }

        protected new bool TryParseRegisterIndexAsValue(out double value, bool throwException = true)
        {
            return base.TryParseRegisterIndexAsValue(out value, throwException);
        }

        protected new bool TryParseValueAsValue(out double value)
        {
            return base.TryParseValueAsValue(out value);
        }

        public long GetVariableLong(AliasTarget type, bool signed = true, bool errorAtEnd = true)
        {
            return GetVariableLong((ProgrammableChip._AliasTarget)type, signed, errorAtEnd);
        }

        public int GetVariableInt(AliasTarget type, bool errorAtEnd = true)
        {
            return GetVariableInt((ProgrammableChip._AliasTarget)type, errorAtEnd);
        }

        public double GetVariableValue(AliasTarget type, bool errorAtEnd = true)
        {
            return GetVariableValue((ProgrammableChip._AliasTarget)type, errorAtEnd);
        }
    }
}
