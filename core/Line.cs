using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IC10_Extender.Highlight;
using IC10_Extender.Operations;

namespace IC10_Extender
{
    public class Line
    {

        /**
         * The line that this expanded from due to preprocessing
         */
        public readonly Line OriginatingLine;

        /**
         * The string as it was when this line was first wrapped. This line may be displayed in the "before"
         * window when using a mod such as IC10 Editor
         */
        public IStyledString<StyledString.StyledToken, StyledString.StyledVirtualToken> Original { get; private set; }

        /**
         * The string as it should be stored in the chip and displayed in the UI. The text in this field is 
         * what will be used any time the chip's code is reloaded or recompiled.
         */
        public IStyledString<StyledString.StyledToken, StyledString.StyledVirtualToken> Display { get; private set; }

        /**
         * The string on which processing is performed. Changes to this string are not inherently reflected in Display,
         * which must be updated separately if desired.
         *
         * This string explicitly contains the results of the current round of processing. If the chip gets recompiled
         * or reloaded, this string will be discarded and replaced with the contents of Display as it was saved in the
         * previous compilation, and preprocessing will start over from there.
         */
        public IPreprocessableString<TokenString.Token, TokenString.VirtualToken> Raw { get; private set; }

        /**
         * This is the string that the line gets reset to when a new CompileContext frame is recursed into due to line count edits.
         *
         * Changes to raw do not carry into the new context unless the change has also been made to this string. This is usually
         * done by passing permanent:true to the various modifying methods.
         *
         * Reset() rebuilds Raw, Display, and Original from ResetRaw.Remainder (the current un-consumed text of ResetRaw),
         * which correctly reflects any permanent writes applied during preprocessing. ResetRaw itself is not reinitialized
         * during Reset(), preserving it as the stable baseline for the context frame.
         */
        public ITokenizedString<TokenString.Token, TokenString.VirtualToken> ResetRaw { get; private set; }

        /**
         * The current line number after preprocessing and line removal. This is used for jumps and line references.
         */
        public int LineNumber;

        public int OriginatingLineNumber => OriginatingLine == null ? LineNumber : OriginatingLine.OriginatingLineNumber;

        /**
         * If this field is non-null, it indicates that this line should be treated as if it were of the specified
         * operation type, regardless of what the Raw string indicates after preprocessing.
         */
        public Operation ForcedOp;

        /**
         * A delegate that is called before the line is executed. This can be used to modify the chip's state or
         * perform other actions.
         */
        public PreExecute PreExecute;

        /**
         * A delegate that is called after the line is executed. This can be used to modify the chip's state or
         * perform other actions.
         */
        public PostExecute PostExecute;

        public Line(string raw, int lineNumber, Line originatingLine = null, Operation forcedOp = null, Theme theme = null)
        {
            OriginatingLine = originatingLine;
            Original = new StyledString(raw, theme);
            Display = new StyledString(raw, theme);
            Raw = new TokenString(raw);
            ResetRaw = new TokenString(raw);

            LineNumber = lineNumber;

            ForcedOp = forcedOp;
            PreExecute = IC10Extender.NoOpPreExecute;
            PostExecute = IC10Extender.NoOpPostExecute;
        }

        public (IStyledToken original, IStyledToken display) GetTokens(IToken anchor)
        {
            return (Original.FromSegments(anchor.Provenance) as IStyledToken, Display.FromSegments(anchor.Provenance) as IStyledToken);
        }

        public IToken Consume(IToken anchor, Style style, string replace = "", bool write = false)
        {
            var (original, display) = GetTokens(anchor);
            original.TryConsume().TryStyle(style);
            display.TryConsume().TryStyle(style);
            if (write) return anchor.TryReplaceOrRemove(replace);
            return anchor;
        }

        public IToken Replace(IToken anchor, Style style, string replace, bool write = false)
        {
            var (original, display) = GetTokens(anchor);
            original.TryConsume().TryStyle(style);
            display.TryReplace(replace).TryStyle(style);
            if (write) return anchor.TryReplace(replace);
            return anchor;
        }

        public IToken Remove(IToken anchor, bool write)
        {
            var (original, display) = GetTokens(anchor);
            original.TryRemove();
            display.TryRemove();
            if (write) return anchor.TryRemove();
            return anchor;
        }

        public (IToken[] Raw, IStyledToken[] Original, IStyledToken[] Display) Split(IToken anchor, params string[] fragments)
        {
            var (original, display) = GetTokens(anchor);
            return (anchor.TrySplit(fragments), original.TrySplit(fragments), display.TrySplit(fragments));
        }

        /**
         * Convenience overload that anchors on Raw.Remainder internally. There is only ever one meaningful
         * remainder anchor, so exposing it to the caller adds no useful control.
         */
        public IToken ConsumeRemainder(Style style, bool write = false)
        {
            var anchor = Raw.First(Raw.Remainder, Index<RemainderSpace>.Zero, false);
            var (original, display) = GetTokens(anchor);

            original.TryConsume().TryStyle(style);
            display.TryConsume().TryStyle(style);
            if (write) return anchor.TryConsume();
            return anchor;
        }

        private void ApplyWrappedStyles(
            ITokenizedString<StyledString.StyledToken, StyledString.StyledVirtualToken> target,
            IReadOnlyList<Segment<OriginalSpace>> segments,
            string start,
            string value,
            string end,
            Style outer,
            Style inner)
        {
            var parts = ((IStyledToken)target.FromSegments(segments)).TrySplit(start, value, end);
            foreach (var part in parts)
            {
                part.TryConsume().TryStyle(part.Text == value ? inner : outer);
            }
        }

        /**
         * Anchors on a single token spanning start+value+end, then splits it by parts to apply outer/inner
         * styles to Original and Display. If write:true, replaces the full span in Raw with <paramref name="replace"/>.
         *
         * The <paramref name="anchor"/> must have been obtained via Raw.First(start+value+end, ...) so that
         * Provenance correctly identifies the full span.
         */
        public IToken WrappedConsume(IToken anchor, string start, string value, string end, Style outer, Style inner, string replace = "", bool write = false)
        {
            ApplyWrappedStyles(Original, anchor.Provenance, start, value, end, outer, inner);
            ApplyWrappedStyles(Display, anchor.Provenance, start, value, end, outer, inner);
            if (write) return anchor.TryReplaceOrRemove(replace);
            return anchor;
        }

        /**
         * Anchors on a single token spanning start+value+end. In Original, splits by parts to apply outer/inner
         * styles. In Display, replaces the full span with <paramref name="replacement"/> as a single unstyled token.
         * If write:true, also replaces the full span in Raw
         *
         * The <paramref name="anchor"/> must have been obtained via Raw.First(start+value+end, ...) so that
         * Provenance correctly identifies the full span.
         */
        public IToken WrappedReplace(TokenString.Token anchor, string start, string value, string end, Style outer, Style inner, string replacement, bool write = false, bool permanent = false)
        {
            ApplyWrappedStyles(Original, anchor.Provenance, start, value, end, outer, inner);
            Display.FromSegments(anchor.Provenance).TryReplace(replacement);
            if (write) return anchor.TryReplace(replacement);
            return anchor;
        }

        public void Reset()
        {
            string resetText = ResetRaw.Remainder;
            Original = new StyledString(resetText, Original.Theme);
            Display = new StyledString(resetText, Display.Theme);
            Raw = new TokenString(resetText);
            ResetRaw = new TokenString(resetText);
        }

        public new string ToString()
        {
            return ToDisplayString();
        }

        public string ToDisplayString()
        {
            return $"{OriginatingLineNumber}: {Display}";
        }

        public string ToRawString()
        {
            return $"{OriginatingLineNumber}: {Raw}";
        }
    }

    public static class LineTokenExtensions
    {
        public static IStyledToken TryConsume(this IStyledToken token)
        {
            if (token == null || token.IsConsumed) return token;
            if (token is StyledString.StyledToken mapped) return mapped.Consume();
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IToken TryConsume(this IToken token)
        {
            if (token == null || token.IsConsumed) return token;
            if (token is IStyledToken styled) return styled.TryConsume();
            if (token is TokenString.Token mapped) return mapped.Consume();
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IStyledToken TryReplace(this IStyledToken token, string replacement)
        {
            if (string.IsNullOrEmpty(replacement)) throw new ArgumentException("Replacement string cannot be null or empty.", nameof(replacement));
            if (token == null) return null;
            if (token is StyledString.StyledToken mapped) return mapped.Replace(replacement);
            if (token is StyledString.StyledVirtualToken node) return node.Replace(replacement);
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IToken TryReplace(this IToken token, string replacement)
        {
            if (string.IsNullOrEmpty(replacement)) throw new ArgumentException("Replacement string cannot be null or empty.", nameof(replacement));
            if (token == null) return null;
            if (token is IStyledToken styled) return styled.TryReplace(replacement);
            if (token is TokenString.Token mapped) return mapped.Replace(replacement);
            if (token is TokenString.VirtualToken node) return node.Replace(replacement);
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IStyledToken TryRemove(this IStyledToken token)
        {
            if (token == null) return null;
            if (token is StyledString.StyledToken mapped) return mapped.Remove();
            if (token is StyledString.StyledVirtualToken node) return node.Remove();
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IToken TryRemove(this IToken token)
        {
            if (token == null) return null;
            if (token is IStyledToken styled) return styled.TryRemove();
            if (token is TokenString.Token mapped) return mapped.Remove();
            if (token is TokenString.VirtualToken node) return node.Remove();
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IStyledToken TryReplaceOrRemove(this IStyledToken token, string replacement)
        {
            if (string.IsNullOrEmpty(replacement)) return token.TryRemove();
            return token.TryReplace(replacement);
        }

        public static IToken TryReplaceOrRemove(this IToken token, string replacement)
        {
            if (string.IsNullOrEmpty(replacement)) return token.TryRemove();
            return token.TryReplace(replacement);
        }

        public static IStyledToken[] TrySplit(this IStyledToken token, params string[] parts)
        {
            if (token == null) return Array.Empty<IStyledToken>();
            if (token is StyledString.StyledToken mapped) return mapped.Split(parts);
            if (token is StyledString.StyledVirtualToken node) return node.Split(parts);
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        public static IToken[] TrySplit(this IToken token, params string[] parts)
        {
            if (token == null) return Array.Empty<IToken>();
            if (token is IStyledToken styled) return styled.TrySplit(parts);
            if (token is TokenString.Token mapped) return mapped.Split(parts);
            if (token is TokenString.VirtualToken node) return node.Split(parts);
            throw new ArgumentException($"Unrecognized token type: {token.GetType().Name}", nameof(token));
        }

        /// <summary>
        /// Resolves <paramref name="key"/> against the token's theme, then applies it. Throws
        /// KeyNotFoundException if the theme does not define the key.
        /// </summary>
        public static IStyledToken TryStyle(this IStyledToken token, string key, bool overrideExisting = false)
        {
            if (token == null) return null;
            return token.TryStyle(token.Theme[key], overrideExisting);
        }

        /// <summary>
        /// Resolves <paramref name="key"/> against the token's theme, falling back to
        /// <paramref name="fallbackKey"/> when the theme does not define it, then applies the result.
        /// </summary>
        public static IStyledToken TryStyle(this IStyledToken token, string key, string fallbackKey, bool overrideExisting = false)
        {
            if (token == null) return null;
            return token.TryStyle(token.Theme[key, fallbackKey], overrideExisting);
        }

        public static IStyledToken TryStyle(this IStyledToken token, Style style, bool overrideExisting = false)
        {
            if (token == null) return null;
            if (token is StyledString.StyledToken mapped)
            {
                if (mapped.IsSplit)
                {
                    foreach (var c in mapped.Leaves)
                    {
                        if (overrideExisting || c.Style == null) c.Style = style;
                    }
                }
                if (!mapped.IsRemoved)
                {
                    mapped.Style = style;
                }
            }
            if (token is StyledString.StyledVirtualToken node)
            {
                if (!node.IsRemoved && (overrideExisting || node.Style == null)) node.Style = style;
            }
            return token;
        }
    }
}
