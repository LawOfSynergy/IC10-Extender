using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using BepInEx;
using IC10_Extender;

namespace _IC10E__Direct_Reference_Extension
{
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e.dre", "IC10 Extender", "0.0.1.0")]
    [BepInDependency("net.lawofsynergy.stationeers.ic10")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake() { 
            IC10Extender.Register(new LoadSlotByDirectReference());
        }
    }

    public class LoadSlotByDirectReference : ExtendedOpCode
    {
        public LoadSlotByDirectReference() : base("lsd") { }

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length != 5) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
        }
        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new LSDInstance(chip, lineNumber, source[1], source[2], source[3], source[4]);
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
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, LineNumber);
                }
                Chip.Registers[variableIndex] = logicableFromId.GetLogicValue(logicType, slotIndex);
                return index + 1;
            }
        }
    }
}
