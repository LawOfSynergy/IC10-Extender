using System.Text.RegularExpressions;

namespace IC10_Extender.Variables
{
    public readonly struct DRCode
    {
        public static readonly Regex Pattern = new Regex(@"^((?<device>d)(?<self>b)(:(?<network>[0-9]+))?|(?<device>d)(?<register>r*)(?<index>[0-9]+)(:(?<network>[0-9]+))?|(?<register>r*)(?<index>[0-9]+))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public readonly string Code; //the full token originally passed in
        public readonly string DRPart; // e.g. db | d6 | dr16 | drr2 | r5 | rr3
        public readonly string NetworkPart; // part after the ":" (excluded) e.g. :123
        public readonly bool Success;
        public readonly bool Self;
        public readonly bool IsDevice;
        public readonly int RegCount;
        public readonly int Index;
        public readonly int Network;

        public bool HasRegisterLookups { get { return RegCount > 0; } }
        public bool ImpliedRegisterLookup { get { return RegCount == 0 && !IsDevice; } }

        public DRCode(string code)
        {
            this.Code = code;

            var results = Pattern.Match(code);
            Success = results.Success;
            if (Success)
            {
                var self = results.Groups["self"];
                var device = results.Groups["device"];
                var register = results.Groups["register"];
                var index = results.Groups["index"];
                var network = results.Groups["network"];

                this.DRPart = device.Value + self.Value + register.Value + index.Value;
                this.NetworkPart = network.Value;

                this.Self = self.Length > 0;
                IsDevice = results.Groups["device"].Length > 0;
                RegCount = register.Length;
                this.Index = this.Self ? int.MaxValue : int.Parse(index.Value);
                this.Network = network.Success ? int.Parse(network.Value) : int.MinValue;
            } else {
                DRPart = "";
                NetworkPart = "";
                Self = default;
                IsDevice = default;
                RegCount = default;
                Index = default;
                Network = int.MinValue;
            }
        }
    }
}
