using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Util;
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
            .OrderBy(t => t.StartIndex).Select(t => new Tuple<string, Style?>(t.Text, t.Style));

        public StyledString(string fullString, Theme theme = null)
            : base(
                fullString,
                (source, original, text, startIndex, segments) =>
                    new StyledToken(
                        source as StyledString ?? throw new ArgumentException("Must be StyledString", nameof(source)),
                        original, text, startIndex, segments),
                (source, original, text) =>
                {
                    var ts = source as StyledString ?? throw new ArgumentException("Expected StyledString instance", nameof(source));
                    if (original is StyledToken mapped) return new StyledVirtualToken(ts, mapped, text);
                    if (original is StyledVirtualToken node) return new StyledVirtualToken(ts, node, text);
                    throw new ArgumentException("Unrecognized type", nameof(original));
                })
        {
            Theme = theme ?? Theme.Default;
        }

        public void ReTheme(Theme theme)
        {
            Theme = theme ?? Theme.Default;
            foreach (var token in consumed.Concat(unconsumed).Cast<IStyledToken>())
            {
                token.Style = Theme[token.Style];
            }
            // ReTheme virtual roots — walk leaves since only leaves carry meaningful style.
            foreach (var token in consumed.OfType<StyledVirtualToken>())
            {
                foreach (var leaf in token.Leaves)
                {
                    leaf.Style = Theme[leaf.Style];
                }
            }
        }

        public string ToString(Func<IStyledToken, string> formatter)
        {
            var sorted = Consumed.Cast<IStyledToken>().Concat(Unconsumed.Cast<IStyledToken>())
                .OrderBy(t => t.StartIndex).ToList();
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

            public StyledToken(StyledString source, StyledToken original, string text, int startIndex, IEnumerable<Segment> segments)
                : base(source, original, text, startIndex, segments)
            {
                Style = Theme[original?.Style];
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
