namespace IC10_Extender.Variables
{

    public delegate bool Getter<T>(out T value, bool throwOnError = true);
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
