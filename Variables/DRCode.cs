using System.Text.RegularExpressions;

namespace IC10_Extender.Variables
{
    public readonly struct DRCode
    {
        public static readonly Regex Pattern = new Regex(@"^(?:(?<device>d)(?<self>b)(?::(?<network>[0-9]+))?|(?<device>d)(?<register>r*)(?<index>[0-9]+)(?::(?<network>[0-9]+))?|(?<register>r*)(?<index>[0-9]+))$", RegexOptions.Compiled);

        public readonly string code;
        public readonly bool success;
        public readonly bool self;
        public readonly bool isDevice;
        public readonly int regCount;
        public readonly int index;
        public readonly int network;

        public bool HasRegisterLookups { get { return regCount > 0; } }
        public bool ImpliedRegisterLookup { get { return regCount == 0 && !isDevice; } }

        public DRCode(string code)
        {
            this.code = code;

            var results = Pattern.Match(code);
            success = results.Success;
            if (success)
            {
                self = results.Groups["self"].Length > 0;
                isDevice = results.Groups["device"].Length > 0;
                regCount = results.Groups["register"].Length;
                index = self ? int.MaxValue : int.Parse(results.Groups["index"].Value);
                network = results.Groups["network"].Success ? int.Parse(results.Groups["network"].Value) : int.MinValue;
            } else {
                self = default;
                isDevice = default;
                regCount = default;
                index = default;
                network = int.MinValue;
            }
        }
    }
}
