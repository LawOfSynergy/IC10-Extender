using Assets.Scripts.Objects.Electrical;
using IC10_Extender.Operations;
using IC10_Extender.Wrappers;
using System.Collections.Generic;
using System.Linq;

using static Assets.Scripts.Objects.Thing;

namespace IC10_Extender
{
    public class ChipWrapper
    {
        public readonly ProgrammableChip chip;
        public PreExecute PreExecute = IC10Extender.NoOpPreExecute;
        public PostExecute PostExecute = IC10Extender.NoOpPostExecute;
        public OnDelete OnDelete = IC10Extender.NoOpOnDelete;

        private readonly Event chipDestroyHandler;

        private static readonly Dictionary<ProgrammableChip, ChipWrapper> wrappers = new Dictionary<ProgrammableChip, ChipWrapper>();

        /**
         * internal to restrict invalidation/recreation to this framework only
         */
        internal static ChipWrapper For(ProgrammableChip chip, bool forceNew = false)
        {
            if (wrappers.TryGetValue(chip, out var wrapper))
            {
                if (forceNew)
                {
                    wrapper.Delete();
                    wrappers[chip] = new ChipWrapper(chip);
                }

            } else {
                wrappers[chip] = new ChipWrapper(chip);
            }
            return wrappers[chip];
        }

        /**
         * public overload that does not force recreation
         */
        public static ChipWrapper For(ProgrammableChip chip)
        {
            return For(chip, false);
        }

        internal static void Remove(ProgrammableChip chip)
        {
            if (wrappers.ContainsKey(chip))
            {
                wrappers[chip].Delete();
                wrappers.Remove(chip);
            }
        }

        private ChipWrapper(ProgrammableChip chip)
        {
            this.chip = chip;
            chipDestroyHandler = () => { Remove(chip); };
            chip.OnDestroyed += chipDestroyHandler;
        }

        private void Delete()
        {
            var onDelete = IC10Extender.OnDelete + OnDelete;
            onDelete(this);
            chip.OnDestroyed -= chipDestroyHandler;
        }

        public static List<IScriptEnum> InternalEnums
        {
            get => ProgrammableChip.InternalEnums;
            set => ProgrammableChip.InternalEnums = value;
        }

        public ICircuitHolder CircuitHousing => chip.CircuitHousing;

        public double[] Stack => chip._Stack;
        public double[] Registers => chip._Registers;
        public int StackPointerIndex => chip._StackPointerIndex;
        public int ReturnAddressIndex => chip._ReturnAddressIndex;

        /// <summary>
        /// this one returns a copy of the backing dictionary since some translation needs to occur between ProgrammableChip._AliasValue and AliasValue.
        /// If you want to modify this list: get the list, make your changes, then set it to the modified list and the backing list will be updated to reflect
        /// the changes.
        /// </summary>
        public Dictionary<string, AliasValue> Aliases
        {
            get
            {
                Dictionary<string, AliasValue> rv = new Dictionary<string, AliasValue>();
                foreach (var entry in chip._Aliases)
                {
                    rv.Add(entry.Key, entry.Value);
                }
                return rv;
            }
            set
            {
                chip._Aliases.Clear();
                foreach (var entry in value)
                {
                    chip._Aliases[entry.Key] = entry.Value;
                }
            }
        }

        public Dictionary<string, int> JumpTags => chip._JumpTags;
        public Dictionary<string, double> Defines => chip._Defines;

        public int NextAddr
        {
            get => chip._NextAddr;
            set => chip._NextAddr = value;
        }

        public List<string> LinesOfCode => chip._LinesOfCode.Select(loc => loc.ToString()).ToList();
        public List<ProgrammableChip._Operation> Operations => chip._LinesOfCode.Select(loc => loc.Operation).ToList();
        public List<OpContext> OperationContexts => chip._LinesOfCode.Select(loc => (loc.Operation as OperationWrapper).op as OpContext).ToList();
        public List<Operation> UnwrappedOperations => chip._LinesOfCode.Select(loc => ((loc.Operation as OperationWrapper).op as OpContext).op).ToList();

        private ProgrammableChipException _compileException;
        public ProgrammableChipException CompileException
        {
            get => _compileException;
            set
            {
                _compileException = value;
                if (value != null)
                {
                    chip.CircuitHousing?.RaiseError(1);
                    chip.CompileErrorType = value.ExceptionType;
                    chip.CompileErrorLineNumber = value.LineNumber;
                } else
                {
                    chip.CompileErrorType = ProgrammableChipException.ICExceptionType.None;
                    chip.CompileErrorLineNumber = 0;
                }
            }
        }

        private ProgrammableChipException _runtimeException;
        public ProgrammableChipException RuntimeException
        {
            get => _runtimeException;
            set
            {
                _runtimeException = value;
                if (value != null)
                {
                    chip.CircuitHousing?.RaiseError(1);
                    chip._ErrorType = value.ExceptionType;
                    chip._ErrorLineNumber = value.LineNumber;
                } else
                {
                    chip._ErrorType = ProgrammableChipException.ICExceptionType.None;
                    chip._ErrorLineNumber = 0;
                }
            }
        }

        public void ClearException()
        {
            CircuitHousing?.ClearError();
            CompileException = null;
            RuntimeException = null;
        }
    }
}
