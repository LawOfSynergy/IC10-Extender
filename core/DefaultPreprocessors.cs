using IC10_Extender.Compat;
using IC10_Extender.Preprocessors;

namespace IC10_Extender
{
    internal static class DefaultPreprocessors
    {
        internal static void Register()
        {
            IC10Extender.Register(new CommentPreprocessor());
            IC10Extender.Register(new RawStringPreprocessor(), -1, CompatabilityCheck.AfterStrings, CompatabilityCheck.PreprocessorVariantsEnabled);
            IC10Extender.Register(new StringPreprocessor(), -1, CompatabilityCheck.AfterStrings);
            IC10Extender.Register(new RawHashPreprocessor(), -1, CompatabilityCheck.PreprocessorVariantsEnabled);
            IC10Extender.Register(new HashPreprocessor());
            IC10Extender.Register(new RawBinaryLiteralPreprocessor(), -1, CompatabilityCheck.PreprocessorVariantsEnabled);
            IC10Extender.Register(new BinaryLiteralPreprocessor());
            IC10Extender.Register(new RawHexLiteralPreprocessor(), -1, CompatabilityCheck.PreprocessorVariantsEnabled);
            IC10Extender.Register(new HexLiteralPreprocessor());
            IC10Extender.Register(new LabelPreprocessor());
        }
    }
}
