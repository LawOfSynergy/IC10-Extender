using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;

namespace IC10_Extender
{
    public readonly struct HelpString
    {
        public static readonly HelpString STRING = new HelpString("str", Theme.Vanilla.String);
        public static readonly HelpString DEVICE_INDEX = new HelpString("d?", Theme.Vanilla.Device);
        public static readonly HelpString REGISTER = new HelpString("r?", Theme.Vanilla.Register);
        public static readonly HelpString INTEGER = new HelpString("int", Theme.Vanilla.Number);
        public static readonly HelpString NUMBER = new HelpString("num", Theme.Vanilla.Number);
        public static readonly HelpString OR = new HelpString("|", Theme.Vanilla.Comment);
        public static readonly HelpString LOGIC_TYPE = new HelpString("logicType", Theme.Vanilla.LogicType);
        public static readonly HelpString LOGIC_SLOT_TYPE = new HelpString("logicSlotType", Theme.Vanilla.LogicSlotType);
        public static readonly HelpString BATCH_MODE = new HelpString("batchMode", Theme.Vanilla.LogicType);
        public static readonly HelpString DEVICE_HASH = new HelpString("deviceHash", Theme.Vanilla.Number);
        public static readonly HelpString NAME_HASH = new HelpString("NameHash", Theme.Vanilla.Number);
        public static readonly HelpString SLOT_INDEX = new HelpString("slotIndex", Theme.Vanilla.Number);
        public static readonly HelpString REAGENT_MODE = new HelpString("reagentMode", Theme.Vanilla.LogicType);
        public static readonly HelpString JUMP_LABEL = new HelpString("jump", Theme.Vanilla.Jump);

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
