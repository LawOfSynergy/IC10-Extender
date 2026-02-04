using Assets.Scripts.Objects.Motherboards;
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
                null, null,
                Lookup(
                    WithDefinesAndConstants()
                ),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve(),
                ParseDouble()
            ),
            HelpString.NUMBER + HelpString.REGISTER
        );

        public static readonly Variable<long> Long = new Variable<long>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants()
                ).AsLong(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsLong(),
                ParseLong()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> Int = new Variable<int>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants()
                ).AsInt(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LogicType = new Variable<int>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants().Concat(WithEnum<LogicType>().AsDouble())
                ).AsInt(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.LOGIC_TYPE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LogicSlotType = new Variable<int>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants().Concat(WithEnum<LogicSlotType>().AsDouble())
                ).AsInt(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.LOGIC_TYPE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<short> Short = new Variable<short>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants()
                ).AsShort(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsShort(),
                ParseShort()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<byte> Byte = new Variable<byte>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants()
                ).AsByte(),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsByte(),
                ParseByte()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LineNumber = new Variable<int>(
            Any(
                null, null,
                Lookup(
                    WithDefinesAndConstants().AsInt().Concat(WithJumpLabels())
                ),
                Register(
                    WithAliases(AliasTarget.Register)
                ).Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.JUMP_LABEL + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<AliasValue?> Register = new Variable<AliasValue?>(
            Register(
                WithAliases(AliasTarget.Register),
                impliedR: true
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<ILogicable> Device = new Variable<ILogicable>(
            Device(
                WithAliases(AliasTarget.Device | AliasTarget.Register)
            ),
            HelpString.DEVICE_INDEX + HelpString.REGISTER + HelpString.INTEGER
        );

        public static readonly Variable<IMemoryWritable> Writable = new Variable<IMemoryWritable>(
            Device.Build.AsWritable(), 
            Device.HelpString
        );

        public static readonly Variable<IMemoryReadable> Readable = new Variable<IMemoryReadable>(
            Device.Build.AsReadable(),
            Device.HelpString
        );

        public static readonly Variable<IMemoryReadWritable> ReadWritable = new Variable<IMemoryReadWritable>(
            Device.Build.AsReadWritable(),
            Device.HelpString
        );
    }
}
