﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten.Font
{
    public class ChainRuleSetTable : FontSubTable
    {
        /// <summary>
        /// Gets an array of PosRule tables, ordered by preference.
        /// </summary>
        public ChainRuleTable[] Tables { get; internal set; }

        internal ChainRuleSetTable(BinaryEndianAgnosticReader reader, Logger log, IFontTable parent, long offset) :
            base(reader, log, parent, offset)
        {
            ushort posRuleCount = reader.ReadUInt16();
            ushort[] posRuleOffsets = reader.ReadArrayUInt16(posRuleCount);
            Tables = new ChainRuleTable[posRuleCount];
            for (int i = 0; i < posRuleCount; i++)
                Tables[i] = new ChainRuleTable(reader, log, this, posRuleOffsets[i]);
        }
    }

    /// <summary>
    /// See for ChainPosRule table: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#chaining-context-positioning-format-2-class-based-glyph-contexts
    /// See for ChainSubRule table: https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chaining-context-substitution-format-2-class-based-glyph-contexts
    /// </summary>
    public class ChainRuleTable : FontSubTable
    {
        /// <summary>
        /// Gets an array of gylph or class IDs (depending on format) for a backtrack sequence.
        /// </summary>
        public ushort[] BacktrackSequence { get; internal set; }

        /// <summary>
        /// Gets an array of glyph or class IDs (depending on format) for an input sequence.
        /// </summary>
        public ushort[] InputSequence { get; internal set; }

        /// <summary>
        /// Gets an array of glyph or class IDs (depending on format) for a look ahead sequence.
        /// </summary>
        public ushort[] LookAheadSequence { get; internal set; }

        /// <summary>
        /// Gets an array of positioning lookups, in design order.
        /// </summary>
        public RuleLookupRecord[] Records { get; internal set; }

        internal ChainRuleTable(BinaryEndianAgnosticReader reader, Logger log, IFontTable parent, long offset) :
            base(reader, log, parent, offset)
        {
            ushort backtrackGlyphCount = reader.ReadUInt16();
            BacktrackSequence = reader.ReadArrayUInt16(backtrackGlyphCount);

            ushort inputGlyphCount = reader.ReadUInt16();
            InputSequence = reader.ReadArrayUInt16(inputGlyphCount - 1);

            ushort lookAheadGlyphCount = reader.ReadUInt16();
            LookAheadSequence = reader.ReadArrayUInt16(lookAheadGlyphCount);

            ushort posCount = reader.ReadUInt16();
            Records = new RuleLookupRecord[posCount];
            for(int i = 0; i < posCount; i++)
            {
                Records[i] = new RuleLookupRecord()
                {
                    SequenceIndex = reader.ReadUInt16(),
                    LookupListIndex = reader.ReadUInt16(),
                };
            }
        }
    }
}