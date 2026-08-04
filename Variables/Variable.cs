using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlight;
using IC10_Extender.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using static IC10_Extender.Variables.VarUtils;

namespace IC10_Extender.Variables
{

    public delegate bool Getter<T>(ChipWrapper chip, out T value, bool throwOnError = true);
    
    /**
     * Any checks for whether the token is valid should be done in this function, not in the Getter itself.
     * This way, these errors can be reported at compile time rather than runtime.
     * 
     * If you are going to do token styling, that should occur at this step, or during execution of a Styler.
     * 
     * If you are wrapping a var factory, make sure to unwrap the getter (i.e. call the supplied VarFactory 
     * with the binding to get the actual Getter) here so that compile errors correctly propogate at compile time.
     */
    public delegate Getter<T> VarFactory<T>(Binding binding);

    public struct Variable<T>
    {
        public readonly VarFactory<T> Build;
        public readonly HelpString HelpString;
        public readonly bool IsOptional;
        public readonly T Default;

        public Variable(VarFactory<T> build, HelpString helpString, bool isOptional = false, T defaultValue = default)
        {
            Build = isOptional ? DefaultIfEmpty(build, defaultValue) : build;
            HelpString = helpString;
            IsOptional = isOptional;
            Default = defaultValue;
        }

        public static implicit operator VarFactory<T>(Variable<T> variable)
        {
            return variable.Build;
        }

        public static implicit operator HelpString(Variable<T> variable)
        {
            return variable.HelpString;
        }

        public Variable<T> Named(string name, Theme theme = null)
        {
            theme = theme ?? Theme.Default;
            return new Variable<T>(Build, HelpString.Var(name, theme.VarName, theme.VarParentheses));
        }

        public Variable<T> Optional(T defaultValue = default, Theme theme = null)
        {
            theme = theme ?? Theme.Default;
            return new Variable<T>(Build, HelpString.Optional(theme.VarBrackets), true, defaultValue);
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
}
