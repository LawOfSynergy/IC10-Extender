using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlighters;
using IC10_Extender.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IC10_Extender.Variables
{

    public delegate bool Getter<T>(out T value, bool throwOnError = true);
    /**
     * Any checks for whether the token is valid should be done in this function, not in the Getter itself.
     * This way, these errors can be reported at compile time rather than runtime.
     * If you are wrapping a var factory, make sure to unwrap the getter (i.e. call the supplied VarFactory 
     * with the binding to get the actual Getter) here so that compile errors correctly propogate at compile time.
     * 
     */
    public delegate Getter<T> VarFactory<T>(Binding binding);

    public struct Variable<T>
    {
        public readonly VarFactory<T> Build;
        public readonly HelpString HelpString;
        public readonly bool IsOptional;
        public readonly T Default;

        public Variable(VarFactory<T> build, HelpString helpString, bool isOptional = false, T defaultVal = default)
        {
            Build = build;
            HelpString = helpString;
            IsOptional = isOptional;
            Default = defaultVal;
        }

        public static implicit operator VarFactory<T>(Variable<T> variable)
        {
            return variable.Build;
        }

        public static implicit operator HelpString(Variable<T> variable)
        {
            return variable.HelpString;
        }

        public Variable<T> Named(string name)
        {
            return new Variable<T>(Build, HelpString.Var(name));
        }

        public Variable<T> Optional(T defaultValue = default)
        {
            return new Variable<T>(Build, HelpString.Optional(), true, defaultValue);
        }

        public Variable<T> Strip(string newColor = null)
        {
            return new Variable<T>(Build, HelpString.Strip());
        }

        public static Variable<T> operator +(Variable<T> left, HelpString right)
        {
            return new Variable<T>(left.Build, left.HelpString + right);
        }

        public static Variable<T> operator +(HelpString left, Variable<T> right)
        {
            return new Variable<T>(right.Build, left + right.HelpString);
        }
    }

    public class Signature
    {
        private List<dynamic> requiredVars = new List<dynamic>();
        private List<dynamic> optionalVars = new List<dynamic>();
        private List<dynamic> varArgVars = new List<dynamic>();

        public List<dynamic> RequiredVars => requiredVars.ToList();
        public List<dynamic> OptionalVars => optionalVars.ToList();
        public List<dynamic> VarArgVars => varArgVars.ToList();

        public Signature Required<T>(Variable<T> variable)
        {
            requiredVars.Add(variable);
            return this;
        }

        public Signature Optional<T>(Variable<T> variable)
        {
            if (varArgVars.Count > 0) throw new InvalidOperationException("VarArgs and Optional are mutually exclusive");
            optionalVars.Add(variable.Optional());
            return this;
        }

        public Signature VarArgs<T>(Variable<T> variable)
        {
            if (optionalVars.Count > 0) throw new InvalidOperationException("VarArgs and Optional are mutually exclusive");
            varArgVars.Add(variable);
            return this;
        }

        public BoundSignature Bind(ChipWrapper chip, int lineNumber, string[] tokens, StyledLine highlightCtx)
        {
            return new BoundSignature(this, chip, lineNumber, tokens, highlightCtx);
        }
    }

    public class BoundSignature
    {
        public Signature Signature { get; private set; }
        public ChipWrapper Chip { get; private set; }
        public int LineNumber { get; private set; }

        private readonly string[] tokens;
        public string[] Tokens => tokens.ToArray();



        public BoundSignature(Signature signature, ChipWrapper chip, int lineNumber, string[] tokens, StyledLine highlightCtx)
        {
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            Chip = chip ?? throw new ArgumentNullException(nameof(chip));
            LineNumber = lineNumber;
            this.tokens = tokens ?? new string[0];

            var required = signature.RequiredVars;
            var optional = signature.OptionalVars;
            var varArgs = signature.VarArgVars;
            
            if (highlightCtx == null) throw new ArgumentNullException(nameof(highlightCtx));

            if (tokens.Length < required.Count) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
            if (optional.Count > 0 && varArgs.Count > 0) throw new InvalidOperationException("Optional and VarArgs are mutually exclusive");
            if (optional.Count > 0 && tokens.Length > (required.Count + optional.Count)) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
            if (varArgs.Count > 0 && ((tokens.Length - required.Count) % varArgs.Count) != 0) throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);

        }
    }
}
