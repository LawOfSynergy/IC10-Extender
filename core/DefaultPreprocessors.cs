using IC10_Extender.Compat;
using IC10_Extender.Preprocessors;

namespace IC10_Extender
{
    internal static class DefaultPreprocessors
    {
        internal static void Register()
        {
            IC10Extender.Register(new CommentPreprocessor());
            IC10Extender.Register(new RawStringPreprocessor(), accept: CompatabilityCheck.AfterStrings);
            IC10Extender.Register(new StringPreprocessor(), accept: CompatabilityCheck.AfterStrings);
            IC10Extender.Register(new RawHashPreprocessor());
            IC10Extender.Register(new HashPreprocessor());
            IC10Extender.Register(new RawBinaryLiteralPreprocessor());
            IC10Extender.Register(new BinaryLiteralPreprocessor());
            IC10Extender.Register(new RawHexLiteralPreprocessor());
            IC10Extender.Register(new HexLiteralPreprocessor());
            IC10Extender.Register(new LabelPreprocessor());
        }
    }
}
