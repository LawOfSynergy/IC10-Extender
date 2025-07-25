using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    public static class Extensions
    {
        public static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static readonly BindingFlags AllDeclared = All | BindingFlags.DeclaredOnly;

        public static FieldInfo DeclaredField(this Type type, string name)
        {
            if ((object)type == null)
            {
                Plugin.Logger?.LogDebug("AccessTools.DeclaredField: type is null");
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                Plugin.Logger?.LogDebug("AccessTools.DeclaredField: name is null/empty");
                return null;
            }

            FieldInfo field = type.GetField(name, AllDeclared);
            if (field is null)
            {
                Plugin.Logger?.LogDebug($"AccessTools.DeclaredField: Could not find field for type {type} and name {name}");
            }

            return field;
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

        public static ProgrammableChipException Wrap(this Exception ex, int lineNumber)
        {
            switch(ex)
            {
                case ProgrammableChipException pce: return pce;
                default: return new ProgrammableChipException(ICExceptionType.Unknown, lineNumber);
            }
        }
    }
}
