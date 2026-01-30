using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using IC10_Extender.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IC10_Extender.Variables
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0039:Use local function", Justification = "Currying-based API Framework")]
    public static class VarUtils
    {
        public static VarFactory<T> Constant<T>(T value)
        {
            return (ctx) => {
                return (out T result, bool throwOnError) => {
                    result = value;
                    return true;
                };
            };
        }

        public static VarFactory<T> Failed<T>(T failedValue, Exception ex)
        {
            return (ctx) =>
            {
                return (out T result, bool throwOnError) => {
                    result = failedValue;
                    if (throwOnError)
                    {
                        throw ex;
                    }
                    return false;
                };
            };
        }

        /**
         * Returns the value stored in the register at the referenced AliasValue index
         */
        public static VarFactory<double> Lookup(this VarFactory<AliasValue?> source)
        {
            return (ctx) =>
            {
                Getter<double> getter = (out double result, bool throwOnError) =>
                {
                    source(ctx)(out var aliasValue, throwOnError);
                    if (aliasValue == null)
                    {
                        if (throwOnError)
                        {
                            throw new ExtendedPCException(ctx.lineNumber, $"Could not find register.");
                        }
                        result = default;
                        return false;
                    }
                    if ((aliasValue.Value.Target & AliasTarget.Register) == AliasTarget.None)
                    {
                        if (throwOnError)
                        {
                            throw new ExtendedPCException(ctx.lineNumber, $"Alias does not point to a register.");
                        }
                        result = default;
                        return false;
                    }
                    result = ctx.chip.Registers[aliasValue.Value.Index];
                    return true;
                };
                return getter;
            };
        }

        /**
         * Returns the AliasValue referencing the register at startIndex, following recurseCount lookups
         */
        public static VarFactory<AliasValue?> Register(int startIndex, int recurseCount = 0)
        {
            return (ctx) =>
            {
                if (recurseCount < 0)
                {
                    return Failed<AliasValue?>(null, new ExtendedPCException(ctx.lineNumber, $"recurseCount: expected >= 0, actual {recurseCount}"))(ctx);
                }

                if (startIndex < 0 || startIndex >= ctx.chip.Registers.Length)
                {
                    return Failed<AliasValue?>(null, new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, ctx.lineNumber))(ctx);
                }

                Getter<AliasValue?> getter = (out AliasValue? result, bool throwOnError) =>
                {
                    var resultValue = startIndex;
                    int index = startIndex;
                    for (int i = 0; i < recurseCount; i++)
                    {
                        resultValue = (int)ctx.chip.Registers[index];
                        index = resultValue;

                        if (index < 0 || index >= ctx.chip.Registers.Length)
                        {
                            if (throwOnError)
                            {
                                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, ctx.lineNumber);
                            }

                            result = null;
                            return false;
                        }
                    }
                    result = new AliasValue(AliasTarget.Register, resultValue);
                    return true;
                };
                return getter;
            };
        }

        /** 
         *  returns the register referenced by the context's token as resolved by alias or parsed as DR code.
         *  If your command has an implicit register lookup, set impliedR to true to add one to the recurse count.
         *  If this is part of a device lookup, set allowDeviceComponent to true to allow DR codes with the device component present.
         *  0, impliedR:true => Register(0, 0) => AliasValue(0, AliasTarget.Register)
         *  0, impliedR:false => Register(0, -1) => error
         *  r0, impliedR:true => Register(0, 1) => AliasValue(value at register 0, AliasTarget.Register)
         *  r0, impliedR:false => Register(0, 0) => AliasValue(0, AliasTarget.Register)
         */
        public static VarFactory<AliasValue?> Register(VarFactory<Dictionary<string, AliasValue>> aliasSource = null, bool allowDeviceComponent = false, bool impliedR = false)
        {
            return (ctx) =>
            {
                Dictionary<string, AliasValue> aliases = null;
                aliasSource?.Invoke(ctx)?.Invoke(out aliases, true);
                aliases = aliases ?? new Dictionary<string, AliasValue>();

                // Try to check aliases first
                if (aliases != null && aliases.TryGetValue(ctx.token, out var alias) && (alias.Target & AliasTarget.Register) != 0)
                {
                    return Register(alias.Index)(ctx);
                }

                // otherwise, parse DR code
                var drCode = new DRCode(ctx.token);

                if (!drCode.success)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.lineNumber);
                }
                if (!allowDeviceComponent && drCode.isDevice)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.lineNumber);
                }

                int startIndex = drCode.index;
                int recurseCount = drCode.regCount - (impliedR ? 0 : 1);

                if(recurseCount < 0)
                {
                    return Failed<AliasValue?>(null, new ExtendedPCException(ctx.lineNumber, $"Cannot accept a negative recurse count. Did you forget to set 'impliedR:true'?"))(ctx);
                }

                return Register(startIndex, recurseCount)(ctx);
            };
        }

        public static VarFactory<long> AsLong(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<long> getter = (out long result, bool throwOnError) =>
                {
                    if (!doubleSource(out double val, throwOnError))
                    {
                        result = -1;
                        return false;
                    }
                    result = (long)Math.Round(val);
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<int> AsInt(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<int> getter = (out int result, bool throwOnError) =>
                {
                    if (!doubleSource(out double val, throwOnError))
                    {
                        result = -1;
                        return false;
                    }
                    result = (int)Math.Round(val);
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<short> AsShort(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<short> getter = (out short result, bool throwOnError) =>
                {
                    if (!doubleSource(out double val, throwOnError))
                    {
                        result = -1;
                        return false;
                    }
                    result = (short)Math.Round(val);
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<byte> AsByte(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<byte> getter = (out byte result, bool throwOnError) =>
                {
                    if (!doubleSource(out double val, throwOnError))
                    {
                        result = default;
                        return false;
                    }
                    result = (byte)Math.Round(val);
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<bool> AsBool(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<bool> getter = (out bool result, bool throwOnError) =>
                {
                    if (!doubleSource(out double val, throwOnError))
                    {
                        result = false;
                        return false;
                    }
                    result = Math.Round(val) != 0;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, long>> AsLong<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, long>> getter = (out Dictionary<TKey, long> result, bool throwOnError) =>
                {
                    if (source(ctx)(out var doubleDict, throwOnError)) {
                        result = doubleDict.ToDictionary(kvp => kvp.Key, kvp => (long)Math.Round(kvp.Value));
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, int>> AsInt<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, int>> getter = (out Dictionary<TKey, int> result, bool throwOnError) =>
                {
                    if (source(ctx)(out var doubleDict, throwOnError))
                    {
                        result = doubleDict.ToDictionary(kvp => kvp.Key, kvp => (int)Math.Round(kvp.Value));
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, short>> AsShort<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, short>> getter = (out Dictionary<TKey, short> result, bool throwOnError) =>
                {
                    if (source(ctx)(out var doubleDict, throwOnError))
                    {
                        result = doubleDict.ToDictionary(kvp => kvp.Key, kvp => (short)Math.Round(kvp.Value));
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, byte>> AsByte<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, byte>> getter = (out Dictionary<TKey, byte> result, bool throwOnError) =>
                {
                    if (source(ctx)(out var doubleDict, throwOnError))
                    {
                        result = doubleDict.ToDictionary(kvp => kvp.Key, kvp => (byte)Math.Round(kvp.Value));
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, bool>> AsBool<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, bool>> getter = (out Dictionary<TKey, bool> result, bool throwOnError) =>
                {
                    if (source(ctx)(out var doubleDict, throwOnError))
                    {
                        result = doubleDict.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value) != 0);
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<ILogicable> DeviceByIdLookup(int id, int network = int.MinValue, bool allowNull = false)
        {
            return (ctx) => {
                Getter<ILogicable> getter = (out ILogicable result, bool throwOnError) =>
                {
                    result = ctx.chip.CircuitHousing.GetLogicableFromId(id, network);

                    if(!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.lineNumber);
                        }
                        return false;
                    }

                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<ILogicable> DeviceByIndexLookup(int index, int network = int.MinValue, bool allowNull = false)
        {
            return (ctx) => {
                Getter<ILogicable> getter = (out ILogicable result, bool throwOnError) =>
                {
                    result = ctx.chip.CircuitHousing.GetLogicableFromIndex(index, network);

                    if (!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.lineNumber);
                        }
                        return false;
                    }

                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<ILogicable> Device(VarFactory<Dictionary<string, AliasValue>> aliasesFactory, bool allowNull = false)
        {
            return (ctx) => {
                
                // Try to check aliases first
                if (aliasesFactory(ctx)(out var aliases, true) && aliases.TryGetValue(ctx.token, out var alias))
                {
                    if ((alias.Target & AliasTarget.Device) != AliasTarget.None)
                    {
                        return DeviceByIndexLookup(alias.Index)(ctx);
                    }
                    if ((alias.Target & AliasTarget.Register) != AliasTarget.None)
                    {
                        return DeviceByIdLookup(alias.Index)(ctx);
                    }
                }

                // otherwise, parse DR code
                var drCode = new DRCode(ctx.token);
                var deviceIdSource = drCode.HasRegisterLookups ? Register(allowDeviceComponent:true).Lookup()(ctx) : Constant<double>(drCode.index)(ctx);

                Getter<ILogicable> getter = (out ILogicable result, bool throwOnError) =>
                {
                    if (!deviceIdSource(out double deviceId, throwOnError))
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.lineNumber);
                        }
                        result = null;
                        return false;
                    }

                    var token = ctx.token;
                    if (
                        token.Length > 0 && (char.IsDigit(token[0]) || token[0] == '$' || token[0] == '%') // number literal, though only char.IsDigit should be possible due to preprocessing
                        || token.Length > 1 && token[0] == 'r' && char.IsDigit(token[1]) // register reference
                    ) {
                        result = ctx.chip.CircuitHousing.GetLogicableFromId((int)deviceId);
                    }
                    else
                    { // device index with optional network index
                        var networkIndex = drCode.network;
                        result = ctx.chip.CircuitHousing.GetLogicableFromIndex((int)deviceId, networkIndex);
                    }

                    if (!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.lineNumber);
                        }
                        return false;
                    }

                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<IMemoryWritable> AsWriteable(this VarFactory<ILogicable> factory)
        {
            return (ctx) =>
            {
                return (out IMemoryWritable writeable, bool throwOnError) =>
                {
                    if (!factory(ctx)(out var logicable, throwOnError))
                    {
                        writeable = null;
                        return false;
                    }
                    if (!(logicable is IMemoryWritable memWritable))
                    {
                        writeable = null;
                        if (throwOnError) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, ctx.lineNumber);
                        return false;
                    }
                    writeable = memWritable;
                    return true;
                };
            };
        }

        public static VarFactory<IMemoryReadable> AsReadable(this VarFactory<ILogicable> factory)
        {
            return (ctx) =>
            {
                return (out IMemoryReadable readable, bool throwOnError) =>
                {
                    if (!factory(ctx)(out var logicable, throwOnError))
                    {
                        readable = null;
                        return false;
                    }
                    if (!(logicable is IMemoryReadable memReadable))
                    {
                        readable = null;
                        if (throwOnError) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, ctx.lineNumber);
                        return false;
                    }
                    readable = memReadable;
                    return true;
                };
            };
        }

        public static Getter<T> Try<T>(this VarFactory<T> source, Binding ctx)
        {
            try
            {
                return source(ctx);
            }
            catch (Exception e)
            {
                return Failed<T>(default, e)(ctx);
            }
        }

        public static VarFactory<T> Any<T>(params VarFactory<T>[] sources)
        {
            return (ctx) => {
                var getters = sources
                    .Select(source => source.Try(ctx))
                    .ToList();
                Getter<T> getter = (out T result, bool throwOnError) =>
                {
                    foreach (var get in getters)
                    {
                        if (get(out result, false))
                        {
                            return true;
                        }
                    }
                    if (throwOnError)
                    {
                        throw new ExtendedPCException(ctx.lineNumber, $"No valid interpretation for {ctx.token}");
                    }
                    result = default;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<T> Lookup<T>(VarFactory<Dictionary<string, T>> WithMappings)
        {
            return (ctx) => {
                Getter<T> getter = (out T result, bool throwOnError) =>
                {
                    WithMappings(ctx)(out var mappings, throwOnError);
                    if (mappings.TryGetValue(ctx.token, out result))
                    {
                        return true;
                    }
                    if (throwOnError)
                    {
                        throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.lineNumber);
                    }
                    result = default;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<T> Substitute<T>(this VarFactory<T> wrapped, VarFactory<Dictionary<string, string>> WithSubstitutions)
        {
            return (ctx) => {
                Getter<T> getter = (out T result, bool throwOnError) =>
                {
                    WithSubstitutions(ctx)(out var substitutions, throwOnError);
                    if (substitutions.TryGetValue(ctx.token, out string substituted))
                    {
                        ctx.token = substituted;
                    }
                    return wrapped(ctx)(out result, throwOnError);
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, TValue>> Concat<TKey, TValue>(params VarFactory<Dictionary<TKey, TValue>>[] sources)
        {
            return (ctx) => {
                Getter<Dictionary<TKey, TValue>> getter = (out Dictionary<TKey, TValue> result, bool throwOnError) =>
                {
                    result = new Dictionary<TKey, TValue>();
                    foreach (var source in sources)
                    {
                        source(ctx)(out var dict, throwOnError);
                        foreach (var kvp in dict)
                        {
                            result[kvp.Key] = kvp.Value;
                        }
                    }
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, TValue>> Concat<TKey, TValue>(this VarFactory<Dictionary<TKey, TValue>> first, params VarFactory<Dictionary<TKey, TValue>>[] others)
        {
            return Concat(others.Prepend(first).ToArray());
        }

        public static VarFactory<Dictionary<string, double>> WithConstants()
        {
            return (ctx) => {
                Getter<Dictionary<string, double>> getter = (out Dictionary<string, double> result, bool throwOnError) =>
                {
                    result = IC10Extender.ConstantValues;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<string, double>> WithDefines()
        {
            return (ctx) => {
                Getter<Dictionary<string, double>> getter = (out Dictionary<string, double> result, bool throwOnError) =>
                {
                    result = ctx.chip.Defines;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<string, AliasValue>> WithAliases(AliasTarget filter)
        {
            return (ctx) => {
                Getter<Dictionary<string, AliasValue>> getter = (out Dictionary<string, AliasValue> result, bool throwOnError) =>
                {
                    result = ctx.chip.Aliases.Where(kvp => (kvp.Value.Target & filter) != AliasTarget.None).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<string, int>> WithJumpLabels()
        {
            return (ctx) => {
                Getter<Dictionary<string, int>> getter = (out Dictionary<string, int> result, bool throwOnError) =>
                {
                    result = ctx.chip.JumpTags;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<double> ParseDouble()
        {
            return (ctx) =>
            {
               Getter<double> getter = (out double result, bool throwOnError) =>
                {
                    if (throwOnError)
                    {
                        result = double.Parse(ctx.token);
                        return true;
                    }
                    return double.TryParse(ctx.token, out result);
                };
                return getter;
            };
        }

        public static VarFactory<long> ParseLong()
        {
            return (ctx) =>
            {
                Getter<long> getter = (out long result, bool throwOnError) =>
                {
                    if (throwOnError)
                    {
                        result = long.Parse(ctx.token);
                        return true;
                    }
                    return long.TryParse(ctx.token, out result);
                };
                return getter;
            };
        }

        public static VarFactory<int> ParseInt()
        {
            return (ctx) =>
            {
                Getter<int> getter = (out int result, bool throwOnError) =>
                {
                    if (throwOnError)
                    {
                        result = int.Parse(ctx.token);
                        return true;
                    }
                    return int.TryParse(ctx.token, out result);
                };
                return getter;
            };
        }

        public static VarFactory<short> ParseShort()
        {
            return (ctx) =>
            {
                Getter<short> getter = (out short result, bool throwOnError) =>
                {
                    if (throwOnError)
                    {
                        result = short.Parse(ctx.token);
                        return true;
                    }
                    return short.TryParse(ctx.token, out result);
                };
                return getter;
            };
        }

        public static VarFactory<byte> ParseByte()
        {
            return (ctx) =>
            {
                Getter<byte> getter = (out byte result, bool throwOnError) =>
                {
                    if (throwOnError)
                    {
                        result = byte.Parse(ctx.token);
                        return true;
                    }
                    return byte.TryParse(ctx.token, out result);
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<string, double>> WithDefinesAndConstants()
        {
             return WithDefines().Concat(
                WithConstants()
            );
        }

        public static Getter<T> Bind<T>(this VarFactory<T> factory, ChipWrapper chip, int lineNumber, string token)
        {
            return factory(new Binding(chip, lineNumber, token));
        }

        public static Getter<T> Bind<T>(this Variable<T> variable, ChipWrapper chip, int lineNumber, string token)
        {
            return variable.Build.Bind(chip, lineNumber, token);
        }
    }
}
