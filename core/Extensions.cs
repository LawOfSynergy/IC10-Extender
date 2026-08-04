using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using IC10_Extender.Highlight;
using IC10_Extender.Variables;
using IC10_Extender.Wrappers;
using static Assets.Scripts.Localization;

namespace IC10_Extender
{
    public static class Extensions
    {
        public static ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        public const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public const BindingFlags AllDeclared = All | BindingFlags.DeclaredOnly;

        public static FieldInfo DeclaredField(this Type type, string name)
        {
            if ((object)type == null)
            {
                Log.Debug("AccessTools.DeclaredField: type is null");
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                Log.Debug("AccessTools.DeclaredField: name is null/empty");
                return null;
            }

            FieldInfo field = type.GetField(name, AllDeclared);
            if (field is null)
            {
                Log.Debug($"AccessTools.DeclaredField: Could not find field for type {type} and name {name}");
            }

            return field;
        }

        /**
         * internal to restrict invalidation/recreation to this framework only
         */
        internal static ChipWrapper Wrap(this ProgrammableChip chip, bool forceNew = false)
        {
            return ChipWrapper.For(chip, forceNew);
        }

        /**
         * public overload that does not force recreation
         */
        public static ChipWrapper Wrap(this ProgrammableChip chip)
        {
            return ChipWrapper.For(chip);
        }

        public static void Print(this Exception ex, ILogger log)
        {
            switch(ex)
            {
                case ExtendedPCException epce:
                    log.Warning($"Error {epce.GetType().Name} at line {epce.LineNumber}: {epce.Message}\n{epce.StackTrace}");
                    break;
                case ProgrammableChipException pce:
                    log.Warning($"Error {pce.GetType().Name} at line {pce.LineNumber}: {pce.ExceptionType}\n{pce.StackTrace}");
                    break;
                default:
                    log.Warning(ex.ToString());
                    break;
            }
        }

        public static MethodInfo GetMethod(this Type type, string name, params Type[] argTypes)
        {
            return type.GetMethod(name, AllDeclared, null, argTypes, null);
        }

        public static ConstructorInfo GetConstructor(this Type type, params Type[] argTypes)
        {
            return type.GetConstructor(AllDeclared, null, argTypes, null);
        }

        public static string[] Split(this string src, char separator, StringSplitOptions options)
        {
            return src.Split(new char[] { separator }, options);
        }

        public static string TrimStart(this string src, out string prefix)
        {
            var result = src.TrimStart();
            if (!string.IsNullOrEmpty(result))
            {
                prefix = src.Substring(0, src.IndexOf(result[0]));
            }
            else
            {
                prefix = string.Empty;
            }
            return result;
        }

        public static int IndexOfWhitespace(this string src)
        {
            for (int i = 0; i < src.Length; i++)
            {
                if (char.IsWhiteSpace(src[i])) return i;
            }
            return -1;
        }

        public static ProgrammableChipException Wrap(this Exception ex, int lineNumber)
        {
            switch(ex)
            {
                case ProgrammableChipException pce: return pce;
                default: return new WrappedException(lineNumber, ex);
            }
        }

        public static RegexResult GetMatches(this Regex regex, string masterString)
        {
            string input = Regexes.CommentLite.Replace(masterString, "");
            MatchCollection matchCollection = regex.Matches(input);
            RegexResult regexResult = new RegexResult();
            regexResult.Full = new string[matchCollection.Count];
            regexResult.Names = new string[matchCollection.Count];
            for (int i = 0; i < matchCollection.Count; i++)
            {
                regexResult.Full[i] = matchCollection[i].Groups[0].Value;
                regexResult.Names[i] = matchCollection[i].Groups[1].Value;
            }

            return regexResult;
        }

        public static IEnumerable<Tuple<T1, T2>> Associate<T1, T2>(this IEnumerable<T1> a, IEnumerable<T2> b)
        {
            var aIter = a.GetEnumerator();
            var bIter = b.GetEnumerator();

            bool aHasNext, bHasNext;

            aHasNext = aIter.MoveNext();
            bHasNext = bIter.MoveNext();

            while(aHasNext || bHasNext)
            {
                yield return new Tuple<T1, T2>(aHasNext ? aIter.Current : default, bHasNext ? bIter.Current : default);
                aHasNext = aIter.MoveNext();
                bHasNext = bIter.MoveNext();
            }
        }

        public static string Concat(this IEnumerable<StyledString> tokens, Func<StyledString, string> formatter, string separator = " ")
        {
            var result = "";
            tokens.ToList().ForEach(x => result += formatter(x) + separator);

            return result.Substring(0, result.Length - separator.Length);
        }
    }
}
