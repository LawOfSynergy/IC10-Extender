using Assets.Scripts.Objects.Electrical;
using System;

namespace IC10_Extender.Wrappers.Variable
{
    public class EnumValuedVariable<T> : ProgrammableChip._Operation.EnumValuedVariable<T> where T : Enum, IConvertible, new()
    {
        public EnumValuedVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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

        public T GetVariableValue(AliasTarget type, bool throwException = true)
        {
            return GetVariableValue((ProgrammableChip._AliasTarget)type, throwException);
        }
    }
}
