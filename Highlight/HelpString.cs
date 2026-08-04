using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Highlight
{
    public class HelpString : IStyledString
    {
        public static readonly Func<Tuple<string, Style?>, string> VanillaFormat = t => t.Item2.HasValue ? t.Item2.Value.ApplyVanillaFormat(t.Item1) : t.Item1;
        public static readonly Func<Tuple<string, Style?>, string> RawFormat = t => t.Item1;

        // Matches any supported open or close tag in a vanilla styled string.
        private static readonly Regex ParseTagPattern = new Regex(
            @"(?<open_color><color=(?<color_value>[#\w]\w*)>)" +
            @"|(?<open_link><link=(?<link_value>[^>]*)>)" +
            @"|(?<open_b><b>)" +
            @"|(?<open_i><i>)" +
            @"|(?<open_u><u>)" +
            @"|(?<open_s><s>)" +
            @"|(?<close_color></color>)" +
            @"|(?<close_link></link>)" +
            @"|(?<close_b></b>)" +
            @"|(?<close_i></i>)" +
            @"|(?<close_u></u>)" +
            @"|(?<close_s></s>)",
            RegexOptions.Compiled);

        public Theme Theme { get; private set; }

        public IEnumerable<Tuple<string, Style?>> AllSegments => tokens.ToList();

        private readonly List<Tuple<string, Style?>> tokens = new List<Tuple<string, Style?>>();

        #region Constants
        public static readonly HelpString OR = new HelpString(Theme.Default, "|", Theme.Default.Or);
        public static readonly HelpString STRING = new HelpString(Theme.Default, "str", Theme.Default.String);
        public static readonly HelpString DEVICE_INDEX = new HelpString(Theme.Default, "d?", Theme.Default.Device);
        public static readonly HelpString REGISTER = new HelpString(Theme.Default, "r?", Theme.Default.Register);
        public static readonly HelpString INTEGER = new HelpString(Theme.Default, "int", Theme.Default.Integer);
        public static readonly HelpString NUMBER = new HelpString(Theme.Default, "num", Theme.Default.Number);
        public static readonly HelpString REF_ID = new HelpString(Theme.Default, "id", Theme.Default.RefId);
        public static readonly HelpString LOGIC_TYPE = new HelpString(Theme.Default, "logicType", Theme.Default.LogicType);
        public static readonly HelpString LOGIC_SLOT_TYPE = new HelpString(Theme.Default, "logicSlotType", Theme.Default.LogicSlotType);
        public static readonly HelpString BATCH_MODE = new HelpString(Theme.Default, "batchMode", Theme.Default.BatchMode);
        public static readonly HelpString DEVICE_HASH = new HelpString(Theme.Default, "deviceHash", Theme.Default.DeviceHash);
        public static readonly HelpString NAME_HASH = new HelpString(Theme.Default, "nameHash", Theme.Default.NameHash);
        public static readonly HelpString SLOT_INDEX = new HelpString(Theme.Default, "slotIndex", Theme.Default.SlotIndex);
        public static readonly HelpString REAGENT_MODE = new HelpString(Theme.Default, "reagentMode", Theme.Default.ReagentMode);
        public static readonly HelpString JUMP_LABEL = new HelpString(Theme.Default, "jump", Theme.Default.Jump);
        #endregion Constants

        /// <summary>
        /// private constructor for internal use when all tokens have already been resolved to the current theme, such as when re-theming 
        /// or stripping styles. Public constructors and operators should ensure that tokens are always in sync with the current theme.
        /// </summary>
        /// <param name="theme"></param>
        /// <param name="tokens"></param>
        protected HelpString(Theme theme, List<Tuple<string, Style?>> tokens)
        {
            Theme = theme;
            this.tokens = tokens.ToList();
        }

        public HelpString(Theme theme, params Tuple<string, Style?>[] tokens)
        {
            Theme = theme;
            this.tokens.AddRange(tokens.Select(t => new Tuple<string, Style?>(t.Item1, theme[t.Item2])));
        }

        public HelpString(Theme theme, string original, Style? style = null)
        {
            Theme = theme;
            tokens.Add(new Tuple<string, Style?>(original, theme[style]));
        }

        public void ReTheme(Theme theme)
        {
            Theme = theme ?? Theme.Default;
            var newTokens = tokens.Select(t => new Tuple<string, Style?>(t.Item1, theme[t.Item2])).ToList();
            tokens.Clear();
            tokens.AddRange(newTokens);
        }

        public HelpString WithTheme(Theme theme)
        {
            theme = theme ?? Theme.Default;
            return new HelpString(theme, tokens.Select(t => new Tuple<string, Style?>(t.Item1, theme[t.Item2])).ToList());
        }

        public HelpString Strip(Style? newStyle = null)
        {
            return new HelpString(Theme, tokens.Select(t => new Tuple<string, Style?>(t.Item1, Theme[newStyle])).ToList());
        }

        public HelpString Optional(Style? bracketStyle = null)
        {
            Style style = Theme[bracketStyle] ?? Theme.VarBrackets;
            List<Tuple<string, Style?>> newTokens = new List<Tuple<string, Style?>>();
            newTokens.Add(new Tuple<string, Style?>("[", style));
            newTokens.AddRange(tokens);
            newTokens.Add(new Tuple<string, Style?>("]", style));
            return new HelpString(Theme, newTokens);
        }

        public HelpString Var(string name, Style? nameStyle = null, Style? parenthesesStyle = null)
        {
            Style ns = Theme[nameStyle] ?? Theme.VarName;
            Style ps = Theme[parenthesesStyle] ?? Theme.VarParentheses;
            List<Tuple<string, Style?>> newTokens = new List<Tuple<string, Style?>>();

            newTokens.Add(new Tuple<string, Style?>(name, ns));
            newTokens.Add(new Tuple<string, Style?>("(", ps));
            newTokens.AddRange(tokens);
            newTokens.Add(new Tuple<string, Style?>(")", ps));

            return new HelpString(Theme, newTokens);
        }

        public static HelpString operator +(HelpString left, HelpString right)
        {
            List<Tuple<string, Style?>> newTokens = new List<Tuple<string, Style?>>();

            newTokens.AddRange(left.tokens);
            newTokens.AddRange(OR.WithTheme(left.Theme).tokens);
            newTokens.AddRange(right.WithTheme(left.Theme).tokens);

            return new HelpString(left.Theme, newTokens);
        }

        public static implicit operator ProgrammableChip.HelpString(HelpString str) => new ProgrammableChip.HelpString(str.ToVanillaString());

        /// <summary>
        /// Parses a vanilla-styled string (with Unity rich-text tags) into a <see cref="HelpString"/>
        /// via recursive descent.
        /// <para>
        /// Supported tags: <c>&lt;color=X&gt;</c>, <c>&lt;link={content}&gt;</c>, <c>&lt;b&gt;</c>,
        /// <c>&lt;i&gt;</c>, <c>&lt;u&gt;</c>, <c>&lt;s&gt;</c>.
        /// </para>
        /// Throws <see cref="FormatException"/> if any tag is unclosed, closed out of order, or opened
        /// while the same tag type is already active.
        /// Plain (untagged) text is added with a null style. Partially-overlapping styled regions
        /// are split into separate tokens each carrying only the styles active at that point.
        /// </summary>
        public static HelpString Parse(string vanillaStyledString)
        {
            var input = vanillaStyledString;
            var tuples = new List<Tuple<string, Style?>>();

            // Each frame records: tagName ("color","link","b","i","u","s"), tagValue (for color/link), open position.
            var tagStack = new Stack<Tuple<string, string, int>>();

            // Tracks which tag types are currently open for O(1) duplicate detection.
            var openTagTypes = new HashSet<string>();

            int cursor = 0;

            foreach (Match match in ParseTagPattern.Matches(input))
            {
                int matchStart = match.Index;

                // Emit any plain text before this tag using the style currently active on the stack.
                if (matchStart > cursor)
                {
                    string plain = input.Substring(cursor, matchStart - cursor);
                    tuples.Add(new Tuple<string, Style?>(plain, BuildStyle(tagStack)));
                }

                cursor = matchStart + match.Length;

                if (match.Groups["open_color"].Success)
                {
                    PushTag(tagStack, openTagTypes, "color", match.Groups["color_value"].Value, matchStart, input);
                }
                else if (match.Groups["open_link"].Success)
                {
                    PushTag(tagStack, openTagTypes, "link", match.Groups["link_value"].Value, matchStart, input);
                }
                else if (match.Groups["open_b"].Success)
                {
                    PushTag(tagStack, openTagTypes, "b", null, matchStart, input);
                }
                else if (match.Groups["open_i"].Success)
                {
                    PushTag(tagStack, openTagTypes, "i", null, matchStart, input);
                }
                else if (match.Groups["open_u"].Success)
                {
                    PushTag(tagStack, openTagTypes, "u", null, matchStart, input);
                }
                else if (match.Groups["open_s"].Success)
                {
                    PushTag(tagStack, openTagTypes, "s", null, matchStart, input);
                }
                else
                {
                    // Closing tag — determine which one.
                    string closingTag;
                    if      (match.Groups["close_color"].Success) closingTag = "color";
                    else if (match.Groups["close_link"].Success)  closingTag = "link";
                    else if (match.Groups["close_b"].Success)     closingTag = "b";
                    else if (match.Groups["close_i"].Success)     closingTag = "i";
                    else if (match.Groups["close_u"].Success)     closingTag = "u";
                    else                                          closingTag = "s";

                    if (tagStack.Count == 0)
                        throw new FormatException(
                            $"Unexpected closing tag </{closingTag}> with no open tags at position {matchStart} in \"{input}\"");

                    var top = tagStack.Peek();
                    if (top.Item1 != closingTag)
                        throw new FormatException(
                            $"Closing tag </{closingTag}> does not match innermost open tag <{top.Item1}> at position {matchStart} in \"{input}\"");

                    tagStack.Pop();
                    openTagTypes.Remove(closingTag);
                }
            }

            // Emit any trailing plain text after the last tag.
            if (cursor < input.Length)
                tuples.Add(new Tuple<string, Style?>(input.Substring(cursor), BuildStyle(tagStack)));

            // Any tags still open are unclosed.
            if (tagStack.Count > 0)
            {
                var unclosed = tagStack.Peek();
                throw new FormatException(
                    $"Unclosed tag <{unclosed.Item1}> opened at position {unclosed.Item3} in \"{input}\"");
            }

            return new HelpString(Theme.Default, tuples);
        }

        private static void PushTag(
            Stack<Tuple<string, string, int>> tagStack,
            HashSet<string> openTagTypes,
            string tagName,
            string tagValue,
            int position,
            string input)
        {
            if (!openTagTypes.Add(tagName))
                throw new FormatException(
                    $"Duplicate tag <{tagName}> at position {position}: tag is already open in \"{input}\"");

            tagStack.Push(new Tuple<string, string, int>(tagName, tagValue, position));
        }

        /// <summary>
        /// Builds a <see cref="Style"/> from the current tag stack, combining all active style
        /// properties. Returns <c>null</c> when the stack is empty.
        /// The key is <c>"parsed-{part1}-{part2}-..."</c> where parts are listed outermost-first:
        /// the color value for <c>&lt;color&gt;</c>, the link content for <c>&lt;link&gt;</c>,
        /// and the tag name for decoration tags.
        /// </summary>
        private static Style? BuildStyle(Stack<Tuple<string, string, int>> tagStack)
        {
            if (tagStack.Count == 0) return null;

            // Stack is LIFO — reverse to get outermost-first order for the key.
            var frames = tagStack.ToArray();
            Array.Reverse(frames);

            string foreground = null;
            string link = null;
            bool bold = false;
            bool italic = false;
            bool underline = false;
            bool strikeThrough = false;

            var keyParts = new List<string> { "parsed" };

            foreach (var frame in frames)
            {
                switch (frame.Item1)
                {
                    case "color":
                        foreground = frame.Item2;
                        keyParts.Add(frame.Item2);
                        break;
                    case "link":
                        link = frame.Item2;
                        keyParts.Add(frame.Item2);
                        break;
                    case "b":
                        bold = true;
                        keyParts.Add("b");
                        break;
                    case "i":
                        italic = true;
                        keyParts.Add("i");
                        break;
                    case "u":
                        underline = true;
                        keyParts.Add("u");
                        break;
                    case "s":
                        strikeThrough = true;
                        keyParts.Add("s");
                        break;
                }
            }

            return new Style(
                key: string.Join("-", keyParts),
                fallbackKey: null,
                foreground: foreground,
                link: link,
                bold: bold,
                italic: italic,
                underline: underline,
                strikeThrough: strikeThrough
            );
        }

        public string ToString(Func<Tuple<string, Style?>, string> formatter)
        {
            return string.Join("", tokens.Select(formatter));
        }

        public string ToVanillaString()
        {
            return ToString(VanillaFormat);
        }

        public string ToRawString()
        {
            return ToString(RawFormat);
        }

        public override string ToString()
        {
            return ToVanillaString();
        }
    }
}
