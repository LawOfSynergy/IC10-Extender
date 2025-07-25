using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{
    //had to separate the Operation from _Operation with a wrapper, because dependent projects
    //did not have visibility into the _Operation class, and so would not correctly register
    //that the Execute method was already overridden.

    //create a publicly accessable class for this, so that add-ons don't have to publicize the core dll
    public abstract class Operation
    {
        public readonly ChipWrapper Chip;
        public readonly int LineNumber;
        protected const string ZeroString = "0";

        protected Operation(ChipWrapper chip, int lineNumber)
        {
            Chip = chip;
            LineNumber = lineNumber;
        }

        public abstract int Execute(int index);

        public class Variable : ProgrammableChip._Operation.Variable
        {
            protected Variable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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
        }

        protected class IndexVariable : ProgrammableChip._Operation.IndexVariable
        {
            public IndexVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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

        }

        protected class ValueVariable : ProgrammableChip._Operation.ValueVariable
        {
            public ValueVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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
        }

        protected class DoubleValueVariable : ProgrammableChip._Operation.DoubleValueVariable
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

        protected class LineNumberVariable : ProgrammableChip._Operation.LineNumberVariable
        {
            public LineNumberVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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

            public int GetVariableValue(AliasTarget type, bool throwException = true)
            {
                return GetVariableValue((ProgrammableChip._AliasTarget)type, throwException);
            }


        }

        protected class DeviceIndexVariable : ProgrammableChip._Operation.DeviceIndexVariable
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

        protected class IntValuedVariable : ProgrammableChip._Operation.IntValuedVariable
        {
            public IntValuedVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
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

            public int GetVariableValue(AliasTarget type, bool throwException = true)
            {
                return GetVariableValue((ProgrammableChip._AliasTarget)type, throwException);
            }
        }

        protected class EnumValuedVariable<T> : ProgrammableChip._Operation.EnumValuedVariable<T> where T : Enum, IConvertible, new()
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

        public static implicit operator ProgrammableChip._Operation(Operation op) => new OperationWrapper(op);
        public static implicit operator Operation(ProgrammableChip._Operation op) => new ReverseWrapper(op);

        class OperationWrapper : ProgrammableChip._Operation
        {
            private readonly Operation op;
            public OperationWrapper(Operation op) : base(op.Chip.chip, op.LineNumber)
            {
                this.op = op;
            }

            public override int Execute(int index)
            {
                return op.Execute(index);
            }
        }

        class ReverseWrapper : Operation
        {
            private readonly ProgrammableChip._Operation op;

            public ReverseWrapper(ProgrammableChip._Operation op) : base(new ChipWrapper(op._Chip), op._LineNumber)
            {
                this.op = op;
            }

            public override int Execute(int index)
            {
                return op.Execute(index);
            }
        }
    }
}
