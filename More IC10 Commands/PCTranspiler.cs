using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace IC10_Extender
{
    [HarmonyPatch(typeof(ProgrammableChip._LineOfCode))]
    [HarmonyPatch(new Type[]{typeof(ProgrammableChip), typeof(string), typeof(int)})]
    public class PCTranspiler
    {
        

        [HarmonyTranspiler]
        static List<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> insert = new List<CodeInstruction>();
            Label normal = generator.DefineLabel();
            Label extended = generator.DefineLabel();
            LocalBuilder ext = generator.DeclareLocal(typeof(ProgrammableChip._Operation));

            insert.Add(new CodeInstruction(OpCodes.Ldarg_1));
            insert[0].labels.Add(extended);
            insert.Add(new CodeInstruction(OpCodes.Ldarg_2));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_3));
            insert.Add(new CodeInstruction(OpCodes.Ldloca_S, 4));
            insert.Add(CodeInstruction.Call(typeof(IC10Extender), "LoadExtension"));
            insert.Add(new CodeInstruction(OpCodes.Stloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Ldloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Brfalse, normal));
            insert.Add(new CodeInstruction(OpCodes.Ldloc, ext.LocalIndex));
            insert.Add(new CodeInstruction(OpCodes.Stfld, typeof(ProgrammableChip._LineOfCode).DeclaredField("Operation")));
            insert.Add(new CodeInstruction(OpCodes.Ldarg_2));
            insert.Add(new CodeInstruction(OpCodes.Stfld, typeof(ProgrammableChip._LineOfCode).DeclaredField("LineOfCode")));
            insert.Add(new CodeInstruction(OpCodes.Ret));

            var cm = new CodeMatcher(instructions, generator);

            var count = 0;
            cm.MatchStartForward(CodeMatch.WithOpcodes(new HashSet<OpCode> { OpCodes.Ldlen }))
                .Repeat(cm =>
                {
                    count++;
                    if (count == 2)
                    {
                        cm.Advance(3);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Bne_Un_S, extended) });
                        cm.Advance(6);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Bne_Un_S, extended) });
                        cm.Advance(12);
                        cm.RemoveInstruction();
                        cm.Insert(new CodeInstruction[] { new CodeInstruction(OpCodes.Bne_Un_S, extended) });
                        cm.Advance(34);
                        cm.Insert(insert);
                        cm.Advance(insert.Count);
                        cm.AddLabels(new List<Label>() { normal });
                    }
                });
            return cm.Instructions();
        }
    }
}
