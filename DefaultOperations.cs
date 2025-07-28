using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    public static class DefaultOperations
    {
        public static void Register()
        {
            foreach (var command in EnumCollections.ScriptCommands.Values)
            {
                IC10Extender.Register(new DefaultOpCode(command));
            }
        }
    }

    public class DefaultOpCode : ExtendedOpCode
    {
        private ScriptCommand command;
        private int argCount;
        private string description;
        private HelpString[] args;
        private Func<ChipWrapper, int, string[]> ctor;


        public DefaultOpCode(ScriptCommand command) : base(Enum.GetName(command.GetType(), command), LogicBase.IsDeprecated(command))
        {
            this.command = command;
            description = ProgrammableChip.GetCommandDescription(command);
            var cmd = ProgrammableChip.GetCommandExample(command);
            var tokens = cmd.Split();
            args = tokens.Skip(1).Select(token => new HelpString(token)).ToArray();
        }

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length != argCount)
            {
                throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
            }
        }

        public override HelpString[] Params()
        {
            return args;
        }

        public override string Description()
        {
            return description;
        }

        public override void InitHelpPage(ScriptHelpWindow window)
        {
            if(!Deprecated)
            {
                helpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                helpPage.Setup(command, window.DefaultItemImage);
                window._helpReferences.Add(helpPage);
            }
        }

        public override Operation Create(ChipWrapper wrapper, int lineNumber, string[] source)
        {
            var chip = wrapper.chip;
            switch (command)
            {
                case ScriptCommand.l: return new ProgrammableChip._L_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.s: return new ProgrammableChip._S_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.ls: return new ProgrammableChip._LS_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.lr: return new ProgrammableChip._LR_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.sb: return new ProgrammableChip._SB_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.lb: return new ProgrammableChip._LB_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.alias: return new ProgrammableChip._ALIAS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.move: return new ProgrammableChip._MOVE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.add: return new ProgrammableChip._ADD_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sub: return new ProgrammableChip._SUB_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sdse: return new ProgrammableChip._SDSE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.sdns: return new ProgrammableChip._SDNS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.slt: return new ProgrammableChip._SLT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sgt: return new ProgrammableChip._SGT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sle: return new ProgrammableChip._SLE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sge: return new ProgrammableChip._SGE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.seq: return new ProgrammableChip._SEQ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sne: return new ProgrammableChip._SNE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sap: return new ProgrammableChip._SAP_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.sna: return new ProgrammableChip._SNA_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.and: return new ProgrammableChip._AND_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.or: return new ProgrammableChip._OR_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.xor: return new ProgrammableChip._XOR_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.nor: return new ProgrammableChip._NOR_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.mul: return new ProgrammableChip._MUL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.div: return new ProgrammableChip._DIV_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.mod: return new ProgrammableChip._MOD_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.j: return new ProgrammableChip._J_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.bltz: return new ProgrammableChip._BGEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.blez: return new ProgrammableChip._BLEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bgtz: return new ProgrammableChip._BGTZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bdse: return new ProgrammableChip._BDSE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bdns: return new ProgrammableChip._BDNS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.beq: return new ProgrammableChip._BEQ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bne: return new ProgrammableChip._BNE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bap: return new ProgrammableChip._BAP_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.bna: return new ProgrammableChip._BNA_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.jal: return new ProgrammableChip._JAL_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.brdse: return new ProgrammableChip._BRDSE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brdns: return new ProgrammableChip._BRDNS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bltzal: return new ProgrammableChip._BLTZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bgezal: return new ProgrammableChip._BGEZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.blezal: return new ProgrammableChip._BLEZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bgtzal: return new ProgrammableChip._BGTZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.beqal: return new ProgrammableChip._BEQAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bneal: return new ProgrammableChip._BNEAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.jr: return new ProgrammableChip._JR_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.bdseal: return new ProgrammableChip._BDSEAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bdnsal: return new ProgrammableChip._BDNSAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brltz: return new ProgrammableChip._BRLTZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brgez: return new ProgrammableChip._BRGEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brlez: return new ProgrammableChip._BRLEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brgtz: return new ProgrammableChip._BRGTZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.breq: return new ProgrammableChip._BREQ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brne: return new ProgrammableChip._BRNE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brap: return new ProgrammableChip._BRAP_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.brna: return new ProgrammableChip._BRNA_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.sqrt: return new ProgrammableChip._SQRT_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.round: return new ProgrammableChip._ROUND_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.trunc: return new ProgrammableChip._TRUNC_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.ceil: return new ProgrammableChip._CEIL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.floor: return new ProgrammableChip._FLOOR_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.max: return new ProgrammableChip._MAX_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.min: return new ProgrammableChip._MIN_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.abs: return new ProgrammableChip._ABS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.log: return new ProgrammableChip._LOG_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.exp: return new ProgrammableChip._EXP_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.rand: return new ProgrammableChip._RAND_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.yield: return new ProgrammableChip._YIELD_Operation(chip, lineNumber);
                case ScriptCommand.label: return new ProgrammableChip._LABEL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.peek: return new ProgrammableChip._PEEK_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.push: return new ProgrammableChip._PUSH_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.pop: return new ProgrammableChip._POP_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.hcf: return new ProgrammableChip._HCF_Operation(chip, lineNumber);
                case ScriptCommand.select: return new ProgrammableChip._SELECT_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.blt: return new ProgrammableChip._BLT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bgt: return new ProgrammableChip._BGT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.ble: return new ProgrammableChip._BLE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bge: return new ProgrammableChip._BGE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brlt: return new ProgrammableChip._BRLT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brgt: return new ProgrammableChip._BRGT_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brle: return new ProgrammableChip._BRLE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brge: return new ProgrammableChip._BRGE_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bltal: return new ProgrammableChip._BLTAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bgtal: return new ProgrammableChip._BGTAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bleal: return new ProgrammableChip._BLEAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bgeal: return new ProgrammableChip._BGEAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bapal: return new ProgrammableChip._BAPAL_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.bnaal: return new ProgrammableChip._BNAAL_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.beqz: return new ProgrammableChip._BEQZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bnez: return new ProgrammableChip._BNEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bapz: return new ProgrammableChip._BAPZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bnaz: return new ProgrammableChip._BNAZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.breqz: return new ProgrammableChip._BREQZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brnez: return new ProgrammableChip._BRNEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.brapz: return new ProgrammableChip._BRAPZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brnaz: return new ProgrammableChip._BRNAZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.beqzal: return new ProgrammableChip._BEQZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bnezal: return new ProgrammableChip._BNEZAL_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bapzal: return new ProgrammableChip._BAPZAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.bnazal: return new ProgrammableChip._BNAZAL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sltz: return new ProgrammableChip._SLTZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.sgtz: return new ProgrammableChip._SGTZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.slez: return new ProgrammableChip._SLEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.sgez: return new ProgrammableChip._SGEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.seqz: return new ProgrammableChip._SEQZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.snez: return new ProgrammableChip._SNEZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.sapz: return new ProgrammableChip._SAPZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.snaz: return new ProgrammableChip._SNAZ_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.define: return new ProgrammableChip._DEFINE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.sleep: return new ProgrammableChip._SLEEP_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.sin: return new ProgrammableChip._SIN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.asin: return new ProgrammableChip._ASIN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.tan: return new ProgrammableChip._TAN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.atan: return new ProgrammableChip._ATAN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.cos: return new ProgrammableChip._COS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.acos: return new ProgrammableChip._ACOS_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.atan2: return new ProgrammableChip._ATAN2_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.brnan: return new ProgrammableChip._BRNAN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.bnan: return new ProgrammableChip._BNAN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.snan: return new ProgrammableChip._SNAN_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.snanz: return new ProgrammableChip._SNANZ_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.lbs: return new ProgrammableChip._LBS_Operation(chip, lineNumber, source[1], source[2], source[3], source[4], source[5]);
                case ScriptCommand.lbn: return new ProgrammableChip._LBN_Operation(chip, lineNumber, source[1], source[2], source[3], source[4], source[5]);
                case ScriptCommand.sbn: return new ProgrammableChip._SBN_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.lbns: return new ProgrammableChip._LBNS_Operation(chip, lineNumber, source[1], source[2], source[3], source[4], source[5], source[6]);
                case ScriptCommand.ss: return new ProgrammableChip._SS_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.sbs: return new ProgrammableChip._SBS_Operation(chip, lineNumber, source[1], source[2], source[3], source[4]);
                case ScriptCommand.srl: return new ProgrammableChip._SRL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sra: return new ProgrammableChip._SRA_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sll: return new ProgrammableChip._SLA_SLL_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.not: return new ProgrammableChip._NOT_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.ld: return new ProgrammableChip._LD_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.sd: return new ProgrammableChip._SD_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.poke: return new ProgrammableChip._POKE_Operation(chip, lineNumber, source[1], source[2]);
                case ScriptCommand.getd: return new ProgrammableChip._GETD_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.putd: return new ProgrammableChip._PUTD_Operation(chip, lineNumber, source[3], source[1], source[2]);
                case ScriptCommand.get: return new ProgrammableChip._GET_Operation(chip, lineNumber, source[1], source[2], source[3]);
                case ScriptCommand.put: return new ProgrammableChip._PUT_Operation(chip, lineNumber, source[3], source[1], source[2]);
                case ScriptCommand.clr: return new ProgrammableChip._CLR_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.clrd: return new ProgrammableChip._CLRD_Operation(chip, lineNumber, source[1]);
                case ScriptCommand.rmap: return new ProgrammableChip._RMAP_Operation(chip, lineNumber, source[1], source[2], source[3]);
                default: throw new ProgrammableChipException(ICExceptionType.UnrecognisedInstruction, lineNumber);
            }
        }
    }
}
