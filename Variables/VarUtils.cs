using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using IC10_Extender.Highlight;
using IC10_Extender.Wrappers;

namespace IC10_Extender.Variables
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0039:Use local function", Justification = "Currying-based API Framework")]
    public static class VarUtils
    {
        public static ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        #region general purpose
        /**
         * Applies <paramref name="style"/> to the region in Original and Display covered by
         * ctx.Token. The anchor is passed directly to Line.Consume(), which handles both the
         * normal (unconsumed) case and the preprocessor-already-consumed case correctly.
         */
        public static void ApplyStyle(Binding ctx, Style style)
        {
            ctx.Line.Consume(ctx.Token, style);
        }

        public static VarFactory<string> Identity()
        {
            return (ctx) =>
            {
                return WrapConstant(ctx.Token.Text)(ctx);
            };
        }

        public static VarFactory<string> StyledIdentity(Style style)
        {
            return (ctx) =>
            {
                ApplyStyle(ctx, style);
                return WrapConstant(ctx.Token.Text)(ctx);
            };
        }

        public static VarFactory<T> WrapConstant<T>(T value)
        {
            return (ctx) => {
                return (ChipWrapper chip, out T result, bool throwOnError) => {
                    result = value;
                    return true;
                };
            };
        }

        public static VarFactory<T> DefaultIfEmpty<T>(VarFactory<T> source, T defaultValue)
        {
            return (ctx) =>
            {
                if (string.IsNullOrEmpty(ctx.Token.Text)) return WrapConstant(defaultValue)(ctx);
                return source(ctx);
            };
        }

        public static VarFactory<T> Any<T>(Exception compileError = null, Exception runtimeError = null, params VarFactory<T>[] sources)
        {
            return (ctx) => {
                var selected = sources
                    .Select(source => //select first source that doesn't throw compile error
                    {
                        try { return source(ctx); } catch { return null; }
                    })
                    .Where(source => source != null)
                    .FirstOrDefault() ?? throw compileError ?? new ExtendedPCException(ctx.LineNumber, $"No valid interpretation for {ctx.Token.Text}");
                Getter<T> getter = (ChipWrapper chip, out T result, bool throwOnError) =>
                {
                    if (selected(chip, out result, false))
                    {
                        return true;
                    }
                    if (throwOnError)
                    {
                        throw runtimeError ?? new ExtendedPCException(ctx.LineNumber, $"Selected interperetation for {ctx.Token.Text} failed at runtime");
                    }
                    result = default;
                    return false;
                };
                return getter;
            };
        }
        #endregion general purpose

        #region device and register
        //This represents a drcode used as the value the alias preprocessor
        public static VarFactory<DRCode> AliasBinding()
        {
            return (ctx) =>
            {
                var drcode = new DRCode(ctx.Token.Text);
                if (!drcode.Success) throw new FormatException($"Token does not match DRCode format: ${ctx.Token.Text}");
                return WrapConstant(drcode)(ctx);
            };
        }

        /**
         * Returns the value stored in the register at the referenced AliasValue index
         */
        public static VarFactory<double> Resolve(this VarFactory<AliasValue?> source)
        {
            return (ctx) =>
            {
                var aliasSource = source(ctx);
                Getter<double> getter = (ChipWrapper chip, out double result, bool throwOnError) =>
                {
                    aliasSource(chip, out var aliasValue, throwOnError);
                    if (aliasValue == null)
                    {
                        if (throwOnError)
                        {
                            throw new ExtendedPCException(ctx.LineNumber, $"Could not find register.");
                        }
                        result = default;
                        return false;
                    }
                    if (!aliasValue.Value.Target.HasFlag(AliasTarget.Register))
                    {
                        if (throwOnError)
                        {
                            throw new ExtendedPCException(ctx.LineNumber, $"Alias does not point to a register.");
                        }
                        result = default;
                        return false;
                    }
                    result = chip.Registers[aliasValue.Value.Index];
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
                    throw new ExtendedPCException(ctx.LineNumber, $"recurseCount: expected >= 0, actual {recurseCount}");
                }

                Getter<AliasValue?> getter = (ChipWrapper chip, out AliasValue? result, bool throwOnError) =>
                {
                    if (startIndex < 0 || startIndex >= chip.Registers.Length)
                    {
                        throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, ctx.LineNumber);
                    }

                    int index = startIndex;
                    for (int i = 0; i < recurseCount; i++)
                    {
                        index = (int)chip.Registers[index];

                        if (index < 0 || index >= chip.Registers.Length)
                        {
                            if (throwOnError)
                            {
                                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, ctx.LineNumber);
                            }

                            result = null;
                            return false;
                        }
                    }
                    result = new AliasValue(AliasTarget.Register, index);
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
         *  
         *  Applies styling in CompileContext
         */
        public static VarFactory<AliasValue?> Register(bool allowDeviceComponent = false, bool impliedR = false)
        {
            return (ctx) =>
            {
                // Try to check aliases first
                if (ctx.Context.Aliases.ContainsKey(ctx.Token.Text) 
                    && ctx.Context.Aliases[ctx.Token.Text].Target == AliasTarget.Register)
                {
                    ApplyStyle(ctx, ctx.Line.Display.Theme.Alias);
                    return (ChipWrapper chip, out AliasValue? register, bool throwOnError) =>
                    {
                        try
                        {
                            register = chip.Aliases[ctx.Token.Text];
                            return true;
                        }
                        catch (Exception ex)
                        {
                            if (throwOnError) throw ex;
                            register = null;
                            return false;
                        }
                    };
                }

                // otherwise, parse DR code
                var drCode = new DRCode(ctx.Token.Text);

                if (!drCode.Success)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.LineNumber);
                }
                if (!allowDeviceComponent && drCode.IsDevice)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.LineNumber);
                }

                int startIndex = drCode.Index;
                int recurseCount = drCode.RegCount - (impliedR ? 0 : 1);

                if(recurseCount < 0)
                {
                    throw new ExtendedPCException(ctx.LineNumber, $"Cannot accept a negative recurse count. Did you forget to set 'impliedR:true'?");
                }

                if (drCode.IsDevice)
                {
                    // DR codes with sub-parts (device + optional network) need an extra split to style both segments
                    var anchor = ctx.Token;
                    if (!string.IsNullOrEmpty(drCode.NetworkPart))
                    {
                        if (!anchor.IsSplit)
                        {
                            var parts = ctx.Line.Split(anchor, drCode.DRPart, ":" + drCode.NetworkPart);
                            ((IStyledToken)parts[1][0]).TryConsume().TryStyle(Theme.Default.Device);
                            ((IStyledToken)parts[2][0]).TryConsume().TryStyle(Theme.Default.Device);
                            ((IStyledToken)parts[1][1]).TryConsume().TryStyle(Theme.Default.Network);
                            ((IStyledToken)parts[2][1]).TryConsume().TryStyle(Theme.Default.Network);
                        }
                        else
                        {
                            if (
                                anchor.Children.Length == 2 && 
                                anchor.Children[0].Text == drCode.DRPart && 
                                anchor.Children[1].Text == ":" + drCode.NetworkPart
                            )
                            {
                                ctx.Line.GetTokens(anchor.Children[0], out var devOriginal, out var devDisplay);
                                ctx.Line.GetTokens(anchor.Children[1], out var netOriginal, out var netDisplay);

                                devOriginal.TryConsume().TryStyle(Theme.Default.Device);
                                devDisplay.TryConsume().TryStyle(Theme.Default.Device);
                                netOriginal.TryConsume().TryStyle(Theme.Default.Network);
                                netDisplay.TryConsume().TryStyle(Theme.Default.Network);
                            } else
                            {
                                // explicit no-op. Some other preprocessor already split this token differently than expected
                            }
                        }
                    }
                    else
                    {
                        ctx.Line.Consume(anchor, Theme.Default.Device);
                    }
                }
                else
                {
                    ApplyStyle(ctx, ctx.Line.Display.Theme.Register);
                }

                return Register(startIndex, recurseCount)(ctx);
            };
        }

        public static VarFactory<ILogicable> DeviceByIdLookup(int id, int network = int.MinValue, bool allowNull = false)
        {
            return (ctx) => {
                Getter<ILogicable> getter = (ChipWrapper chip, out ILogicable result, bool throwOnError) =>
                {
                    result = chip.CircuitHousing.GetLogicableFromId(id, network);

                    if (!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.LineNumber);
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
                Getter<ILogicable> getter = (ChipWrapper chip, out ILogicable result, bool throwOnError) =>
                {
                    result = chip.CircuitHousing.GetLogicableFromIndex(index, network);

                    if (!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.LineNumber);
                        }
                        return false;
                    }

                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<ILogicable> Device(bool allowNull = false)
        {
            return (ctx) => {

                // Try to check aliases first.
                if (ctx.Context.Aliases.ContainsKey(ctx.Token.Text)
                    && (AliasTarget.Device | AliasTarget.Register).HasFlag(ctx.Context.Aliases[ctx.Token.Text].Target))
                {
                    ApplyStyle(ctx, ctx.Token.Text == "db" ? ctx.Line.Display.Theme.Device : ctx.Line.Display.Theme.Alias);
                    return (ChipWrapper chip, out ILogicable result, bool throwOnError) =>
                    {
                        if (chip.Aliases.TryGetValue(ctx.Token.Text, out var alias))
                        {
                            if (alias.Target.HasFlag(AliasTarget.Device))
                            {
                                return DeviceByIndexLookup(alias.Index)(ctx)(chip, out result, throwOnError);
                            }
                            if (alias.Target.HasFlag(AliasTarget.Register))
                            {
                                return DeviceByIdLookup(alias.Index)(ctx)(chip, out result, throwOnError);
                            }
                        }
                        if (throwOnError) throw new ExtendedPCException(ctx.LineNumber, "Device not found");
                        result = null;
                        return false;
                    };
                }

                // otherwise, parse DR code
                var drCode = new DRCode(ctx.Token.Text);
                var deviceIdSource = drCode.HasRegisterLookups ? Register(allowDeviceComponent: true).Resolve()(ctx) : WrapConstant<double>(drCode.Index)(ctx);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Device);

                Getter<ILogicable> getter = (ChipWrapper chip, out ILogicable result, bool throwOnError) =>
                {
                    if (!deviceIdSource(chip, out double deviceId, throwOnError))
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.LineNumber);
                        }
                        result = null;
                        return false;
                    }

                    if (drCode.IsDevice)
                    {
                        // device index with optional network index
                        var networkIndex = drCode.Network;
                        result = chip.CircuitHousing.GetLogicableFromIndex((int)deviceId, networkIndex);
                    }
                    else
                    {
                        // device refid
                        result = chip.CircuitHousing.GetLogicableFromId((int)deviceId);
                    }

                    if (!allowNull && result == null)
                    {
                        if (throwOnError)
                        {
                            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, ctx.LineNumber);
                        }
                        return false;
                    }

                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<IMemoryWritable> AsWritable(this VarFactory<ILogicable> source)
        {
            return (ctx) =>
            {
                var lSource = source(ctx);
                return (ChipWrapper chip, out IMemoryWritable writeable, bool throwOnError) =>
                {
                    if (!lSource(chip, out var logicable, throwOnError))
                    {
                        writeable = null;
                        return false;
                    }
                    if (!(logicable is IMemoryWritable memWritable))
                    {
                        writeable = null;
                        if (throwOnError) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, ctx.LineNumber);
                        return false;
                    }
                    writeable = memWritable;
                    return true;
                };
            };
        }

        public static VarFactory<IMemoryReadable> AsReadable(this VarFactory<ILogicable> source)
        {
            return (ctx) =>
            {
                var lSource = source(ctx);
                return (ChipWrapper chip, out IMemoryReadable readable, bool throwOnError) =>
                {
                    if (!lSource(chip, out var logicable, throwOnError))
                    {
                        readable = null;
                        return false;
                    }
                    if (!(logicable is IMemoryReadable memReadable))
                    {
                        readable = null;
                        if (throwOnError) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, ctx.LineNumber);
                        return false;
                    }
                    readable = memReadable;
                    return true;
                };
            };
        }

        public static VarFactory<IMemoryReadWritable> AsReadWritable(this VarFactory<ILogicable> source)
        {
            return (ctx) =>
            {
                var lSource = source(ctx);
                return (ChipWrapper chip, out IMemoryReadWritable result, bool throwOnError) =>
                {
                    if (!lSource(chip, out var logicable, throwOnError))
                    {
                        result = null;
                        return false;
                    }

                    try
                    {
                        result = new MemoryReadWritableWrapper(logicable, ctx.LineNumber);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (throwOnError) throw ex;
                        result = null;
                        return false;
                    }
                };
            };
        }
        #endregion device and register

        #region primitive conversions
        public static VarFactory<object> AsObject<TValue>(this VarFactory<TValue> source)
        {
            return (ctx) =>
            {
                var sourceGetter = source(ctx);
                Getter<object> getter = (ChipWrapper chip, out object result, bool throwOnError) =>
                {
                    sourceGetter(chip, out var t, throwOnError);
                    result = t;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<TValue> As<TValue>(this VarFactory<object> source)
        {
            return ctx =>
            {
                var sourceGetter = source(ctx);
                Getter<TValue> getter = (ChipWrapper chip, out TValue result, bool throwOnError) =>
                {
                    sourceGetter(chip, out var t, throwOnError);
                    result = (TValue)t;
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<double> AsDouble<TValue>(this VarFactory<TValue> source) where TValue : Enum
        {
            return (ctx) =>
            {
                var enumSource = source(ctx);
                Getter<double> getter = (ChipWrapper chip, out double result, bool throwOnError) =>
                {
                    if (!enumSource(chip, out TValue val, throwOnError))
                    {
                        result = -1;
                        return false;
                    }
                    result = int.Parse(val.ToString("d"));
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<long> AsLong(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<long> getter = (ChipWrapper chip, out long result, bool throwOnError) =>
                {
                    if (!doubleSource(chip, out double val, throwOnError))
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
                Getter<int> getter = (ChipWrapper chip, out int result, bool throwOnError) =>
                {
                    if (!doubleSource(chip, out double val, throwOnError))
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

        public static VarFactory<int> AsInt<TValue>(this VarFactory<TValue> source) where TValue : Enum
        {
            return (ctx) =>
            {
                var enumSource = source(ctx);
                Getter<int> getter = (ChipWrapper chip, out int result, bool throwOnError) =>
                {
                    if (!enumSource(chip, out TValue val, throwOnError))
                    {
                        result = -1;
                        return false;
                    }
                    result = int.Parse(val.ToString("d"));
                    return true;
                };
                return getter;
            };
        }

        public static VarFactory<short> AsShort(this VarFactory<double> source)
        {
            return (ctx) => {
                var doubleSource = source(ctx);
                Getter<short> getter = (ChipWrapper chip, out short result, bool throwOnError) =>
                {
                    if (!doubleSource(chip, out double val, throwOnError))
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
                Getter<byte> getter = (ChipWrapper chip, out byte result, bool throwOnError) =>
                {
                    if (!doubleSource(chip, out double val, throwOnError))
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
                Getter<bool> getter = (ChipWrapper chip, out bool result, bool throwOnError) =>
                {
                    if (!doubleSource(chip, out double val, throwOnError))
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
        #endregion primitive conversions

        #region collection primitive conversions
        public static VarFactory<Dictionary<TKey, double>> AsDouble<TKey, TValue>(this VarFactory<Dictionary<TKey, TValue>> source) where TValue : Enum
        {
            return (ctx) =>
            {
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, double>> getter = (ChipWrapper chip, out Dictionary<TKey, double> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var enumDict, throwOnError))
                    {
                        result = enumDict.ToDictionary(kvp => kvp.Key, kvp => (double)int.Parse(kvp.Value.ToString("d")));
                        return true;
                    }
                    result = null;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<Dictionary<TKey, long>> AsLong<TKey>(this VarFactory<Dictionary<TKey, double>> source)
        {
            return (ctx) => {
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, long>> getter = (ChipWrapper chip, out Dictionary<TKey, long> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var doubleDict, throwOnError)) {
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
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, int>> getter = (ChipWrapper chip, out Dictionary<TKey, int> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var doubleDict, throwOnError))
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

        public static VarFactory<Dictionary<TKey, int>> AsInt<TKey, TValue>(this VarFactory<Dictionary<TKey, TValue>> source) where TValue : Enum
        {
            return (ctx) =>
            {
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, int>> getter = (ChipWrapper chip, out Dictionary<TKey, int> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var enumDict, throwOnError))
                    {
                        result = enumDict.ToDictionary(kvp => kvp.Key, kvp => int.Parse(kvp.Value.ToString("d")));
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
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, short>> getter = (ChipWrapper chip, out Dictionary<TKey, short> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var doubleDict, throwOnError))
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
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, byte>> getter = (ChipWrapper chip, out Dictionary<TKey, byte> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var doubleDict, throwOnError))
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
                var dictSource = source(ctx);
                Getter<Dictionary<TKey, bool>> getter = (ChipWrapper chip, out Dictionary<TKey, bool> result, bool throwOnError) =>
                {
                    if (dictSource(chip, out var doubleDict, throwOnError))
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
        #endregion collection primitive conversions

        #region dictionary lookups
        public static VarFactory<T> Lookup<T>(Func<Binding, HashSet<string>> keys, Func<ChipWrapper, Dictionary<string, T>> provider, Func<Binding, Style?> style)
        {
            return (ctx) => {
                if (!keys(ctx).Contains(ctx.Token.Text)) throw new KeyNotFoundException(ctx.Token.Text);

                var retrievedStyle = style(ctx);
                if (retrievedStyle.HasValue) ApplyStyle(ctx, retrievedStyle.Value);

                Getter<T> getter = (ChipWrapper chip, out T result, bool throwOnError) =>
                {
                    var map = provider(chip);
                    if (map.TryGetValue(ctx.Token.Text, out result))
                    {
                        return true;
                    }
                    if (throwOnError) //TODO update this exception to something better?
                    {
                        throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, ctx.LineNumber);
                    }
                    result = default;
                    return false;
                };
                return getter;
            };
        }

        public static VarFactory<double> ConstantLookup()
        {
            return Lookup(ctx => IC10Extender.ConstantValues.Keys.ToHashSet(), chip => IC10Extender.ConstantValues, ctx => Theme.Default.Constant);
        }

        public static VarFactory<double> DefineLookup()
        {
            return Lookup(ctx => ctx.Context.Defines.Keys.ToHashSet(), chip => chip.Defines, ctx => Theme.Default.Define);
        }

        public static VarFactory<int> JumpLookup()
        {
            return Lookup(ctx => ctx.Context.JumpTags.Keys.ToHashSet(), chip => chip.JumpTags, ctx => Theme.Default.Jump);
        }

        public static VarFactory<T> EnumLookup<T>(Func<Binding, Style?> style = null) where T : Enum
        {
            var type = typeof(T);
            return Lookup<T>(
                ctx => Enum.GetNames(type).ToHashSet(),
                chip => ((T[])Enum.GetValues(type))
                    .ToDictionary(t => Enum.GetName(type, t), t => t),
                style ?? ((ctx) => Theme.Default[type.Name, nameof(Theme.Enum)])
            );
        }
        #endregion dictionary lookups

        #region concatenators
        public static VarFactory<HashSet<T>> Concat<T>(params VarFactory<HashSet<T>>[] sources)
        {
            return (ctx) =>
            {
                var getters = sources.Select(src => src(ctx)).ToList();
                return (ChipWrapper chip, out HashSet<T> result, bool throwOnError) =>
                {
                    result = new HashSet<T>();
                    foreach (var source in getters)
                    {
                        if (!source(chip, out var set, throwOnError))
                        {
                            if (throwOnError) throw new ExtendedPCException(ctx.LineNumber, "source failed at runtime");
                            set = new HashSet<T>();
                            return false;
                        }
                        result.AddRange(set);
                    }
                    return true;
                };
            };
        }

        public static VarFactory<HashSet<T>> Concat<T>(this VarFactory<HashSet<T>> first, params VarFactory<HashSet<T>>[] sources)
        {
            return Concat<T>(sources.Prepend(first).ToArray());
        }

        public static VarFactory<Dictionary<TKey, TValue>> Concat<TKey, TValue>(params VarFactory<Dictionary<TKey, TValue>>[] sources)
        {
            return (ctx) => {
                var getters = sources.Select(src => src(ctx)).ToList();
                Getter<Dictionary<TKey, TValue>> getter = (ChipWrapper chip, out Dictionary<TKey, TValue> result, bool throwOnError) =>
                {
                    result = new Dictionary<TKey, TValue>();
                    foreach (var get in getters)
                    {
                        get(chip, out var dict, throwOnError);
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
        #endregion concatenators

        #region single number parsers
        public static VarFactory<double> ParseDouble()
        {
            return (ctx) =>
            {
                var result = double.Parse(ctx.Token.Text);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Double);
                return WrapConstant(result)(ctx);
            };
        }

        public static VarFactory<long> ParseLong()
        {
            return (ctx) =>
            {
                var result = long.Parse(ctx.Token.Text);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Long);
                return WrapConstant(result)(ctx);
            };
        }

        public static VarFactory<int> ParseInt()
        {
            return (ctx) =>
            {
                var result = int.Parse(ctx.Token.Text);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Integer);
                return WrapConstant(result)(ctx);
            };
        }

        public static VarFactory<short> ParseShort()
        {
            return (ctx) =>
            {
                var result = short.Parse(ctx.Token.Text);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Short);
                return WrapConstant(result)(ctx);
            };
        }

        public static VarFactory<byte> ParseByte()
        {
            return (ctx) =>
            {
                var result = byte.Parse(ctx.Token.Text);
                ApplyStyle(ctx, ctx.Line.Display.Theme.Byte);
                return WrapConstant(result)(ctx);
            };
        }
        #endregion single number parsers

        #region binding extensions
        public static Getter<T> Bind<T>(this VarFactory<T> factory, CompileContext ctx, Line line, TokenString.Token token)
        {
            return factory(new Binding(ctx, line, token));
        }

        public static Getter<T> Bind<T>(this Variable<T> variable, CompileContext ctx, Line line, TokenString.Token token)
        {
            return variable.Build.Bind(ctx, line, token);
        }
        #endregion binding extensions
    }
}
