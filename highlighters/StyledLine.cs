using Assets.Scripts.Util;
using Objects.Rockets.Scanning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IC10_Extender.Highlighters
{
    public readonly struct Style
    {
        public readonly string ForegroundColor;
        public readonly string BackgroundColor; //null means use default background
        public readonly bool Italic;
        public readonly bool Bold;
        public readonly bool Underline;
        public readonly bool StrikeThrough;

        public Style(string foreground, string background = null, bool italic = false, bool bold = false, bool underline = false, bool strikeThrough = false)
        {
            ForegroundColor = foreground;
            BackgroundColor = background;
            Italic = italic;
            Bold = bold;
            Underline = underline;
            StrikeThrough = strikeThrough;
        }
    }

    public class Theme : IDictionary<string, Style>
    {
        public static readonly Theme Vanilla = new Theme
        (
            new Style("#A0A0A0"),
            new Style("#0080FFFF"),
            new Style("#20B2AA"),
            new Style("#00FFEC"),
            new Style("#585858FF"),
            new Style("#808080"),
            new Style("white"),
            new Style("yellow"),
            new Style("orange"),
            new Style("orange"),
            new Style("green"),
            new Style("purple"),
            new Style("red"),
            new Style("grey"),
            new Style("darkgrey")
        );

        private Dictionary<string, Style> colors = new Dictionary<string, Style>();

        public Style Macro => this[nameof(Macro)];
        public Style Register => this[nameof(Register)];
        public Style Number => this[nameof(Number)];
        public Style Network => this[nameof(Network)];
        public Style Comment => this[nameof(Comment)];
        public Style Help => this[nameof(Help)];
        public Style String => this[nameof(String)];
        public Style OpCode => this[nameof(OpCode)];
        public Style LogicType => this[nameof(LogicType)];
        public Style LogicSlotType => this[nameof(LogicSlotType)];
        public Style Device => this[nameof(Device)];
        public Style Jump => this[nameof(Jump)];
        public Style Error => this[nameof(Error)];
        public Style Description => this[nameof(Description)];
        public Style Example => this[nameof(Example)];

        public ICollection<string> Keys => colors.Keys;

        public ICollection<Style> Values => colors.Values;

        public int Count => colors.Count;

        public bool IsReadOnly => false;

        public Style this[string key] { 
            get => colors[key];
            set => Add(key, value);
        }

        public Theme(
            Style macro,
            Style register,
            Style number,
            Style network,
            Style comment,
            Style help,
            Style str,
            Style opCode,
            Style logicType,
            Style logicSlotType,
            Style device,
            Style jump,
            Style error,
            Style description,
            Style example,
            Dictionary<string, Style> additional = null
        )
        {
            Add(nameof(Macro), macro);
            Add(nameof(Register), register);
            Add(nameof(Number), number);
            Add(nameof(Network), network);
            Add(nameof(Comment), comment);
            Add(nameof(Help), help);
            Add(nameof(String), str);
            Add(nameof(OpCode), opCode);
            Add(nameof(LogicType), logicType);
            Add(nameof(LogicSlotType), logicSlotType);
            Add(nameof(Device), device);
            Add(nameof(Jump), jump);
            Add(nameof(Error), error);
            Add(nameof(Description), description);
            Add(nameof(Example), example);

            if(additional != null) AddAll(additional);
        }

        public bool ContainsKey(string key)
        {
            return colors.ContainsKey(key);
        }

        public void Add(string key, Style value)
        {
            if (ContainsKey(key)) throw new ArgumentException($"{key} already exists in this theme"); //avoid accidental overriding
            colors.Add(key, value);
        }

        public void Add(KeyValuePair<string, Style> item)
        {
            Add(item.Key, item.Value);
        }

        public void AddAll(Dictionary<string, Style> additional)
        {
            foreach (var kvp in additional)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public void Override(string key, Style value)
        {
            colors.Add(key, value);
        }

        public void Override(KeyValuePair<string, Style> item)
        {
            Override(item.Key, item.Value);
        }

        public void OverrideAll(Dictionary<string, Style> additional)
        {
            foreach (var kvp in additional)
            {
                Override(kvp.Key, kvp.Value);
            }
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException("Entires cannot be removed from a theme");
        }

        public bool TryGetValue(string key, out Style value)
        {
            return colors.TryGetValue(key, out value);
        }

        public void Clear()
        {
            throw new InvalidOperationException("Themes cannot be cleared");
        }

        public bool Contains(KeyValuePair<string, Style> item)
        {
            return colors.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Style>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, Style>>)colors).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, Style> item)
        {
            return ((ICollection<KeyValuePair<string, Style>>)colors).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, Style>> GetEnumerator()
        {
            return colors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)colors).GetEnumerator();
        }
    }

    public class StyledLine
    {
        public readonly Theme Theme;
        public readonly string Original;
        private List<Token> remainder = new List<Token>();
        
        private readonly List<Token> Tokens = new List<Token>();

        public StyledLine(Theme theme, string original)
        {
            Theme = theme;
            Original = original;
            remainder.Add(new Token(this, original, 0, null));
        }

        public void Consume(string token, string color)
        {
            if(TryConsume(token, color, 0, false, out var consumed, out var remaining))
            {
                remaining.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
                remainder = remaining;

                Tokens.AddRange(consumed);
                Tokens.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
            }
        }

        public void ConsumeAll(string token, string color)
        {
            while (TryConsume(token, color, 0, false, out var consumed, out var remaining))
            {
                Tokens.AddRange(consumed);
                remaining.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
                remainder = remaining;
            }
            Tokens.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
        }

        private bool TryConsume(string token, string color, int tokenIndex, bool inSpan, out List<Token> consumedTokens, out List<Token> remaining)
        {
            Console.WriteLine($"TryConsume(Token:\"{token}\", color:\"{color}\", tokenIndex:{tokenIndex}, inSpan:{inSpan}");

            if (tokenIndex < 0) throw new ArgumentOutOfRangeException(nameof(tokenIndex));
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (token.Length == 0) throw new ArgumentException("token must have at least one character");
            if (color == null) throw new ArgumentNullException(nameof(color));

            if (tokenIndex >= remainder.Count) //end of list, hard fail
            {
                Console.WriteLine("End of list, hard fail");
                consumedTokens = new List<Token>();
                remaining = remainder.ToList();
                Console.WriteLine($"Returning false (consumedTokens:{consumedTokens.ToArrayString()}, remaining: {remaining.ToArrayString()}");
                return false;
            }

            var len = token.Length;
            var pattern = compile(token);

            
            for(var result = pattern.Match(remainder[tokenIndex].Text); result.Success; result = pattern.Match(remainder[tokenIndex].Text, result.Index+1))
            {
                Token? before = null;
                Token? after = null;

                Console.WriteLine($"Match(Value:{result.Value}, Index:{result.Index}, Length:{result.Length})");
                if (result.Index != 0) //doesn't start at beginning of token
                {
                    Console.WriteLine("Doesn't start at beginning of token");
                    if (inSpan) //then not a contiguous match, return false
                    {
                        Console.WriteLine("Not a contiguous match");
                        consumedTokens = new List<Token>();
                        remaining = remainder.ToList();
                        Console.WriteLine($"Returning false (before:{before}, after:{after},\nconsumedTokens:{consumedTokens.ToArrayString()}, remaining: {remaining.ToArrayString()}");
                        return false;
                    }

                    // else new start of match, with potential before token
                    var beforeText = remainder[tokenIndex].Text.Substring(0, result.Index);
                    before = new Token(this, beforeText, remainder[tokenIndex].StartIndex, null);
                    Console.WriteLine($"Setting potential before token: {before}");
                }

                if (result.Length == token.Length) // full match
                {
                    Console.WriteLine("Full match");
                    if (result.Index + result.Length < remainder[tokenIndex].Length) //with after component
                    {
                        var afterText = remainder[tokenIndex].Text.Substring(result.Index + result.Length);
                        after = new Token(this, afterText, remainder[tokenIndex].StartIndex + result.Index + result.Length, null);
                        Console.WriteLine($"Setting after token: {after}");
                    }

                    consumedTokens = new List<Token>
                    {
                        new Token(this, result.Value, remainder[tokenIndex].StartIndex + result.Index, color)
                    };

                    remaining = remainder.ToList();
                    var index = remaining.IndexOf(remainder[tokenIndex]);
                    remaining.Remove(remainder[tokenIndex]);
                    if (before.HasValue) remaining.Insert(index, before.Value);
                    if (after.HasValue) remaining.Insert(index, after.Value);
                    Console.WriteLine($"Returning true (before:{before}, after:{after},\nconsumedTokens:{consumedTokens.ToArrayString()}, remaining: {remaining.ToArrayString()}");
                    return true;
                }

                // else partial match only
                Console.WriteLine("Partial match only");
                if (result.Index + result.Length != remainder[tokenIndex].Length) //does not run up to end
                {
                    Console.WriteLine("Does not run to end of token");
                    if (inSpan) // partial match ends without continuing to next token. Return span failure
                    {
                        Console.WriteLine("Partial match ends without continuing to next token");
                        consumedTokens = new List<Token>();
                        remaining = remainder.ToList();
                        Console.WriteLine($"Returning false (before:{before}, after:{after},\nconsumedTokens:{consumedTokens.ToArrayString()}, remaining: {remaining.ToArrayString()}");
                        return false;
                    } else // partial match ends, but since not in span, we can continue to next potential match
                    {
                        Console.WriteLine("Partial match ends outside of span. Continue to next potential match");
                        continue;
                    }
                }

                //else runs up to end, recurse to next token, starting span
                Console.WriteLine("Runs up to end of token, starting span");
                var spanSuccess = TryConsume(token.Substring(result.Length), color, tokenIndex + 1, true, out consumedTokens, out remaining);
                if (spanSuccess)
                {
                    Console.WriteLine("Span successful");
                    consumedTokens.Add(new Token(this, result.Value, remainder[tokenIndex].StartIndex + result.Index, color));
                    var index = remaining.IndexOf(remainder[tokenIndex]);
                    remaining.Remove(remainder[tokenIndex]);
                    if(before.HasValue) remaining.Insert(index, before.Value);
                    Console.WriteLine($"Returning true (before:{before}, after:{after},\nconsumedTokens:{consumedTokens.ToArrayString()}, remaining: {remaining.ToArrayString()}");
                    return true;
                }

                //else span unsuccessful, continue to next potential match
                Console.WriteLine("Span unsuccessful. Continuing to next potential match");
                continue;
            }

            //potential matches exhausted, recursing on next token
            Console.WriteLine("Exhausted potential matches in token. Recursing on next token");
            return TryConsume(token, color, tokenIndex + 1, false, out consumedTokens, out remaining);
        }

        private Regex compile(string token)
        {
            string pattern = ""; //abcde
            foreach (var c in token.Reverse())
            {
                pattern = $"({c}{pattern})?";
            }
            return new Regex(pattern.Substring(0, pattern.Length-1), RegexOptions.ExplicitCapture);
        }

        private string Concat(List<Token> tokens)
        {
            var result = "";
            tokens.ForEach(x => result += x.Text);
            return result;
        }

        public string Remainder()
        {
            return Concat(remainder);
        }


        public List<Token> GetTokens()
        {
            return Tokens.ToList();
        }

        public string ToVanillaString()
        {
            var allTokens = Tokens.Concat(remainder).ToList();
            allTokens.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));

            var result = "";

            foreach (var token in allTokens)
            {
                result += token.ToVanillaString();
            }

            return result;
        }
    }

    public readonly struct Token
    {
        public readonly StyledLine Parent;
        public readonly string Text;
        public readonly int StartIndex;
        public int Length => Text.Length;
        public readonly string Color;

        public Token(StyledLine parent, string text, int startIndex, string color)
        {
            Parent = parent;
            Text = text;
            StartIndex = startIndex;
            Color = color;
        }

        public override string ToString()
        {
            return $"Token(Parent:{Parent}, Text:\"{Text}\", StartIndex:{StartIndex}, Length:{Length}, Color:{Color})";
        }

        public string ToVanillaString()
        {
            return Color == null ? Text : $"<color={Color}>{Text}</color>";
        }
    }
}
