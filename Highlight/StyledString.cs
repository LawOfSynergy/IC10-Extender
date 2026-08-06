using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IC10_Extender;
using IC10_Extender.Variables;
using static IC10_Extender.Highlight.TokenString;

namespace IC10_Extender.Highlight
{
    public interface IThemed
    {
        Theme Theme { get; }
    }

    public interface IReThemeable : IThemed
    {
        void ReTheme(Theme theme);
    }

    public interface IStyled : IThemed
    {
        Style? Style { get; set; }
    }

    public interface IStyledToken : IStyled, IToken { }

    public interface IStyledToken<T, TVirtual> : IStyledToken, IToken<T, TVirtual>
        where T : IStyledToken<T, TVirtual>
        where TVirtual : IStyledToken<TVirtual, TVirtual>
    { }

    public interface IStyledString : IReThemeable, IFormattableString<Tuple<string, Style?>>
    {
        IEnumerable<Tuple<string, Style?>> AllSegments { get; }
    }

    public interface IStyledString<T, TVirtual> : ITokenizedString<T, TVirtual>, IStyledString
        where T : IStyledToken<T, TVirtual>
        where TVirtual : IStyledToken<TVirtual, TVirtual>
    { }

    public class StyledString : TokenizedString<StyledString.StyledToken, StyledString.StyledVirtualToken>, 
        IStyledString<StyledString.StyledToken, StyledString.StyledVirtualToken>
    {
        public static readonly Func<StyledToken, string> TokenVanillaFormat =
            t => t.Style.HasValue ? t.Style.Value.ApplyVanillaFormat(t.Text) : t.Text;

        /// <summary>
        /// Formatter for virtual tokens: walks <see cref="VirtualTokenBase.Leaves"/> and applies
        /// each leaf's style independently.
        /// </summary>
        public static readonly Func<StyledVirtualToken, string> VirtualVanillaFormat =
            t => string.Join("", t.Leaves.Select(l => l.Style.HasValue ? l.Style.Value.ApplyVanillaFormat(l.Text) : l.Text));

        public static readonly Func<IStyledToken, string> VanillaFormat = t =>
        {
            if (t is StyledToken st) return TokenVanillaFormat(st);
            if (t is StyledVirtualToken vt) return VirtualVanillaFormat(vt);
            throw new ArgumentException("Unrecognized token type", nameof(t));
        };

        public Theme Theme { get; private set; }

        public IEnumerable<Tuple<string, Style?>> AllSegments => Unconsumed.Cast<IStyledToken>().Concat(Consumed).Cast<IStyledToken>()
            .OrderBy(t => t.Start).Select(t => new Tuple<string, Style?>(t.Text, t.Style));

        public StyledString(string fullString, Theme theme = null)
            : base(
                fullString,
                (source, original, text, start, segments) =>
                    new StyledToken(
                        source as StyledString ?? throw new ArgumentException("Must be StyledString", nameof(source)),
                        original, text, start, segments),
                (source, original, text) =>
                {
                    var ts = source as StyledString ?? throw new ArgumentException("Expected StyledString instance", nameof(source));
                    if (original is StyledToken mapped) return new StyledVirtualToken(ts, mapped, text);
                    if (original is StyledVirtualToken node) return new StyledVirtualToken(ts, node, text);
                    throw new ArgumentException("Unrecognized type", nameof(original));
                })
        {
            
            ReTheme(theme);
        }

        /// <summary>
        /// Themes tokens built by the base ctor.
        /// Non-virtual: called from a constructor.
        /// </summary>
        /// <param name="theme">The new theme to resolve all token styles against</param>
        public void ReTheme(Theme theme)
        {
            var target = theme ?? Theme.Default;
            foreach (var token in consumed.Concat(unconsumed).Cast<IStyledToken>())
            {
                token.Style = target[token.Style];
            }
            // ReTheme virtual roots — walk leaves since only leaves carry meaningful style.
            foreach (var token in consumed.OfType<StyledVirtualToken>())
            {
                foreach (var leaf in token.Leaves)
                {
                    leaf.Style = target[leaf.Style];
                }
            }
            Theme = target;
        }

        public string ToString(Func<IStyledToken, string> formatter)
        {
            var sorted = Consumed.Cast<IStyledToken>().Concat(Unconsumed.Cast<IStyledToken>())
                .OrderBy(t => t.Start).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (var token in sorted)
            {
                sb.Append(formatter(token));
            }
            return sb.ToString();
        }

        public string ToString(Func<Tuple<string, Style?>, string> formatter)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var token in AllSegments)
            {
                sb.Append(formatter(token));
            }
            return sb.ToString();
        }

        public class StyledToken : TokenBase, IStyledToken<StyledToken, StyledVirtualToken>
        {
            public new StyledString Source => base.Source as StyledString;
            public Style? Style { get; set; }
            public Theme Theme => Source.Theme;

            public StyledToken(StyledString source, StyledToken original, string text, Index<OriginalSpace> start, IEnumerable<Segment<OriginalSpace>> segments)
                : base(source, original, text, start, segments)
            {
                // Inherit verbatim; Source.Theme is unset during base construction.
                Style = original?.Style;
            }
        }

        public class StyledVirtualToken : VirtualTokenBase, IStyledToken<StyledVirtualToken, StyledVirtualToken>
        {
            public new StyledString Source => base.Source as StyledString;
            private Style? style;
            public Style? Style { get => style ?? Parent?.Style; set => style = value; }
            public Theme Theme => Source.Theme;

            /// <summary>Constructs a virtual root node.</summary>
            public StyledVirtualToken(
                StyledString source,
                StyledToken original,
                string text)
                : base(source, original, text)
            { }

            /// <summary>Constructs a virtual sub-node.</summary>
            public StyledVirtualToken(StyledString source, StyledVirtualToken parent, string text)
                : base(source, parent, text)
            { }

            public new StyledVirtualToken[] Leaves =>
                base.Leaves.Cast<StyledVirtualToken>().ToArray();
        }
    }
}
