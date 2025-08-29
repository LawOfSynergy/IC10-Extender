﻿using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.SqlServer.Server;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Reagents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UI.Tooltips;
using UnityEngine;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;
using static Networking.Servers.WorldPrefs.UI;
using static UnityEngine.Random;

namespace IC10_Extender
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
                var lineRef = type.GetField("LineOfCode", BindingFlags.Public | BindingFlags.Instance);
                var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);

                Line line = ConstructionContext.Get(chip, lineNumber);

                var op = line.ForcedOp;

                //if line was given a forced operation during preprocessing, use that operation
                if (op != null)
                {
                    lineRef.SetValue(__instance, line.Raw);
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(op, line));
                    return __state = false;
                }

                var tokens = line.Raw.Split().Where(token => !string.IsNullOrEmpty(token)).ToArray();
                //if line is empty, operation is noop
                if (tokens.Length == 0)
                {
                    lineRef.SetValue(__instance, line.Raw);
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(new ProgrammableChip._NOOP_Operation(chip, lineNumber), line));
                    return __state = false;
                }

                var opCode = IC10Extender.OpCode(tokens[0]);
                //if opcode exists in this library, use that opcode
                if (opCode != null)
                {
                    lineRef.SetValue(__instance, line.Raw);
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)new OpContext(opCode.Create(new ChipWrapper(chip), lineNumber, tokens), line));
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
            if(__state) //vanilla logic ran. Wrap in OpContext
            {
                var type = typeof(ProgrammableChip._LineOfCode);
                var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);
                Line line = ConstructionContext.Get(chip, lineNumber);

                opRef.SetValue(__instance, new OperationWrapper(new OpContext(new ReverseWrapper((ProgrammableChip._Operation)opRef.GetValue(__instance)), line)));
            }

            // clean up construction state regardless of modded or vanilla
            ConstructionContext.Remove(chip, lineNumber);
        }

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
                try {
                    var lines = src.Select((line, index) => new Line(line, (ushort)index));

                    foreach (var preprocessor in IC10Extender.Preprocessors)
                    {
                        lines = preprocessor.Create(new ChipWrapper(chip)).Process(lines).ToList();
                    }

                    chip.SourceCode = new AsciiString(string.Join("\n", lines.Select(line => line.Raw).ToArray()));

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
                    chip.CircuitHousing?.RaiseError(1);
                    chip.CompileErrorLineNumber = ex.LineNumber;
                    chip.CompileErrorType = ex.ExceptionType;
                }
                ConstructionContext.Clear(chip);
                
                chip._NextAddr = 0;
            });
            c.Emit(OpCodes.Br, returnLabel);
        }

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
            foreach(var preprocessor in IC10Extender.Preprocessors)
            {
                masterString = preprocessor.Highlighter().Highlight(masterString);
            }
            
            string format = "<color={1}>{0}</color>";


            var original = masterString;

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
                        masterString = $"{string.Format(format, name, opcode.Color())}{fragment.TrimEnd()} {opcode.CommandExample(spaceCount, "darkgrey")}";
                        break;
                    }
                }
            }
            masterString = prefix + masterString;
        }

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

    //patches that ONLY work in the release branch
    public static class ReleasePatches
    {

    }

    //patches that ONLY work in the beta branch
    public static class BetaPatches
    {

    }
}
