using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender
{
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
