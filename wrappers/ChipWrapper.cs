using Assets.Scripts.Objects.Electrical;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        static void Remove(ProgrammableChip chip)
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

        public double[] Registers => chip._Registers;
        public int StackPointerIndex => chip._StackPointerIndex;
        public int ReturnAddressIndex => chip._ReturnAddressIndex;
        public double[] Stack => chip._Stack;

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

        public int ExecuteIndex
        {
            get => chip._executeIndex;
            set => chip._executeIndex = value;
        }

        public ushort ErrorLineNumber
        {
            get => chip._ErrorLineNumber;
            set => chip._ErrorLineNumber = value;
        }

        public string ErrorTypeString => chip.ErrorTypeString;

        public ProgrammableChipException.ICExceptionType ErrorType
        {
            get => chip._ErrorType;
            set => chip._ErrorType = value;
        }

        public bool CompilationError => chip.CompilationError;

        public ushort CompileErrorLineNumber
        {
            get => chip.CompileErrorLineNumber;
            set => chip.CompileErrorLineNumber = value;
        }

        public ProgrammableChipException.ICExceptionType CompileErrorType
        {
            get => chip.CompileErrorType;
            set => chip.CompileErrorType = value;
        }

        public ICircuitHolder CircuitHousing => chip.CircuitHousing;
    }
}
