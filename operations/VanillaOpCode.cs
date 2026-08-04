using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using IC10_Extender.Highlight;
using IC10_Extender.Variables;
using static Assets.Scripts.Objects.Electrical.ScriptCommand;
using static IC10_Extender.Variables.Variables;

namespace IC10_Extender.operations
{
    public class VanillaOpCode
    {
        //redefined to remove ambiguity
        private static readonly Variable<double> Double = Variables.Variables.Double;
        private static readonly Variable<int> LogicType = Variables.Variables.LogicType;

        public static Dictionary<string, Signature> Signatures { get; } = new Dictionary<string, Signature>();

        static VanillaOpCode()
        {
            foreach (ScriptCommand command in Enum.GetValues(typeof(ScriptCommand)))
            {
                var signature = new Signature(Enum.GetName(typeof(ScriptCommand), command), true)
                    .WithStyle(LogicBase.IsDeprecated(command) ? Theme.Default.DeprecatedOpCode : Theme.Default.OpCode);
                switch (command) {
                    case l: signature = signature.Required(Register).Required(Device).Required(LogicType); break;
                    case s: signature = signature.Required(Device).Required(LogicType).Required(Double); break;
                    case ls: signature = signature.Required(Register).Required(Device).Required(Int, LogicType); break;
                    case lr: signature = signature.Required(Register).Required(Device).Required(RegentMode, Int); break;
                    case sb: signature = signature.Required(Device).Required(LogicType).Required(Double); break;
                    case lb: signature = signature.Required(Register).Required(Device).Required(LogicType, LogicBatchMode); break;
                    case alias: signature = signature.Required(Variables.Variables.String).Required(AliasBinding); break;
                    case label: signature = signature.Required(AliasBinding).Required(Variables.Variables.String); break;
                    case define: signature = signature.Required(Variables.Variables.String).Required(Double); break;
                    case sbn: signature = signature.Required(Int, Int, LogicType).Required(Double); break;
                    case lbns: signature = signature.Required(Register).Required(Int, Int, Int, LogicType, LogicBatchMode); break;
                    case ss: signature = signature.Required(Device).Required(Int, LogicType).Required(Double); break;
                    case sbs: signature = signature.Required(Int, Int, LogicType).Required(Double); break;
                    case ld: signature = signature.Required(Register).Required(Int, LogicType); break;
                    case sd: signature = signature.Required(Int, LogicType).Required(Double); break;
                    case poke: signature = signature.Required(Int).Required(Double); break;
                    case getd: signature = signature.Required(Register).Required(Int, Int); break;
                    case putd: signature = signature.Required(Double).Required(Int, Int); break;
                    case get: signature = signature.Required(Register).Required(Device).Required(Int); break;
                    case put: signature = signature.Required(Double).Required(Device).Required(Int); break;
                    case clr: signature = signature.Required(Device); break;
                    case clrd: signature = signature.Required(Int); break;
                    case rmap: signature = signature.Required(Register).Required(Device).Required(Double); break;
                    
                    case bdnvl: case bdnvs:
                    signature = signature.Required(Device).Required(LogicType).Required(Double); break;


                    case lbs: case lbn: 
                    signature = signature.Required(Register).Required(Int, Int, LogicType, LogicBatchMode); break;

                    case yield: case hcf:
                    break;

                    case push: case sleep:
                    signature = signature.Required(Double); break;

                    case rand:
                    case peek: case pop:
                    signature = signature.Required(Register); break;

                    case sdse: case sdns: 
                    signature = signature.Required(Register).Required(Device); break;
                    
                    case move:
                    case sqrt: case round: case trunc: case ceil: case floor:
                    case abs: case log: case exp:
                    case sltz: case sgtz: case slez: case sgez: case seqz: case snez:
                    case sin: case asin: case tan: case atan: case cos: case acos:
                    case snan: case snanz:
                    case ScriptCommand.not:
                    signature = signature.Required(Register).Required(Double); break;

                    case add: case sub: case mul: case div: case mod: case pow:
                    case slt: case sgt: case sle: case sge: case seq: case sne:
                    case and: case or: case xor: case nor:
                    case max: case min:
                    case sapz: case snaz:
                    case atan2:
                    case srl: case sra: case sll:
                    signature = signature.Required(Register).Required(Double, Double); break;

                    case sap: case sna:
                    case select:
                    case ext: case ins: case lerp:
                    signature = signature.Required(Register).Required(Double, Double, Double); break;
                    
                    case j: case jal:
                    case jr:
                    signature = signature.Required(LineNumber); break;

                    case bdse: case bdns: case brdse: case brdns:
                    case bdseal: case bdnsal:
                    signature = signature.Required(Device).Required(LineNumber); break;
                    
                    case bltz: case bgez: case blez: case bgtz: 
                    case bltzal: case bgezal: case blezal: case bgtzal:
                    case brltz: case brgez: case brlez: case brgtz:
                    case beqz: case bnez:
                    case breqz: case brnez:
                    case brnan: case bnan:
                    signature = signature.Required(Double).Required(LineNumber); break;
                    
                    case beq: case bne:
                    case beqal: case bneal:
                    case breq: case brne:
                    case blt: case bgt: case ble: case bge:
                    case brlt: case brgt: case brle: case brge:
                    case bltal: case bgtal: case bleal: case bgeal:
                    case bapz: case bnaz:
                    case brapz: case brnaz:
                    case beqzal: case bnezal:
                    case bapzal: case bnazal:
                    signature = signature.Required(Double, Double).Required(LineNumber); break;

                    case bap: case bna:
                    case brap: case brna:
                    case bapal: case bnaal:
                    signature = signature.Required(Double, Double, Double).Required(LineNumber); break;

                    default:
                    signature = new UnknownSignature(command); break;
                }
                Signatures.Add(signature.Name, signature.Lock());
            }
        }
    }
}
