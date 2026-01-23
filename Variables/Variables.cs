using Assets.Scripts.Objects.Pipes;
using IC10_Extender.Wrappers;
using static IC10_Extender.Variables.VarUtils;

namespace IC10_Extender.Variables
{
    public static class Variables
    {
        // 
        public static readonly Variable<double> Double = new Variable<double>(
            Any(
                Lookup(
                    WithDefinesAndConstants()
                ),
                Register(),
                ParseDouble()
            ),
            HelpString.NUMBER + HelpString.REGISTER
        );

        public static readonly Variable<long> Long = new Variable<long>(
            Any(
                Lookup(
                    WithDefinesAndConstants()
                ).AsLong(),
                Register().AsLong(),
                ParseLong()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> Int = new Variable<int>(
            Any(
                Lookup(
                    WithDefinesAndConstants()
                ).AsInt(),
                Register().AsInt(),
                ParseInt()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<short> Short = new Variable<short>(
            Any(
                Lookup(
                    WithDefinesAndConstants()
                ).AsShort(),
                Register().AsShort(),
                ParseShort()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<byte> Byte = new Variable<byte>(
            Any(
                Lookup(
                    WithDefinesAndConstants()
                ).AsByte(),
                Register().AsByte(),
                ParseByte()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LineNumber = new Variable<int>(
            Any(
                Lookup(
                    WithDefinesAndConstants().AsInt().Concat(WithJumpLabels())
                ),
                Register().AsInt(),
                ParseInt()
            ),
            HelpString.JUMP_LABEL + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<ILogicable> Device = new Variable<ILogicable>(
            Device(
                WithAliases(AliasTarget.Device | AliasTarget.Register)
            ),
            HelpString.DEVICE_INDEX + HelpString.REGISTER + HelpString.INTEGER
        );

        public static readonly Variable<IMemoryWritable> Writeable = new Variable<IMemoryWritable>(
            Device.Build.AsWriteable(), 
            Device.HelpString
        );

        public static readonly Variable<IMemoryReadable> Readable = new Variable<IMemoryReadable>(
            Device.Build.AsReadable(),
            Device.HelpString
        );
    }
}
