using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using IC10_Extender.Operations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UI.Tooltips;
using static Assets.Scripts.Objects.Thing;

namespace IC10_Extender.Compat
{
    //patches that work in BOTH branches
    public static class CommonPatches
    {
        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip._LineOfCode), MethodType.Constructor, new Type[] { typeof(ProgrammableChip), typeof(string), typeof(int) })]
        public static bool LineOfCodePreCTOR(ProgrammableChip._LineOfCode __instance, ProgrammableChip chip, int lineNumber, out bool __state)
        {
            try
            { 
                var type = typeof(ProgrammableChip._LineOfCode);
                var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);

                Line line = ConstructionContext.Get(chip, lineNumber);

                var op = line.ForcedOp;

                //if line was given a forced operation during preprocessing, use that operation
                if (op != null)
                {
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(op, line));
                    return __state = false;
                }

                var tokens = line.Raw.Split().Where(token => !string.IsNullOrEmpty(token)).ToArray();
                //if line is empty, operation is noop
                if (tokens.Length == 0)
                {
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(new ProgrammableChip._NOOP_Operation(chip, lineNumber), line));
                    return __state = false;
                }

                var opCode = IC10Extender.OpCode(tokens[0]);
                //if opcode exists in this library, use that opcode
                if (opCode != null)
                {
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(opCode.Create(chip.Wrap(), lineNumber, tokens), line));
                    return __state = false;
                }

                return __state = true; //if none of the above, default to vanilla execution, then wrap the operation in a postfix
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
                throw ex.Wrap(lineNumber);
            }
        }

        [HarmonyDebug]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProgrammableChip._LineOfCode), MethodType.Constructor, new Type[] { typeof(ProgrammableChip), typeof(string), typeof(int) })]
        public static void LineOfCodePostCTOR(ProgrammableChip._LineOfCode __instance, ProgrammableChip chip, int lineNumber, ref bool __state)
        {
            var type = typeof(ProgrammableChip._LineOfCode);
            var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);
            var lineRef = type.GetField("LineOfCode", BindingFlags.Public | BindingFlags.Instance);
            Line line = ConstructionContext.Get(chip, lineNumber);
            
            if(__state) //vanilla logic ran. Wrap in OpContext
            {
                opRef.SetValue(__instance, new OperationWrapper(new OpContext(new ReverseWrapper((ProgrammableChip._Operation)opRef.GetValue(__instance)), line)));
            }

            Plugin.Logger.LogInfo($"Line.Raw: {line.Raw}, Line.Display:{line.Display}, Before set: {__instance.LineOfCode}");
            // set LineOfCode to display value now that operation has finished constructing, regardless of modded or vanilla
            lineRef.SetValue(__instance, line.Display);
            Plugin.Logger.LogInfo($"After set: {__instance.LineOfCode}");

            // clean up construction state regardless of modded or vanilla
            ConstructionContext.Remove(chip, lineNumber);
        }

        [HarmonyDebug]
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(ProgrammableChip), "SetSourceCode", new Type[] {typeof(string)})]
        public static void SetSourceCode(ILContext il, ILLabel returnLabel)
        {
            var c = new ILCursor(il);

            c
            .GotoNext(MoveType.After,
                x => x.MatchCallvirt(typeof(string).GetMethod(nameof(string.Split), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(char), typeof(StringSplitOptions) }, null)),
                x => x.MatchStloc(0)
            )
            .Emit(OpCodes.Ldarg_0)
            .Emit(OpCodes.Ldloc_0)
            .EmitDelegate<Action<ProgrammableChip, string[]>>((chip, src) =>
            {
                //force construction of a new wrapper and invalidate old one to allow resource cleanup
                var wrapper = chip.Wrap(true);
                try {
                    var lines = src.Select((line, index) => new Line(line, (ushort)index));

                    foreach (var preprocessor in IC10Extender.Preprocessors)
                    {
                        lines = preprocessor.Create(wrapper).Process(lines).ToList();
                    }

                    chip.SourceCode = new AsciiString(string.Join("\n", lines.Select(line => line.Display).ToArray()));

                    ConstructionContext.Store(chip, lines);
                    var tmp = lines.ToArray();
                    for (var i = 0; i < tmp.Count(); i++)
                    {
                        try
                        {
                            var line = tmp[i];
                            chip._LinesOfCode.Add(new ProgrammableChip._LineOfCode(chip, line.Raw, i));
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger.LogError(ex);
                            throw ex.Wrap(i);
                        }
                    }
                    ConstructionContext.Clear(chip);
                }
                catch (ProgrammableChipException ex)
                {
                    wrapper.CompileException = ex;
                }
                ConstructionContext.Clear(chip);
                
                chip._NextAddr = 0;
            });
            c.Emit(OpCodes.Br, returnLabel);
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "Execute", new Type[] { typeof(int) })]
        public static bool OverridePCExecute(ProgrammableChip __instance, int runCount)
        {
            ChipWrapper chip = __instance.Wrap();
            if (chip.NextAddr < 0 || chip.NextAddr >= chip.LinesOfCode.Count || chip.LinesOfCode.Count == 0) return false;

            int nextAddr1 = chip.NextAddr;
            int num = runCount;

            var ops = chip.Operations;

            while (num-- > 0 && chip.NextAddr >= 0 && chip.NextAddr < chip.LinesOfCode.Count)
            {
                int nextAddr2 = chip.NextAddr;
                try
                {
                    chip.NextAddr = ops[chip.NextAddr].Execute(chip.NextAddr);
                }
                catch (ProgrammableChipException ex)
                {
                    chip.RuntimeException = ex;
                    break;
                }
                catch (Exception ex)
                {
                    chip.RuntimeException = ex.Wrap(nextAddr2);
                    break;
                }

                if (chip.CircuitHousing != null)
                {
                    chip.ClearException();
                }

                if (chip.NextAddr < 0)
                {
                    chip.NextAddr = -chip.NextAddr;
                    break;
                }
            }

            return false; //skip original
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "AppendErrorsToActionInstance", new Type[] { typeof(DelayedActionInstance) })]
        public static bool ExtendErrorReporting(ProgrammableChip __instance, DelayedActionInstance actionInstance)
        {
            ChipWrapper chip = __instance.Wrap();
            ExtendedPCException ex;
            if ((ex = chip.CompileException as ExtendedPCException) != null)
            {
                actionInstance.AppendStateMessage(ex.ToString());
                return false; //skip original
            }
            if ((ex = chip.RuntimeException as ExtendedPCException) != null)
            {
                actionInstance.AppendStateMessage(ex.ToString());
                return false; //skip original
            }
            return true; //run original
        }

        [HarmonyDebug]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProgrammableChip), "Reset", new Type[] {})]
        public static void OnResetHook(ProgrammableChip __instance)
        {
            __instance.Wrap().Reset();
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "GetErrorCode", new Type[] { })]
        public static bool GetErrorCode(ProgrammableChip __instance, string __result)
        {
            ChipWrapper chip = __instance.Wrap();
            ExtendedPCException ex;
            if ((ex = chip.CompileException as ExtendedPCException) != null)
            {
                __result = ex.ToString() + "\n";
                return false; //skip original
            }
            if ((ex = chip.RuntimeException as ExtendedPCException) != null)
            {
                __result = ex.ToString() + "\n";
                return false; //skip original
            }
            return true; //run original
        }

        [HarmonyDebug]
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(Localization), nameof(Localization.ReplaceNumbers))]
        public static void SkipNativeNumberPreprocessorHighlighting(ILContext il, ILLabel returnLabel)
        {
            ILCursor c = new ILCursor(il);

            ILLabel jump = c.DefineLabel();

            c
            .Emit(OpCodes.Br, jump)
            .GotoNext(MoveType.AfterLabel, x => x.MatchLdarg(0), x => x.MatchCall(typeof(Localization).GetMethod(nameof(Localization.GetMatchesForNumbers), Extensions.AllDeclared)))
            .MarkLabel(jump);
        }

        [HarmonyDebug]
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(Localization), nameof(Localization.ParseScriptLine))]
        public static void HighlightSyntax(ILContext il, ILLabel returnLabel)
        {
            ILCursor c = new ILCursor(il);

            c
            .GotoNext(MoveType.After, x => x.MatchCall(typeof(Localization).GetMethod(nameof(Localization.ReplaceDeviceReferences), Extensions.AllDeclared)))
            .Emit(OpCodes.Ldloca, 1)
            .Emit(OpCodes.Ldarg_1)
            .Emit(OpCodes.Ldarg_2)
            .Emit(OpCodes.Ldarg_3)
            .Emit(OpCodes.Call, typeof(CommonPatches).GetMethod(nameof(ApplyHighlight), Extensions.AllDeclared));
        }

        private static void ApplyHighlight(ref string masterString, ref List<string> acceptedStrings, ref List<string> acceptedJumps, EditorLineOfCode line = null)
        {
            foreach (var preprocessor in IC10Extender.Preprocessors)
            {
                masterString = preprocessor.Highlighter().Highlight(masterString);
            }
            
            string format = "<color={1}>{0}</color>";


            var original = masterString;

            masterString = masterString.TrimStart(out string prefix);

            int index = masterString.IndexOfWhitespace();
            masterString = ProgrammableChip.StripColorTags(index == -1 ? masterString : masterString.Substring(0, index)) + (index == -1 ? string.Empty : masterString.Substring(index));

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
                        masterString = $"{prefix}{string.Format(format, name, opcode.Color())}{fragment.TrimEnd()} {opcode.CommandExample(spaceCount, "darkgrey")}";
                        return;
                    }
                }
            }
            
            masterString = original;
        }

        [HarmonyDebug]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Localization), nameof(Localization.ParseScriptLine))]
        public static void ApplyColors(ref string __result)
        {
            try
            {
                foreach (var entry in IC10Extender.Colors)
                {
                    __result = __result.Replace($"<color={entry.Key}>", $"<color={entry.Value}>");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScriptHelpWindow), nameof(ScriptHelpWindow.Initialize))]
        public static bool InitHelpPages(ScriptHelpWindow __instance)
        {
            try
            {
                if (__instance.HelpMode == HelpMode.Functions)
                {
                    if (__instance.Description)
                        __instance.Description.text = ProgrammableChip.GetIntroString();
                    IC10Extender.Preprocessors.Do(preprocessor => preprocessor.InitHelpPage(__instance));
                    ProgrammableChip.AllConstants.Concat(IC10Extender.Constants.Values).Do(constant =>
                    {
                        HelpReference helpReference = UnityEngine.Object.Instantiate(__instance.ReferencePrefab, __instance.FunctionTransform);
                        helpReference.Setup(constant, __instance.DefaultItemImage2);
                        __instance._helpReferences.Add(helpReference);
                    });

                    var sorted = IC10Extender.OpCodes.Values.Select(op => op.OpCode).Concat(EnumCollections.ScriptCommands.Values.Select(op => op.ToString())).ToList();
                    sorted.Sort((a, b) => string.Compare(a, b, StringComparison.Ordinal));
                    sorted.Do(opcode => 
                    {
                        var inst = IC10Extender.OpCode(opcode);
                        if (inst != null) 
                        {
                            if (!inst.Deprecated) 
                            { 
                                inst.InitHelpPage(__instance);
                            }
                        } 
                        else
                        {
                            ScriptCommand inst2 = (ScriptCommand)Enum.Parse(typeof(ScriptCommand), opcode);
                            if(!LogicBase.IsDeprecated(inst2))
                            {
                                HelpReference helpReference = UnityEngine.Object.Instantiate(__instance.ReferencePrefab, __instance.FunctionTransform);
                                helpReference.Setup(inst2, __instance.DefaultItemImage);
                                __instance._helpReferences.Add(helpReference);
                            }
                        }
                    });

                    return false; // Do not execute original, this superceded the original
                }
                return true; // Continue with the original. This is not the particular instance we are targeting
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError (ex);
                return true; //Something went wrong. Revert to the original implementation
            }
        }

        [HarmonyDebug]
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(ScriptHelpWindow), nameof(ScriptHelpWindow.ForceSearch))]
        public static void ShowHelpPage(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt(typeof(string).GetProperty(nameof(string.Length)).GetGetMethod())
            )
            .Emit(OpCodes.Ldarg_0)
            .Emit(OpCodes.Ldarg_1)
            .EmitDelegate<Func<ScriptHelpWindow, string, bool>>((window, searchText) =>
            {
                if (IC10Extender.HasOpCode(searchText))
                {
                    window.ClearPreviousSearch();
                    IC10Extender.OpCode(searchText).HelpPage().SetVisible(true);
                    return false; //doExtendedSearch = false
                }

                return true;
            });
            c.Emit(OpCodes.Stloc_0);
        }

        [HarmonyDebug]
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(EditorLineOfCode), nameof(EditorLineOfCode.HandleUpdate))]
        public static void EmbedOnHoverHelpWindow(ILContext il, ILLabel returnLabel)
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld(typeof(EnumCollections).GetField(nameof(EnumCollections.ScriptCommands))),
                x => x.MatchLdfld(typeof(EnumCollection<ScriptCommand, int>).GetField("Values"))
            )
            .Emit(OpCodes.Ldloc_2)
            .EmitDelegate<Func<string, bool>>(word =>
            {
                var opcode = IC10Extender.OpCode(word);
                if (opcode != null && !opcode.Deprecated)
                {

                    var str = new StringBuilder($"<color=white><b>Instruction</b></color>\n<i>{opcode.CommandExample()}</i>\n");
                    StringManager.WrapLineLength(str, opcode.Description(), 70, "grey");
                    UITooltipManager.SetTooltip(str);
                    return false;
                }
                return true; //continue with vanilla logic
            });

            c.Emit(OpCodes.Brfalse, returnLabel);
        }
    }
}
