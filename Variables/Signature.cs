using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using IC10_Extender.Highlight;

namespace IC10_Extender.Variables
{
    public class Signature
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        private readonly List<dynamic> required = new List<dynamic>();
        private readonly List<dynamic> optional = new List<dynamic>();
        private readonly List<dynamic> varargs = new List<dynamic>();

        public string Name { get; private set; }
        public Style? Style { get; set; }
        public List<dynamic> RequiredVars => required.ToList();
        public List<dynamic> OptionalVars => optional.ToList();
        public List<dynamic> VarArgVars => varargs.ToList();

        private bool locked = false;
        public bool Locked => locked;
        public bool RequiresVarArgsAtLeastOnce { get; private set; }

        public bool IsVanilla { get; private set; }

        public Signature(string name, bool isVanilla = false)
        {
            Name = name;
            IsVanilla = isVanilla;
        }

        public Signature(Signature original, string name, bool isVanilla = false)
        {
            Name = name;
            IsVanilla = isVanilla;

            required = new List<dynamic>(original.RequiredVars);
            optional = new List<dynamic>(original.OptionalVars);
            varargs = new List<dynamic>(original.VarArgVars);
        }

        public Signature Clone(string newName)
        {
            return new Signature(this, newName);
        }

        public Signature WithStyle(Style? style)
        {
            Style = style;
            return this;
        }

        //Throws ArgumentException if a variable that is otherwise marked as optional is passed into here
        public Signature Required<T>(params Variable<T>[] vars)
        {
            if (locked) throw new InvalidOperationException("This signature has already been locked and is now readonly");
            foreach (var v in vars)
            {
                if (v.IsOptional) throw new ArgumentException("Required variable cannot be marked optional");
                required.Add(v);
            }

            return this;
        }

        // defaultValue is the default used for any variables that are not already marked optional
        public Signature Optional<T>(T defaultValue, Variable<T>[] vars)
        {
            if (locked) throw new InvalidOperationException("This signature has already been locked and is now readonly");
            if (varargs.Count > 0) throw new InvalidOperationException("Optional and VarArgs are mutually exclusive");

            foreach (var v in vars)
            {
                optional.Add(v.IsOptional ? v : v.Optional(defaultValue));
            }

            return this;
        }

        public Signature Optional<T>(T defaultValue, Variable<T> first, params Variable<T>[] rest)
        {
            return Optional(defaultValue, rest.Prepend(first).ToArray());
        }

        public Signature Optional<T>(params Variable<T>[] vars)
        {
            return Optional(default, vars);
        }

        public Signature VarArgs<T>(params Variable<T>[] vars)
        {
            if (locked) throw new InvalidOperationException("This signature has already been locked and is now readonly");
            if (optional.Count > 0) throw new InvalidOperationException("Optional and VarArgs are mutually exclusive");

            foreach (var v in vars)
            {
                if (v.IsOptional) throw new ArgumentException("VarArgs variable cannot be marked optional");
                varargs.Add(v);
            }

            return this;
        }

        public Signature RequireVarArgsAtLeastOnce()
        {
            if (locked) throw new InvalidOperationException("This signature has already been locked and is now readonly");
            RequiresVarArgsAtLeastOnce = true;
            return this;
        }

        public Signature Lock()
        {
            locked = true;
            return this;
        }

        public virtual BoundSignature Bind(CompileContext ctx, Line line, IToken[] tokens)
        {
            Lock();
            return new BoundSignature(this, ctx, line, tokens);
        }

        public string CommandExample(Theme theme, int spaceCount = 0)
        {
            var paramList = required.Concat(optional);
            if (RequiresVarArgsAtLeastOnce) paramList = paramList.Concat(varargs);

            List<HelpString> result = new List<HelpString>();
            if (spaceCount == 0) result.Add(new HelpString(theme, Name, Style));

            result.AddRange(paramList.Skip(spaceCount).Select(v => (HelpString)v.HelpString));
            int remainder = spaceCount - paramList.Count();
            if (remainder > 0)
            {
                result.AddRange(varargs.Skip(remainder % varargs.Count()).Select(v => (HelpString)v.HelpString));
            }

            return string.Join(" ", result);
        }
    }

    public class UnknownSignature : Signature
    {
        private bool unrestricted = false;
        public UnknownSignature(ScriptCommand opcode) : base(Enum.GetName(typeof(ScriptCommand), opcode))
        {
            try
            {
                var tokens = ProgrammableChip.GetCommandExample(opcode).Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in tokens)
                {
                    Required(Variables.Interpret(t));
                }
            }
            // if either of these, we are probably in a unit test context. 
            catch (SecurityException)
            {
                unrestricted = true;
            }
            catch (TypeInitializationException ex) when (ex.InnerException is SecurityException)
            {
                unrestricted = true;
            }
        }

        public override BoundSignature Bind(CompileContext ctx, Line line, IToken[] tokens)
        {
            if (unrestricted) return new UnrestrictedBoundSignature(this, ctx, line, tokens);
            return base.Bind(ctx, line, tokens);
        }
    }

    //This class is only for unit testing. It gets created when a SecurityException by GetCommandExample() while trying to bind UnknownSignature
    internal class UnrestrictedBoundSignature : BoundSignature
    {
        public UnrestrictedBoundSignature(UnknownSignature signature, CompileContext ctx, Line line, IToken[] tokens) : base(signature, ctx, line, tokens)
        {
            foreach (var token in tokens)
            {
                mainGetters.Add(VarUtils.Identity().AsObject()(new Binding(ctx, line, token)));
            }
        }
    }

    public class BoundSignature
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        public Signature Signature { get; private set; }
        public CompileContext Context { get; private set; }
        public Line Line { get; private set; }

        private readonly IToken[] tokens;
        public IToken[] Tokens => tokens.ToArray();

        protected readonly List<dynamic> mainGetters;
        protected readonly List<List<dynamic>> varArgGetters = new List<List<dynamic>>();

        public List<dynamic> MainCaptures => mainGetters.ToList();
        public List<List<dynamic>> VarArgCaptures => varArgGetters.ToList();

        public BoundSignature(Signature signature, CompileContext ctx, Line line, IToken[] tokens)
        {
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Line = line ?? throw new ArgumentNullException(nameof(line));
            this.tokens = tokens ?? new IToken[0];

            var lineNumber = line.LineNumber;

            var required = signature.RequiredVars;
            var optional = signature.OptionalVars;
            var varArgs = signature.VarArgVars;

            Log.Debug($"tokens: {tokens.Length}, required: {required.Count}, optional:{optional.Count}, varArgs:{varArgs.Count}");

            if (tokens.Length < required.Count) throw StyleError(ctx, line, new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, line.LineNumber));
            if (optional.Count > 0 && varArgs.Count > 0) throw StyleError(ctx, line, new InvalidOperationException("Optional and VarArgs are mutually exclusive"));
            if (varArgs.Count == 0 && tokens.Length > (required.Count + optional.Count)) throw StyleError(ctx, line, new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber));
            if (varArgs.Count > 0 && ((tokens.Length - required.Count) % varArgs.Count) != 0) throw StyleError(ctx, line, new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber));

            var hasErrors = false;

            // Consume the opcode token in Original and Display. The opcode token was already consumed
            // in Raw by Tokenize(), so Line.Consume handles the already-consumed case via Locate().
            var opcodeToken = line.Raw.Locate(signature.Name);
            if (opcodeToken != null) line.Consume(opcodeToken, signature.Style.Value);

            mainGetters = required.Concat(optional)
                .Associate(tokens.Take(required.Count + optional.Count))
                .Select((tuple) =>
                {
                    try
                    {
                        return tuple.Item1.Build(new Binding(ctx, line, tuple.Item2));
                    }
                    catch (Exception ex)
                    {
                        hasErrors = true;
                        Log.Debug($"{ex.GetType().Name}: {ex.Message}");
                        StyleError(ctx, line, ex, tuple.Item2);
                        return default;
                    }
                })
                .ToList();
            var remainingTokens = tokens.Skip(required.Count + optional.Count);

            if (varArgs.Count > 0)
            {
                while (remainingTokens.Count() > 0)
                {
                    varArgGetters.Add(
                        varArgs.Associate(remainingTokens.Take(varArgs.Count))
                        .Select((tuple) =>
                        {
                            try
                            {
                                return tuple.Item1.Build(new Binding(ctx, line, tuple.Item2));
                            }
                            catch (Exception ex)
                            {
                                hasErrors = true;
                                Log.Debug($"{ex.GetType().Name}: {ex.Message}");
                                StyleError(ctx, line, ex, tuple.Item2);
                                return default;
                            }
                        }).ToList()
                    );
                    remainingTokens = remainingTokens.Skip(varArgs.Count);
                }
            }

            if (signature.RequiresVarArgsAtLeastOnce && varArgGetters.Count < 1) throw StyleError(ctx, line, new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber));

            if (hasErrors) throw new InvalidOperationException("Compile errors were encountered while binding signature");
        }

        private static T StyleError<T>(CompileContext context, Line line, T ex, IToken token = null) where T : Exception
        {
            // Fall back to the remainder anchor when no specific token is provided (argument count errors etc.)
            var anchor = token ?? line.Raw.Locate(line.Raw.Remainder, useTokenBounds: false);
            int column = anchor != null ? anchor.AdjustedStartIndex : 0;
            context.AddError(line, column, ex.Wrap(line.LineNumber));
            if (anchor != null) line.Consume(anchor, Theme.Default.Error.WithError(ex));
            return ex;
        }

        private T Resolve<T>(Getter<T> getter, ChipWrapper chip)
        {
            getter(chip, out var result);
            return result;
        }

        private T Take<T>(IEnumerable<object> getters, out IEnumerable<object> remainder, ChipWrapper chip)
        {
            remainder = getters.Skip(1);
            return Resolve<T>((Getter<T>)getters.First(), chip);
        }

        private Tuple<T1> Fill<T1>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1>(
                Take<T1>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2> Fill<T1, T2>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3> Fill<T1, T2, T3>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3, T4> Fill<T1, T2, T3, T4>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3, T4>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out getters, chip),
                Take<T4>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3, T4, T5> Fill<T1, T2, T3, T4, T5>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3, T4, T5>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out getters, chip),
                Take<T4>(getters, out getters, chip),
                Take<T5>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3, T4, T5, T6> Fill<T1, T2, T3, T4, T5, T6>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out getters, chip),
                Take<T4>(getters, out getters, chip),
                Take<T5>(getters, out getters, chip),
                Take<T6>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3, T4, T5, T6, T7> Fill<T1, T2, T3, T4, T5, T6, T7>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out getters, chip),
                Take<T4>(getters, out getters, chip),
                Take<T5>(getters, out getters, chip),
                Take<T6>(getters, out getters, chip),
                Take<T7>(getters, out _, chip)
            );
        }

        private Tuple<T1, T2, T3, T4, T5, T6, T7, T8> Fill<T1, T2, T3, T4, T5, T6, T7, T8>(IEnumerable<dynamic> getters, ChipWrapper chip)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, T8>(
                Take<T1>(getters, out getters, chip),
                Take<T2>(getters, out getters, chip),
                Take<T3>(getters, out getters, chip),
                Take<T4>(getters, out getters, chip),
                Take<T5>(getters, out getters, chip),
                Take<T6>(getters, out getters, chip),
                Take<T7>(getters, out getters, chip),
                Take<T8>(getters, out _, chip)
            );
        }

        public T1 Get<T1>(ChipWrapper chip) => Fill<T1>(mainGetters, chip).Item1;
        public Tuple<T1, T2> Get<T1, T2>(ChipWrapper chip) => Fill<T1, T2>(mainGetters, chip);
        public Tuple<T1, T2, T3> Get<T1, T2, T3>(ChipWrapper chip) => Fill<T1, T2, T3>(mainGetters, chip);
        public Tuple<T1, T2, T3, T4> Get<T1, T2, T3, T4>(ChipWrapper chip) => Fill<T1, T2, T3, T4>(mainGetters, chip);
        public Tuple<T1, T2, T3, T4, T5> Get<T1, T2, T3, T4, T5>(ChipWrapper chip) => Fill<T1, T2, T3, T4, T5>(mainGetters, chip);
        public Tuple<T1, T2, T3, T4, T5, T6> Get<T1, T2, T3, T4, T5, T6>(ChipWrapper chip) => Fill<T1, T2, T3, T4, T5, T6>(mainGetters, chip);
        public Tuple<T1, T2, T3, T4, T5, T6, T7> Get<T1, T2, T3, T4, T5, T6, T7>(ChipWrapper chip) => Fill<T1, T2, T3, T4, T5, T6, T7>(mainGetters, chip);
        public Tuple<T1, T2, T3, T4, T5, T6, T7, T8> Get<T1, T2, T3, T4, T5, T6, T7, T8>(ChipWrapper chip) => Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip);

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11, T12>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11, T12, T13>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11, T12, T13, T14>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14, T15>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14, T15>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11, T12, T13, T14, T15>(mainGetters.Skip(8), chip));
        }

        public Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14, T15, T16>> Get<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ChipWrapper chip)
        {
            return new Tuple<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, Tuple<T9, T10, T11, T12, T13, T14, T15, T16>>(Fill<T1, T2, T3, T4, T5, T6, T7, T8>(mainGetters, chip), Fill<T9, T10, T11, T12, T13, T14, T15, T16>(mainGetters.Skip(8), chip));
        }

        public Tuple<T1>[] GetVarArgs<T1>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1>(set, chip)).ToArray();
        public Tuple<T1, T2>[] GetVarArgs<T1, T2>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2>(set, chip)).ToArray();
        public Tuple<T1, T2, T3>[] GetVarArgs<T1, T2, T3>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3>(set, chip)).ToArray();
        public Tuple<T1, T2, T3, T4>[] GetVarArgs<T1, T2, T3, T4>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3, T4>(set, chip)).ToArray();
        public Tuple<T1, T2, T3, T4, T5>[] GetVarArgs<T1, T2, T3, T4, T5>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3, T4, T5>(set, chip)).ToArray();
        public Tuple<T1, T2, T3, T4, T5, T6>[] GetVarArgs<T1, T2, T3, T4, T5, T6>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3, T4, T5, T6>(set, chip)).ToArray();
        public Tuple<T1, T2, T3, T4, T5, T6, T7>[] GetVarArgs<T1, T2, T3, T4, T5, T6, T7>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3, T4, T5, T6, T7>(set, chip)).ToArray();
        public Tuple<T1, T2, T3, T4, T5, T6, T7, T8>[] GetVarArgs<T1, T2, T3, T4, T5, T6, T7, T8>(ChipWrapper chip) => varArgGetters.Select(set => Fill<T1, T2, T3, T4, T5, T6, T7, T8>(set, chip)).ToArray();
    }
}