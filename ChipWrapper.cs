using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{
    public class ChipWrapper
    {
        public readonly ProgrammableChip chip;
        public PreExecute PreExecute = IC10Extender.NoOpPreExecute;
        public PostExecute PostExecute = IC10Extender.NoOpPostExecute;

        public ChipWrapper(ProgrammableChip chip)
        {
            this.chip = chip;
        }

        public static List<IScriptEnum> InternalEnums
        {
            get => ProgrammableChip.InternalEnums;
            set => ProgrammableChip.InternalEnums = value;
        }

        public double[] Registers => chip._Registers;
        public int StackPointerIndex => chip._StackPointerIndex;
        public int ReturnAddressIndex => chip._ReturnAddressIndex;
        public double[] Stack => chip._Stack;

        /// <summary>
        /// this one returns a copy of the backing dictionary since some translation needs to occur between ProgrammableChip._AliasValue and AliasValue.
        /// If you want to modify this list: get the list, make your changes, then set it to the modified list and the backing list will be updated to reflect
        /// the changes.
        /// </summary>
        public Dictionary<string, AliasValue> Aliases
        {
            get
            {
                Dictionary<string, AliasValue> rv = new Dictionary<string, AliasValue>();
                foreach (var entry in chip._Aliases)
                {
                    rv.Add(entry.Key, entry.Value);
                }
                return rv;
            }
            set
            {
                chip._Aliases.Clear();
                foreach (var entry in value)
                {
                    chip._Aliases[entry.Key] = entry.Value;
                }
            }
        }

        public Dictionary<string, int> JumpTags => chip._JumpTags;
        public Dictionary<string, double> Defines => chip._Defines;

        public int NextAddr
        {
            get => chip._NextAddr;
            set => chip._NextAddr = value;
        }

        public List<string> LinesOfCode => chip._LinesOfCode.Select(loc => loc.ToString()).ToList();

        public int ExecuteIndex
        {
            get => chip._executeIndex;
            set => chip._executeIndex = value;
        }

        public ushort ErrorLineNumber
        {
            get => chip._ErrorLineNumber;
            set => chip._ErrorLineNumber = value;
        }

        public string ErrorTypeString => chip.ErrorTypeString;

        public ProgrammableChipException.ICExceptionType ErrorType
        {
            get => chip._ErrorType;
            set => chip._ErrorType = value;
        }

        public bool CompilationError => chip.CompilationError;

        public ushort CompileErrorLineNumber
        {
            get => chip.CompileErrorLineNumber;
            set => chip.CompileErrorLineNumber = value;
        }

        public ProgrammableChipException.ICExceptionType CompileErrorType
        {
            get => chip.CompileErrorType;
            set => chip.CompileErrorType = value;
        }

        public ICircuitHolder CircuitHousing => chip.CircuitHousing;
    }

    [Flags]
    public enum AliasTarget
    {
        None = 0,
        Register = 1,
        Device = 2,
        Network = 4,
        All = 268435455, // 0x0FFFFFFF
    }

    public readonly struct AliasValue
    {
        public readonly AliasTarget Target;
        public readonly int Index;

        public AliasValue(AliasTarget target, int index)
        {
            Target = target;
            Index = index;
        }

        public static implicit operator ProgrammableChip._AliasValue(AliasValue self) => new ProgrammableChip._AliasValue((ProgrammableChip._AliasTarget)self.Target, self.Index);
        public static implicit operator AliasValue(ProgrammableChip._AliasValue self) => new AliasValue((AliasTarget)self.Target, self.Index);
    }

    public readonly struct HelpString
    {
        public static readonly HelpString STRING = new HelpString("str", "white");
        public static readonly HelpString DEVICE_INDEX = new HelpString("d?", "green");
        public static readonly HelpString REGISTER = new HelpString("r?", "#0080FFFF");
        public static readonly HelpString INTEGER = new HelpString("int", "#20B2AA");
        public static readonly HelpString NUMBER = new HelpString("num", "#20B2AA");
        public static readonly HelpString OR = new HelpString("|", "#585858FF");
        public static readonly HelpString LOGIC_TYPE = new HelpString("logicType", "orange");
        public static readonly HelpString LOGIC_SLOT_TYPE = new HelpString("logicSlotType", "orange");
        public static readonly HelpString BATCH_MODE = new HelpString("batchMode", "orange");
        public static readonly HelpString DEVICE_HASH = new HelpString("deviceHash", "#20B2AA");
        public static readonly HelpString NAME_HASH = new HelpString("NameHash", "#20B2AA");
        public static readonly HelpString SLOT_INDEX = new HelpString("slotIndex", "#20B2AA");
        public static readonly HelpString REAGENT_MODE = new HelpString("reagentMode", "orange");

        private readonly string _string;
        private readonly string color;

        public HelpString(string str, string color = null, bool stripExistingColors = false)
        {
            _string = stripExistingColors ? ProgrammableChip.StripColorTags(str) : str;
            this.color = color;
        }

        public new string ToString() => color == null ? _string : string.Format($"<color={color}>{_string}</color>");

        public HelpString Var(string variable)
        {
            return new HelpString($"{variable}({ToString()})");
        }

        public HelpString Optional()
        {
            return new HelpString($"[{ToString()}]");
        }

        public HelpString Strip(string newColor = null)
        {
            return new HelpString(_string, newColor, true);
        }

        public static HelpString operator +(HelpString left, HelpString right)
        {
            return new HelpString(left.ToString() + OR.ToString() + right.ToString());
        }

        public static implicit operator HelpString(ProgrammableChip.HelpString other) => new HelpString(other._string);
        public static implicit operator ProgrammableChip.HelpString(HelpString other) => new ProgrammableChip.HelpString(other.ToString());
        public static implicit operator string (HelpString other) => other.ToString();
    }
}
