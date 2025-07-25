using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.SqlServer.Server;
using Reagents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;
using static IC10_Extender.PreprocessorOperation;

namespace IC10_Extender
{

    public class Patches
    {
        public static void Apply(Harmony harmony)
        {
            Plugin.Logger.LogInfo("Patching ProgrammableChip._LineOfCode.<ctor>");
            harmony.Patch(
                typeof(ProgrammableChip._LineOfCode).GetConstructor(typeof(ProgrammableChip), typeof(string), typeof(int)),
                prefix: new HarmonyMethod(typeof(Patches).GetMethod("LineOfCodeCTOR", typeof(ProgrammableChip._LineOfCode), typeof(ProgrammableChip), typeof(int)), debug: true)
            );

            Plugin.Logger.LogInfo("Patching ProgrammableChip.SetSourceCode");
            harmony.Patch(
                typeof(ProgrammableChip).GetMethod("SetSourceCode",typeof(string)),
                prefix: new HarmonyMethod(typeof(Patches).GetMethod("SetSourceCode", typeof(ProgrammableChip), typeof(string)), debug: true)
            );

            Plugin.Logger.LogInfo("Patching Localization.ReplaceCommands");
            harmony.Patch(
                typeof(Localization).GetMethod("ReplaceCommands", Extensions.All),
                prefix: new HarmonyMethod(typeof(Patches).GetMethod("HighlightSyntax", Extensions.All), debug: true)
            );

            Plugin.Logger.LogInfo("Patching Localization.ParseScriptLine");
            harmony.Patch(
                typeof(Localization).GetMethod("ParseScriptLine", Extensions.All),
                postfix: new HarmonyMethod(typeof(Patches).GetMethod("ApplyColors", Extensions.All), debug: true)
            );

            Plugin.Logger.LogInfo("Patching ScriptHelpWindow.Initialize");
            harmony.Patch(
                typeof(ScriptHelpWindow).GetMethod("Initialize", new Type[0]),
                prefix: new HarmonyMethod(typeof(Patches).GetMethod("InitHelpPages", typeof(ScriptHelpWindow)), debug: true)
            );
        }

        public static bool LineOfCodeCTOR(ProgrammableChip._LineOfCode __instance, ProgrammableChip chip, int lineNumber)
        {
            try
            { 
                var type = typeof(ProgrammableChip._LineOfCode);
                var lineRef = type.GetField("LineOfCode", BindingFlags.Public | BindingFlags.Instance);
                var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);

                lineRef?.SetValue(__instance, lineNumber.ToString());

                Line line = ConstructionContext.Get(chip, lineNumber);
                Plugin.Logger.LogInfo($"Loading preprocessed line info:\n{line.OriginatingLineNumber}: \"{line.Raw}\", op={line.Op}");
                var op = line.Op;

                if (op != null)
                {
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)op);
                    return false;
                }

                var tokens = line.Raw.Split().Where(token => !string.IsNullOrEmpty(token)).ToArray();
                if (tokens.Length == 0)
                {
                    opRef?.SetValue(__instance, new ProgrammableChip._NOOP_Operation(chip, lineNumber));
                    return false;
                }

                var opCode = IC10Extender.OpCode(tokens[0]);
                if (opCode != null)
                {
                    opRef.SetValue(__instance, (ProgrammableChip._Operation)opCode.Create(new ChipWrapper(chip), lineNumber, tokens));
                    return false;
                }

                throw new ProgrammableChipException(ICExceptionType.UnrecognisedInstruction, lineNumber);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
                throw ex.Wrap(lineNumber);
            }
        }

        public static bool SetSourceCode(ProgrammableChip __instance, string sourceCode)
        {
            try
            {
                __instance._LinesOfCode.Clear();
                __instance._Aliases.Clear();
                __instance._Defines.Clear();
                __instance._JumpTags.Clear();
                __instance._ErrorType = ICExceptionType.None;
                __instance._ErrorLineNumber = 0;
                __instance.CompileErrorType = ICExceptionType.None;
                __instance.CompileErrorLineNumber = (ushort)0;
                __instance._Registers[__instance._StackPointerIndex] = 0.0;
                if (string.IsNullOrEmpty(sourceCode))
                    sourceCode = string.Empty;
                __instance.SourceCode = new AsciiString(sourceCode);
                if (__instance.CircuitHousing != null)
                {
                    __instance.CircuitHousing.ClearError();
                    new ProgrammableChip._ALIAS_Operation(__instance, 0, "db", string.Format("d{0}", int.MaxValue)).Execute(0, false);
                    new ProgrammableChip._ALIAS_Operation(__instance, 0, "sp", string.Format("r{0}", __instance._StackPointerIndex)).Execute(0);
                    new ProgrammableChip._ALIAS_Operation(__instance, 0, "ra", string.Format("r{0}", __instance._ReturnAddressIndex)).Execute(0);
                }
                var lines = sourceCode.Split('\n', StringSplitOptions.None).Select((line, index) => new Line(line, (ushort)index));

                try
                {
                    foreach (var preprocessor in IC10Extender.Preprocessors)
                    {
                        lines = preprocessor.Create(new ChipWrapper(__instance)).Process(lines).ToList();
                    }

                    ConstructionContext.Store(__instance, lines);
                    var tmp = lines.ToArray();
                    for (var i = 0; i < tmp.Count(); i++)
                    {
                        try
                        {
                            var line = tmp[i];
                            __instance._LinesOfCode.Add(new ProgrammableChip._LineOfCode(__instance, line.Raw, i));
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger.LogError(ex);
                            throw ex.Wrap(i);
                        }
                    }
                }
                catch(ProgrammableChipException ex)
                {
                    __instance.CircuitHousing?.RaiseError(1);
                    __instance.CompileErrorLineNumber = ex.LineNumber;
                    __instance.CompileErrorType = ex.ExceptionType;
                    Plugin.Logger.LogError(ex);
                }

                ConstructionContext.Clear(__instance);
                __instance._NextAddr = 0;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
                return true;
            }
        }

        public static bool HighlightSyntax(ref string masterString, ref List<string> acceptedStrings, ref List<string> acceptedJumps, EditorLineOfCode line = null)
        {
            try
            {
                string format = "<color={1}>{0}</color>";

                if (string.IsNullOrWhiteSpace(masterString)) return false;
                foreach (IScriptEnum internalEnum in ProgrammableChip.InternalEnums)
                    internalEnum.Parse(ref masterString);
                foreach (Reagent allReagent in Reagent.AllReagents)
                    masterString = masterString.ReplaceWholeWord(allReagent.TypeNameShort, string.Format(format, allReagent.DisplayName, "orange"));
                Localization.ReplaceCommandStringA2(ref masterString, "alias", "<color=yellow>{1}</color> <color=colorstring>{0}</color> {2}");
                Localization.ReplaceCommandStringA2(ref masterString, "define", "<color=yellow>{1}</color> <color=colorstring>{0}</color> {2}");
                Localization.ReplaceJumpString(ref masterString, "<color=purple>{0}</color>");
                foreach (string wordToFind in acceptedStrings)
                    masterString = masterString.ReplaceWholeWord(wordToFind, string.Format("<color={1}>{0}</color>", wordToFind, "colorstring"), line);
                foreach (string wordToFind in acceptedJumps)
                    masterString = masterString.ReplaceWholeWord(wordToFind, string.Format("<color={1}>{0}</color>", wordToFind, "purple"), line);

                var original = masterString;
                try
                {

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
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Encountered exception processing \"{original}\"\n{ex}");
                    masterString = original;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
                return true;
            }

            return false;
        }

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

        public static bool InitHelpPages(ScriptHelpWindow __instance)
        {
            try
            {
                if (__instance.HelpMode == HelpMode.Functions)
                {
                    if (__instance.Description)
                        __instance.Description.text = ProgrammableChip.GetIntroString();
                    IC10Extender.Preprocessors.Do(preprocessor => preprocessor.InitHelpPage(__instance));
                    IC10Extender.Constants.Values.Do(constant =>
                    {
                        HelpReference helpReference = UnityEngine.Object.Instantiate(__instance.ReferencePrefab, __instance.FunctionTransform);
                        helpReference.Setup(constant, __instance.DefaultItemImage2);
                        __instance._helpReferences.Add(helpReference);
                    });

                    var sorted = IC10Extender.OpCodes.Values.ToList();
                    sorted.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
                    sorted.Do(opcode => opcode.InitHelpPage(__instance));

                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError (ex);
                return true;
            }
        }

        public static void ShowHelpPage(ScriptHelpWindow window, string opcode)
        {
            window.ClearPreviousSearch();
            IC10Extender.OpCode(opcode)?.HelpPage().SetVisible(true);
        }
    }
}
