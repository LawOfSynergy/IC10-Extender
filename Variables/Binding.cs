using System;
using IC10_Extender.Highlight;

namespace IC10_Extender.Variables
{
    /// <summary>
    /// Carries the context for a single argument position during signature binding.
    /// <para>
    /// The <see cref="Token"/> is the live token object from <c>Raw</c>. <see cref="ApplyStyle"/>
    /// can then pass it directly to <see cref="Line.Consume(TokenString.Token, Style, string, bool, bool)"/>
    /// without re-searching, correctly handling tokens that were already consumed by a preprocessor.
    /// </para>
    /// </summary>
    public struct Binding
    {
        public readonly CompileContext Context;
        public readonly Line Line;

        /// <summary>
        /// The <see cref="TokenString.Token"/> from <c>Raw</c> that corresponds to this binding
        /// position
        /// </summary>
        public readonly IToken Token;

        public int LineNumber => Line.LineNumber;

        /// <summary>
        /// Constructs a binding from a live <see cref="TokenString.Token"/> produced by
        /// <see cref="TokenizedString{T}.Tokenize"/>. <see cref="Token"/> and
        /// <see cref="OriginalToken"/> are set from <see cref="TokenString.Token.Text"/>.
        /// </summary>
        public Binding(CompileContext context, Line line, IToken anchor)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Line = line ?? throw new ArgumentNullException(nameof(line));
            Token = anchor ?? throw new ArgumentNullException(nameof(anchor));
        }
    }
}
