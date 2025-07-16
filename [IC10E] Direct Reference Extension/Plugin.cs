using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using BepInEx;
using IC10_Extender;
using static IC10_Extender.HelpString;
using System.ComponentModel;

namespace IC10E__Direct_Reference_Extension
{
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e.dre", "[IC10E] Direct Reference Extensions", "0.0.1.0")]
    [BepInDependency("net.lawofsynergy.stationeers.ic10e")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake() { 
            IC10Extender.Register(new LoadSlotByDirectReference());
            IC10Extender.Register(new SetSlotByDirectReference());
        }
    }

    public class Constants
    {
        public static readonly HelpString ID = (REGISTER + NUMBER).Var("id");
    }

    public class LoadSlotByDirectReference : ExtendedOpCode
    {
        private static readonly HelpString[] Args = { REGISTER, Constants.ID, SLOT_INDEX, LOGIC_SLOT_TYPE };
        public LoadSlotByDirectReference() : base("lsd") { }

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length != 5) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
        }
        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new LSDInstance(chip, lineNumber, source[1], source[2], source[3], source[4]);
        }

        public override HelpString[] Params()
        {
            return Args;
        }

        public class LSDInstance : Operation
        {
            protected readonly IndexVariable Store;
            protected readonly IntValuedVariable DeviceId;
            protected readonly IntValuedVariable SlotIndex;
            protected readonly EnumValuedVariable<LogicSlotType> LogicType;

            public LSDInstance(ChipWrapper chip, int lineNumber, string register, string referenceId, string slot, string logicType) : base(chip, lineNumber)
            {
                Store = new IndexVariable(chip.chip, lineNumber, register, InstructionInclude.MaskStoreIndex, false);
                DeviceId = new IntValuedVariable(chip.chip, lineNumber, referenceId, InstructionInclude.MaskDoubleValue, false);
                SlotIndex = new IntValuedVariable(chip.chip, lineNumber, slot, InstructionInclude.MaskDoubleValue, false);
                LogicType = new EnumValuedVariable<LogicSlotType>(chip.chip, lineNumber, logicType, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicSlotType, false);
            }

            public override int Execute(int index)
            {
                int variableIndex = Store.GetVariableIndex(AliasTarget.Register);
                int slotIndex = SlotIndex.GetVariableValue(AliasTarget.Register);
                ILogicable logicableFromId = Chip.CircuitHousing.GetLogicableFromId(DeviceId.GetVariableValue(AliasTarget.Register));
                LogicSlotType logicType = LogicType.GetVariableValue(AliasTarget.Register);
                if(!logicableFromId.CanLogicRead(logicType, slotIndex)) {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, LineNumber);
                }
                Chip.Registers[variableIndex] = logicableFromId.GetLogicValue(logicType, slotIndex);
                return index + 1;
            }
        }
    }

    public class SetSlotByDirectReference : ExtendedOpCode
    {
        private static readonly HelpString[] Args = { Constants.ID, SLOT_INDEX, LOGIC_SLOT_TYPE, (REGISTER + NUMBER).Var("value") };
        public SetSlotByDirectReference() : base("ssd") { }

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length != 5) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
        }
        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new SSDInstance(chip, lineNumber, source[1], source[2], source[3], source[4]);
        }

        public override HelpString[] Params()
        {
            return Args;
        }

        public class SSDInstance : Operation
        {
            protected readonly IntValuedVariable DeviceId;
            protected readonly IntValuedVariable SlotIndex;
            protected readonly EnumValuedVariable<LogicSlotType> LogicType;
            protected readonly DoubleValueVariable Arg1;

            public SSDInstance(ChipWrapper chip, int lineNumber, string referenceId, string slot, string logicType, string registerOrValue) : base(chip, lineNumber)
            {
                DeviceId = new IntValuedVariable(chip.chip, lineNumber, referenceId, InstructionInclude.MaskDoubleValue, false);
                SlotIndex = new IntValuedVariable(chip.chip, lineNumber, slot, InstructionInclude.MaskDoubleValue, false);
                LogicType = new EnumValuedVariable<LogicSlotType>(chip.chip, lineNumber, logicType, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicSlotType, false);
                Arg1 = new DoubleValueVariable(chip.chip, lineNumber, registerOrValue, InstructionInclude.MaskDoubleValue, false);
            }

            public override int Execute(int index)
            {
                int slotIndex = SlotIndex.GetVariableValue(AliasTarget.Register);
                double arg1 = Arg1.GetVariableValue(AliasTarget.Register);
                ILogicable logicableFromId = Chip.CircuitHousing.GetLogicableFromId(DeviceId.GetVariableValue(AliasTarget.Register));
                LogicSlotType logicType = LogicType.GetVariableValue(AliasTarget.Register);
                if (!(logicableFromId is ISlotWriteable slotWriteable))
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotSlotWriteable, LineNumber);
                }
                if(!slotWriteable.CanLogicWrite(logicType, slotIndex))
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, LineNumber);
                }
                slotWriteable.SetLogicValue(logicType, slotIndex, arg1);
                return index + 1;
            }
        }
    }

    //public class PushDevicesInBatch : ExtendedOpCode
    //{
    //    public PushDevicesInBatch() : base("pdb") { }

    //    public override void Accept(int lineNumber, string[] source)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public override HelpString[] Params()
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}

    //public class PushDevicesInBatchByName : ExtendedOpCode
    //{
    //    public PushDevicesInBatchByName() : base("pdbn") { }

    //    public override void Accept(int lineNumber, string[] source)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}
