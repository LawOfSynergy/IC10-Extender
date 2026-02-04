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

    public readonly struct Variable<T>
    {
        public readonly VarFactory<T> Build;
        public readonly HelpString HelpString;

        public Variable(VarFactory<T> build, HelpString helpString)
        {
            Build = build;
            HelpString = helpString;
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

        public Variable<T> Optional()
        {
            return new Variable<T>(Build, HelpString.Optional());
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
