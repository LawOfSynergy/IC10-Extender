using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace IC10_Extender
{

    public class Patches
    {
        private static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static void Apply(Harmony harmony)
        {
            var opCodeLoadingTarget = typeof(ProgrammableChip._LineOfCode)
                .GetConstructor(
                    All,
                    null,
                    new Type[] { typeof(ProgrammableChip), typeof(string), typeof(int) },
                    null
                );
            var opCodeLoadingTranspiler = typeof(Patches)
                .GetMethod("InjectOpCodeLoading", All);
            harmony.Patch(opCodeLoadingTarget, transpiler: new HarmonyMethod(opCodeLoadingTranspiler));

            var highlightSyntaxTarget = typeof(Localization)
                .GetMethod(
                    "ReplaceCommands",
                    All
                );
            var highlightSyntaxPostfix = typeof(Patches)
                .GetMethod("HighlightSyntax", All);
            harmony.Patch(highlightSyntaxTarget, postfix: new HarmonyMethod(highlightSyntaxPostfix));
        }


        public static List<CodeInstruction> InjectOpCodeLoading(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> insert = new List<CodeInstruction>();
            Label normal = generator.DefineLabel();
            Label extended = generator.DefineLabel();
            LocalBuilder ext = generator.DeclareLocal(typeof(ProgrammableChip._Operation));
            var extMethod = typeof(IC10Extender).GetMethod("LoadOpCode", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);

            insert.Add(new CodeInstruction(OpCodes.Ldarg_1));
            insert[0].labels.Add(extended);
            insert.Add(new CodeInstruction(OpCodes.Ldarg_2));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_3));
            insert.Add(new CodeInstruction(OpCodes.Ldloc_S, 4));
            insert.Add(new CodeInstruction(OpCodes.Call, extMethod));
            insert.Add(new CodeInstruction(OpCodes.Stloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Ldloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Brfalse, normal));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_0));
            insert.Add(new CodeInstruction(OpCodes.Ldloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Stfld, typeof(ProgrammableChip._LineOfCode).DeclaredField("Operation")));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_0));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_2));
            insert.Add(new CodeInstruction(OpCodes.Stfld, typeof(ProgrammableChip._LineOfCode).DeclaredField("LineOfCode")));
            insert.Add(new CodeInstruction(OpCodes.Ret));

            var cmatcher = new CodeMatcher(instructions, generator);

            var count = 0;
            cmatcher.MatchStartForward(new CodeMatch(OpCodes.Ldlen))
                .Repeat(cm =>
                {
                    count++;
                    if (count == 3)
                    {
                        cm.Advance(3);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Bne_Un_S, extended) });
                        cm.Advance(6);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Blt_S, extended) });
                        cm.Advance(12);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Bne_Un_S, extended) });
                        cm.Advance(33);
                        cm.Insert(insert);
                        cm.Advance(insert.Count);
                        cm.AddLabels(new List<Label>() { normal });
                    }
                    cm.Advance(1);
                });
            return cmatcher.Instructions();
        }

        public static void HighlightSyntax(ref string masterString)
        {
            if (string.IsNullOrEmpty(masterString)) return;

            var original = masterString;
            try
            {
                string format = "<color={1}>{0}</color>";
                masterString = masterString.TrimStart(out string prefix);
                foreach (var opcode in IC10Extender.OpCodes.Values)
                {
                    string name = opcode.OpCode;
                    int len = opcode.OpCode.Length;
                    if (masterString.Length >= len && masterString.Substring(0, len).Equals(name))
                    {
                        string copy = masterString;
                        if (masterString.Length < name.Length + 1 || masterString[name.Length] == ' ')
                        {

                            int spaceCount = copy.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length - 1;
                            string fragment = masterString.Substring(len, masterString.Length - len);
                            masterString = $"{string.Format(format, name, opcode.Color())}{fragment.TrimEnd()} {opcode.CommandExample("darkgrey", spaceCount)}";
                            break;
                        }
                    }
                }
                masterString = prefix + masterString;
            }catch (Exception ex)
            {
                Plugin.Logger.LogError($"Encountered exception processing \"{original}\"\n{ex}");
                masterString = original;
            }
        }
    }

    public static class Extensions
    {
        public static FieldInfo DeclaredField(this Type type, string name) {
            if ((object)type == null)
            {
                FileLog.Debug("AccessTools.DeclaredField: type is null");
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                FileLog.Debug("AccessTools.DeclaredField: name is null/empty");
                return null;
            }
            var allDeclared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.DeclaredOnly;

            FieldInfo field = type.GetField(name, allDeclared);
            if (field is null)
            {
                FileLog.Debug($"AccessTools.DeclaredField: Could not find field for type {type} and name {name}");
            }

            return field;
        }

        public static string[] Split(this string src, char separator, StringSplitOptions options)
        {
            return src.Split(new char[] {separator }, options);
        }

        public static string TrimStart(this string src, out string prefix)
        {
            var result = src.TrimStart();
            prefix = src.Substring(0, src.IndexOf(result[0]));
            return result;
        }
    }
}
