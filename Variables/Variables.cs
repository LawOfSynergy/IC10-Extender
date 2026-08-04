using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using IC10_Extender.Highlight;
using IC10_Extender.Wrappers;
using static IC10_Extender.Variables.VarUtils;

namespace IC10_Extender.Variables
{
    public static class Variables
    {
        public static readonly Variable<string> String = new Variable<string>(
            Identity(), HelpString.STRING
        );

        public static readonly Variable<DRCode> AliasBinding = new Variable<DRCode>(
            AliasBinding(), HelpString.DEVICE_INDEX + HelpString.REGISTER
        );

        public static readonly Variable<double> Double = new Variable<double>(
            Any(
                null, null,
                DefineLookup(),
                ConstantLookup(),
                Register().Resolve(),
                ParseDouble()
            ),
            HelpString.NUMBER + HelpString.REGISTER
        );

        public static readonly Variable<long> Long = new Variable<long>(
            Any(
                null, null,
                DefineLookup().AsLong(),
                ConstantLookup().AsLong(),
                Register().Resolve().AsLong(),
                ParseLong()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> Int = new Variable<int>(
            Any(
                null, null,
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LogicType = new Variable<int>(
            Any(
                null, null,
                EnumLookup<LogicType>(ctx => Theme.Default.LogicType).AsInt(),
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.LOGIC_TYPE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LogicSlotType = new Variable<int>(
            Any(
                null, null,
                EnumLookup<LogicSlotType>(ctx => Theme.Default.LogicSlotType).AsInt(),
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.LOGIC_SLOT_TYPE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LogicBatchMode = new Variable<int>(
            Any(
                null, null,
                EnumLookup<LogicBatchMethod>(ctx => ctx.Line.Display.Theme.BatchMode).AsInt(),
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.BATCH_MODE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> RegentMode = new Variable<int>(
            Any(
                null, null,
                EnumLookup<LogicReagentMode>(ctx => ctx.Line.Display.Theme.ReagentMode).AsInt(),
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.REAGENT_MODE + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<short> Short = new Variable<short>(
            Any(
                null, null,
                DefineLookup().AsShort(),
                ConstantLookup().AsShort(),
                Register().Resolve().AsShort(),
                ParseShort()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<byte> Byte = new Variable<byte>(
            Any(
                null, null,
                DefineLookup().AsByte(),
                ConstantLookup().AsByte(),
                Register().Resolve().AsByte(),
                ParseByte()
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<int> LineNumber = new Variable<int>(
            Any(
                null, null,
                JumpLookup(),
                DefineLookup().AsInt(),
                ConstantLookup().AsInt(),
                Register().Resolve().AsInt(),
                ParseInt()
            ),
            HelpString.JUMP_LABEL + HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<AliasValue?> Register = new Variable<AliasValue?>(
            Register(
                impliedR: true
            ),
            HelpString.INTEGER + HelpString.REGISTER
        );

        public static readonly Variable<ILogicable> Device = new Variable<ILogicable>(
            Device(),
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

        public static Variable<object> Interpret(string token)
        {
            HelpString parsed = HelpString.Parse(token);
            if (token.Contains(HelpString.DEVICE_INDEX.ToRawString()) || token.Contains(HelpString.REF_ID.ToRawString()))
            {
                return new Variable<object>(Device.Build.AsObject(), parsed);
            }

            var intTypes = new HelpString[] { 
                HelpString.INTEGER, 
                HelpString.JUMP_LABEL, 
                HelpString.DEVICE_HASH, 
                HelpString.NAME_HASH,
                HelpString.LOGIC_TYPE,
                HelpString.LOGIC_SLOT_TYPE,
                HelpString.BATCH_MODE,
                HelpString.REAGENT_MODE,
                HelpString.SLOT_INDEX
            };

            var applicableIntTypes = intTypes.Where(type => token.Contains(type.ToRawString()));

            if (applicableIntTypes.Any())
            {
                List<VarFactory<int>> lookups = new List<VarFactory<int>>()
                {
                    JumpLookup(),
                    DefineLookup().AsInt(),
                    ConstantLookup().AsInt(),
                    Register().Resolve().AsInt(),
                };

                foreach (var type in applicableIntTypes)
                {
                    if (type == HelpString.LOGIC_TYPE) lookups.Add(EnumLookup<LogicType>(ctx => Theme.Default.LogicType).AsInt());
                    if (type == HelpString.LOGIC_SLOT_TYPE) lookups.Add(EnumLookup<LogicSlotType>(ctx => Theme.Default.LogicSlotType).AsInt());
                    if (type == HelpString.BATCH_MODE) lookups.Add(EnumLookup<LogicBatchMethod>(ctx => Theme.Default.BatchMode).AsInt());
                    if (type == HelpString.REAGENT_MODE) lookups.Add(EnumLookup<LogicReagentMode>(ctx => Theme.Default.ReagentMode).AsInt());
                }

                return new Variable<object>(Any(null, null, lookups.ToArray()).AsObject(), parsed);
            }

            var doubleTypes = new HelpString[]
            {
                HelpString.NUMBER,
                HelpString.REGISTER,
            };

            var applicableDoubleTypes = doubleTypes.Where(type => token.Contains(type.ToRawString()));

            if(applicableDoubleTypes.Any())
            {
                return new Variable<object>(
                    Any(
                        null, null,
                        DefineLookup(),
                        ConstantLookup(),
                        Register().Resolve()
                    ).AsObject(),
                    parsed
                );
            }

            //either string or unknown. fallback to identity
            return new Variable<object>(StyledIdentity(Theme.Default.String).AsObject(), parsed);
        }
    }
}
