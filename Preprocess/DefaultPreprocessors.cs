using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Highlight;
using IC10_Extender.Operations;
using IC10_Extender.Wrappers;
using UI.ImGuiUi.Debug;
using UnityEngine;
using static IC10_Extender.Preprocessors.Preprocessor;

namespace IC10_Extender.Preprocess
{
    public static class DefaultPreprocessors
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Debug);

        public static Func<string, int, double> Packer;
        public static Func<string, double> Hasher;

        internal static void Register()
        {
            IC10Extender.Register(new RegexPreprocessor(
                "comment_preprocessor",
                PriorityComment,
                $"<color={Theme.Default.Comment.Foreground}>#</color>",
                "any following characters on this line are ignored",
                new Regex(@"(?<full>#.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                RegexPreprocessor.Remainder,
                (context, line, match, write) =>
                {
                    var anchor = line.Raw.First(match.Groups["full"].Value, match.Index, false);
                    line.Consume(anchor, Theme.Default.Comment, write: write);
                    return new RegexPreprocessor.Result(false, -1); //should only run once per line, since it consumes the remainder of the line
                }
            ));

            if (Plugin.Instance == null) //must be unit testing
            {
                Packer = (s, lineNumber) => s.GetHashCode();
                Hasher = s => s.GetHashCode();
            } else
            {
                try {
                    Packer = ProgrammableChip.PackAscii6;
                } catch (SecurityException) {
                    Packer = (s, lineNumber) => s.GetHashCode();
                }

                try {
                    Hasher = s => Animator.StringToHash(s);
                } catch (SecurityException) {
                    Hasher = s => s.GetHashCode();
                }
            }

            IC10Extender.Register(
                new RegexPreprocessor(
                    "raw_string_preprocessor",
                    PriorityRawLiteral,
                    $"<color={Theme.Default.Macro.Foreground}>RSTR(</color><color={Theme.Default.String.Foreground}>"
                    + $"\"...\"</color><color={Theme.Default.Macro.Foreground}>)</color>",
                    "any text inside will be packed to a double before processing takes place. "
                    + "Use this to pass a string literal, such as for use in text displays. "
                    + "The original text is not maintained when compiled, and will be replaced with the raw computed value.",
                    new Regex(@"(^|\s+)(?<start>RSTR\("")(?<value>[^""]+)(?<end>""\))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var start = match.Groups["start"].Value;
                        var value = match.Groups["value"].Value;
                        var replacement = Packer(value, line.LineNumber).ToString();
                        var end = match.Groups["end"].Value;
                        var anchor = line.Raw.First(start + value + end, 0, false);

                        var result = line.WrappedReplace(anchor, start, value, end, Theme.Default.Macro, Theme.Default.String, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                ),
                Plugin.PreprocessorVariantsEnabled
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "string_preprocessor",
                    PriorityLiteral,
                    $"<color={Theme.Default.Macro.Foreground}>STR(</color><color={Theme.Default.String.Foreground}>"
                    + $"\"...\"</color><color={Theme.Default.Macro.Foreground}>)</color>",
                    "any text inside will be packed to a double before processing takes place. "
                    + "Use this to pass a string literal, such as for use in text displays.",
                    new Regex(@"(^|\s+)(?<start>STR\("")(?<value>[^""]+)(?<end>""\))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var start = match.Groups["start"].Value;
                        var value = match.Groups["value"].Value;
                        var replacement = Packer(value, line.LineNumber).ToString();
                        var end = match.Groups["end"].Value;
                        var anchor = line.Raw.First(start + value + end, match.Index, false);

                        var result = line.WrappedConsume(anchor, start, value, end, Theme.Default.Macro, Theme.Default.String, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                ),
                Plugin.AfterStrings
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "raw_hash_preprocessor",
                    PriorityRawLiteral,
                    $"<color={Theme.Default.Macro.Foreground}>RHASH(</color><color={Theme.Default.String.Foreground}>"
                    + $"\"...\"</color><color={Theme.Default.Macro.Foreground}>)</color>",
                    "any text inside will be hashed to an integer before processing takes place. "
                    + "Use this to generate integer values for use wherever hashes are required. "
                    + "The original text is not maintained when compiled, and will be replaced with the raw computed value.",
                    new Regex(@"(^|\s+)(?<start>RHASH\("")(?<value>[^""]+)(?<end>""\))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var start = match.Groups["start"].Value;
                        var value = match.Groups["value"].Value;
                        var replacement = Hasher(value).ToString();
                        var end = match.Groups["end"].Value;
                        var anchor = line.Raw.First(start + value + end, match.Index, false);

                        var result = line.WrappedReplace(anchor, start, value, end, Theme.Default.Macro, Theme.Default.String, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                ),
                Plugin.PreprocessorVariantsEnabled
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "hash_preprocessor",
                    PriorityLiteral,
                    $"<color={Theme.Default.Macro.Foreground}>HASH(</color><color={Theme.Default.String.Foreground}>"
                    + $"\"...\"</color><color={Theme.Default.Macro.Foreground}>)</color>",
                    "any text inside will be hashed to an integer before processing takes place. "
                    + "Use this to generate integer values for use wherever hashes are required. ",
                    new Regex(@"(^|\s+)(?<start>HASH\("")(?<value>[^""]+)(?<end>""\))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var start = match.Groups["start"].Value;
                        var value = match.Groups["value"].Value;
                        var replacement = Hasher(value).ToString();
                        var end = match.Groups["end"].Value;
                        var anchor = line.Raw.First(start + value + end, match.Index, false);

                        var result = line.WrappedConsume(anchor, start, value, end, Theme.Default.Macro, Theme.Default.String, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                )
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "raw_binary_literal_preprocessor",
                    PriorityRawLiteral,
                    $"<color={Theme.Default.Number.Foreground}>%%</color>",
                    "any valid binary numbers (0 or 1) will be parsed together as a binary value. "
                    + "You can use underscores to help with readability, but they have no functional use. "
                    + "As an example, %1111 or %11_11 will parse as 15. The original text is not maintained when compiled, "
                    + "and will be replaced with the raw computed value.",
                    new Regex(@"(^|\s+)(?<match>%%(?<value>[_01]+))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var replacement = Convert.ToInt64(match.Groups["value"].Value.Replace("_", ""), 2).ToString();
                        var anchor = line.Raw.First(match.Groups["match"].Value, match.Index, false);

                        var result = line.Replace(anchor, Theme.Default.Number, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                ),
                Plugin.PreprocessorVariantsEnabled
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "binary_literal_preprocessor",
                    PriorityLiteral,
                    $"<color={Theme.Default.Number.Foreground}>%</color>",
                    "any valid binary numbers (0 or 1) will be parsed together as a binary value. "
                    + "You can use underscores to help with readability, but they have no functional use. "
                    + "As an example, %1111 or %11_11 will parse as 15.",
                    new Regex(@"(^|\s+)(?<match>%(?<value>[_01]+))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var replacement = Convert.ToInt64(match.Groups["value"].Value.Replace("_", ""), 2).ToString();
                        var anchor = line.Raw.First(match.Groups["match"].Value, match.Index, false);

                        var result = line.Consume(anchor, Theme.Default.Number, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                )
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "raw_hex_literal_preprocessor",
                    PriorityRawLiteral,
                    $"<color={Theme.Default.Number.Foreground}>$$</color>",
                    "any valid hex characters after this will be parsed together as a hex value. "
                    + "You can use underscores to help with readability, but they have no functional use. "
                    + "As an example, $F will parse as 15. The original text is not maintained when compiled, "
                    + "and will be replaced with the raw computed value.",
                    new Regex(@"(^|\s+)(?<match>\$\$(?<value>[_0-9a-fA-F]+))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var replacement = Convert.ToInt64(match.Groups["value"].Value.Replace("_", ""), 16).ToString();
                        var anchor = line.Raw.First(match.Groups["match"].Value, match.Index, false);

                        var result = line.Replace(anchor, Theme.Default.Number, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                ),
                Plugin.PreprocessorVariantsEnabled
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "hex_literal_preprocessor",
                    PriorityLiteral,
                    $"<color={Theme.Default.Number.Foreground}>$</color>",
                    "any valid hex characters after this will be parsed together as a hex value. "
                    + "You can use underscores to help with readability, but they have no functional use. "
                    + "As an example, $F will parse as 15.",
                    new Regex(@"(^|\s+)(?<match>\$(?<value>[_0-9a-fA-F]+))(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var replacement = Convert.ToInt64(match.Groups["value"].Value.Replace("_", ""), 16).ToString();
                        var anchor = line.Raw.First(match.Groups["match"].Value, match.Index, false);

                        var result = line.Consume(anchor, Theme.Default.Number, replacement, write);

                        return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                    }
                )
            );

            var sharedLabelLinesRegex = new Regex(@"^\s*(?<match>(?<value>[-_.0-9a-zA-Z]+):)(\s+|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            var nonSharedLabelLinesRegex = new Regex(@"^\s*(?<match>(?<value>[-_.0-9a-zA-Z]+):)\s*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            IC10Extender.Register(
                new RegexPreprocessor(
                    "jump_label_preprocessor",
                    PriorityKeywordFinder,
                    $"<color={Theme.Default.Jump.Foreground}>NAME:</color>",
                    "creates a jump label at the named line. These can used in place of line numbers, like in j- and b- commands.",
                    () => Plugin.LabelsCanShareLineEnabled.Accept() ? sharedLabelLinesRegex : nonSharedLabelLinesRegex,
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var sharedLabelLines = Plugin.LabelsCanShareLineEnabled.Accept();
                        var anchor = line.Raw.First(match.Groups["match"].Value, match.Index, false);

                        var result = line.Consume(anchor, Theme.Default.Jump, write: write); //trailing punctuation e.g. ":" breaks word boundary

                        if (!sharedLabelLines)
                        {
                            if (write) line.ForcedOp = new NoOpOperation(line);
                            var trimmed = line.Raw.Remainder.Trim();
                            if (trimmed.Length > 0) //if any text remains, and shared lines is disabled, style as error
                            {
                                context.AddError(
                                    line,
                                    line.Raw.Unconsumed.First().StartIndex,
                                    new LabelSharedLineException(line.LineNumber, line.Display.Theme)
                                );
                                line.ConsumeRemainder(Theme.Default.Error, write);
                            }
                            return new RegexPreprocessor.Result(false, -1); //only one match per line if feature disabled
                        } else
                        {
                            if (write) context.JumpTags.Add(match.Groups["value"].Value, line.LineNumber);
                            return new RegexPreprocessor.Result(true, write ? 0 : match.Index + result.Length); //run until all matches are consumed
                        }
                    }
                )
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "alias_preprocessor",
                    PriorityKeywordFinder,
                    new Regex(@"^\s*alias\s+(?<key>[-_.0-9a-zA-Z]+)\s+(?<value>[dr][0-9]+)\s*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var key = match.Groups["key"].Value;
                        var value = match.Groups["value"].Value;
                        var isDevice = value.StartsWith("d");

                        // Anchor on the whole match, then split to locate each named fragment.
                        // Interstitial whitespace tokens produced by Split are left unconsumed.
                        var anchor = line.Raw.First(match.Value, match.Index, false);
                        var parts = anchor.Split("alias", key, value);
                        foreach (var part in parts)
                        {
                            if (part.Text == "alias")
                                line.Consume(part, Theme.Default.OpCode);
                            else if (part.Text == key)
                                line.Consume(part, Theme.Default.AliasKey);
                            else if (part.Text == value)
                                line.Consume(part, isDevice ? Theme.Default.Device : Theme.Default.Register);
                        }
                        anchor.Remove();

                        if (write) //remove vanilla alias operations, since they are effectively replaced by this preprocessor
                        {
                            line.ForcedOp = new NoOpOperation(line);

                            context.Aliases.Add(
                                key,
                                new AliasValue(isDevice ? AliasTarget.Device : AliasTarget.Register, int.Parse(value.Substring(1)))
                            );
                        }

                        return new RegexPreprocessor.Result(false, -1); //only one match per line
                    }
                )
            );

            IC10Extender.Register(
                new RegexPreprocessor(
                    "define_preprocessor",
                    PriorityKeywordFinder,
                    new Regex(@"^\s*define\s+(?<key>[-_.0-9a-zA-Z]+)\s+(?<value>[0-9]+)\s*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
                    RegexPreprocessor.Remainder,
                    (context, line, match, write) =>
                    {
                        var key = match.Groups["key"].Value;
                        var value = match.Groups["value"].Value;

                        // Anchor on the whole match, then split to locate each named fragment.
                        // Interstitial whitespace tokens produced by Split are left unconsumed.
                        var anchor = line.Raw.First(match.Value, match.Index, false);
                        var parts = anchor.Split("define", key, value);
                        foreach (var part in parts)
                        {
                            if (part.Text == "define")
                                line.Consume(part, Theme.Default.OpCode);
                            else if (part.Text == key)
                                line.Consume(part, Theme.Default.DefineKey);
                            else if (part.Text == value)
                                line.Consume(part, Theme.Default.Number);
                        }
                        anchor.Remove();

                        if (write) //remove vanilla define operations
                        {
                            line.ForcedOp = new NoOpOperation(line);

                            context.Defines.Add(
                                key,
                                double.Parse(value)
                            );
                        }

                        return new RegexPreprocessor.Result(false, -1); //only one match per line
                    }
                )
            );
        }
    }
}
