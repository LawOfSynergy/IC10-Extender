using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using IC10_Extender.Highlight;
using IC10_Extender.operations;
using IC10_Extender.Operations;
using IC10_Extender.Variables;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UI.Tooltips;
using static Assets.Scripts.Objects.Thing;

namespace IC10_Extender.Compat
{
    //patches that work in BOTH branches
    internal static class CommonPatches
    {
        public static ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip._LineOfCode), MethodType.Constructor, new Type[] { typeof(ProgrammableChip), typeof(string), typeof(int) })]
        public static bool LineOfCodePreCTOR(ProgrammableChip._LineOfCode __instance, ProgrammableChip chip, int lineNumber, out bool __state)
        {
            try
            { 
                var type = typeof(ProgrammableChip._LineOfCode);
                var opRef = type.GetField("Operation", BindingFlags.Public | BindingFlags.Instance);

                ChipWrapper wrapper = chip.Wrap();
                CompileContext context = CompileContext.Get(chip);
                Line line = context.Construction[lineNumber];

                var op = line.ForcedOp;

                //if loc was given a forced operation during preprocessing, use that operation
                if (op != null)
                {
                    opRef.SetValue(__instance, new IC10AsLegacyOpWrapper(op));
                    return __state = false;
                }

                //load operation by signature
                if (context.Operations.ContainsKey(line))
                {
                    BoundSignature signature = context.Operations[line];
                    ExtendedOpCode opcode = IC10Extender.OpCode(signature.Signature.Name);

                    //if opcode is registered opcode
                    if (opcode != null)
                    {
                        opRef.SetValue(__instance, new IC10AsLegacyOpWrapper(opcode.Create(context, line, signature, wrapper)));
                        return __state = false;
                    }
                }

                var tokens = line.Raw.Tokenize();
                //if loc is empty, operation is noop
                if (tokens.Count == 0)
                {
                    opRef.SetValue(__instance, new IC10AsLegacyOpWrapper(new NoOpOperation(line)));
                    return __state = false;
                }

                //opcode is not in this library, either vanilla or added by non-ic10e mod
                return __state = true; //if none of the above, default to vanilla execution, then wrap the operation in a postfix
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
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

            ChipWrapper wrapper = chip.Wrap();
            CompileContext context = CompileContext.Get(chip);
            Line line = context.Construction[lineNumber];

            if (__state) //vanilla logic ran. Wrap in OpContext
            {
                opRef.SetValue(
                    __instance, 
                    new IC10AsLegacyOpWrapper(
                        new LegacyAsIC10OpWrapper(
                            context, 
                            line, 
                            context.Operations.GetValueOrDefault(line), 
                            wrapper, 
                            (ProgrammableChip._Operation)opRef.GetValue(__instance)
                        )
                    )
                );
            }

            // set LineOfCode to display value now that operation has finished constructing, regardless of modded or vanilla
            lineRef.SetValue(__instance, line.Display.ToString(HelpString.VanillaFormat));
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "SetSourceCode", new Type[] { typeof(string), typeof(ICircuitHolder) })]
        public static bool WriteWithCircuitHolder(string sourceCode, ICircuitHolder parent)
        {
            var parentContext = CompileContext.Get(parent) ?? CompileContext.Compile(sourceCode);
            CompileContext.Store(parent, parentContext);
            return true; //execute vanilla version, which is just ClearError() and this.SetSourceCode(script) (which will become CommonPatches.WriteToChip())
        }

        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "SetSourceCode", new Type[] { typeof(string) })]
        public static bool WriteToChip(ProgrammableChip __instance, string sourceCode)
        {
            var parentContext = CompileContext.Get(__instance.CircuitHousing) ?? CompileContext.Compile(sourceCode);
            CompileContext.Write(parentContext, __instance);
            return false; //skip vanilla version
        }


        //[HarmonyDebug]
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(EditorLineOfCode), "ReformatText", new Type[] { typeof(string) })]
        //public static bool ReformatTextOverride(EditorLineOfCode __instance, string sourceCode)
        //{

        //}


        [HarmonyDebug]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProgrammableChip), "Execute", new Type[] { typeof(int) })]
        public static bool OverridePCExecute(ProgrammableChip __instance, int runCount)
        {
            ChipWrapper chip = __instance.Wrap();
            if (chip.NextAddr < 0 || chip.NextAddr >= chip.LinesOfCode.Count || chip.LinesOfCode.Count == 0) return false;

            int num = runCount;

            var ops = chip.RawOperations;

            while (num-- > 0 && chip.NextAddr >= 0 && chip.NextAddr < chip.LinesOfCode.Count)
            {
                int nextAddr = chip.NextAddr;
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
                    chip.RuntimeException = ex.Wrap(nextAddr);
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
            if (chip.CompileException is ExtendedPCException cex)
            {
                actionInstance.AppendStateMessage(cex.ToString());
                return false; //skip original
            }
            if (chip.RuntimeException is ExtendedPCException rex)
            {
                actionInstance.AppendStateMessage(rex.ToString());
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
        public static bool GetErrorCode(ProgrammableChip __instance, ref string __result)
        {
            ChipWrapper chip = __instance.Wrap();
            if (chip.CompileException is ExtendedPCException cex)
            {
                __result = cex.ToString() + "\n";
                return false; //skip original
            }
            if (chip.RuntimeException is ExtendedPCException rex)
            {
                __result = rex.ToString() + "\n";
                return false; //skip original
            }
            return true; //run original
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
                    ProgrammableChip.AllConstants.Do(constant =>
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
                Log.Error(ex.ToString());
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
                    IC10Extender.OpCode(searchText).HelpPage.SetVisible(true);
                    return false; //doExtendedSearch = false
                }

                return true;
            });
            c.Emit(OpCodes.Stloc_0);
        }

        //[HarmonyDebug]
        //[HarmonyILManipulator]
        //[HarmonyPatch(typeof(EditorLineOfCode), nameof(EditorLineOfCode.HandleUpdate))]
        //public static void EmbedOnHoverHelpWindow(ILContext il, ILLabel returnLabel)
        //{
        //    var c = new ILCursor(il);

        //    c.GotoNext(MoveType.Before,
        //        x => x.MatchLdsfld(typeof(EnumCollections).GetField(nameof(EnumCollections.ScriptCommands))),
        //        x => x.MatchLdfld(typeof(EnumCollection<ScriptCommand, int>).GetField("Values"))
        //    )
        //    .Emit(OpCodes.Ldloc_2)
        //    .EmitDelegate<Func<string, bool>>(word =>
        //    {
        //        var opcode = IC10Extender.OpCode(word);
        //        if (opcode != null && !opcode.Deprecated)
        //        {

        //            var str = new StringBuilder($"<color={Theme.Default.String}><b>Instruction</b></color>\n<i>{opcode.Signature.CommandExample(Theme.Default)}</i>\n");
        //            StringManager.WrapLineLength(str, opcode.Description(), 70, Theme.Default.Description);
        //            UITooltipManager.SetTooltip(str);
        //            return false;
        //        }
        //        return true; //continue with vanilla logic
        //    });

        //    c.Emit(OpCodes.Brfalse, returnLabel);
        //}
    }
}
