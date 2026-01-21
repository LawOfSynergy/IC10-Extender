namespace IC10_Extender
{
    public struct Line
    {
        /**
         * The string on which preprocessing is performed. Changes to this string are not inherently reflected in Display, which must be updated separately if desired. 
         * This string explicitly contains the results of the current round of preprocessing. If the chip gets recompiled or reloaded, this string will be discarded and 
         * replaced with the contents of Display as it was saved in the previous compilation, and preprocessing will start over from there.
         */
        public string Raw;

        /**
         * The string as it should be stored in the chip and displayed in the UI. The text in this field is what will be used any time the chip's code is reloaded or recompiled.
         */
        public string Display;

        /**
         * The original line number from the source code before any preprocessing or line removal took place. This is used for error reporting.
         */
        public readonly ushort OriginatingLineNumber;

        /**
         * The current line number after preprocessing and line removal. This is used for jumps and line references.
         */
        public ushort LineNumber;

        /**
         * If this field is non-null, it indicates that this line should be treated as if it were of the specified operation type, regardless of what the Raw string indicates after preprocessing.
         */
        public Operation ForcedOp;

        /**
         * A delegate that is called before the line is executed. This can be used to modify the chip's state or perform other actions.
         */
        public PreExecute PreExecute;

        /**
         * A delegate that is called after the line is executed. This can be used to modify the chip's state or perform other actions.
         */
        public PostExecute PostExecute;

        public Line(string raw, ushort originatingLineNumber, Operation forcedOp = null)
        {
            Raw = raw;
            Display = raw;
            OriginatingLineNumber = originatingLineNumber;
            LineNumber = originatingLineNumber;
            ForcedOp = forcedOp;
            PreExecute = IC10Extender.NoOpPreExecute;
            PostExecute = IC10Extender.NoOpPostExecute;
        }

        public new string ToString()
        {
            return ToDisplayString();
        }

        public string ToDisplayString()
        {
            return $"{OriginatingLineNumber}: {Display}";
        }

        public string ToRawString()
        {
            return $"{OriginatingLineNumber}: {Raw}";
        }
    }
}
