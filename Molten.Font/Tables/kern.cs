﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten.Font
{
    /// <summary>Kerning table (maxp).<para/>
    /// See: https://docs.microsoft.com/en-us/typography/opentype/spec/kern </summary>
    public class Kern : FontTable
    {
        public ushort Version { get; private set; }

        public KerningTable[] Tables { get; private set; }

        internal class Parser : FontTableParser
        {
            public override string TableTag => "kern";

            internal override FontTable Parse(BinaryEndianAgnosticReader reader, TableHeader header, Logger log, FontTableList dependencies)
            {
                /* TODO NOTE: Previous versions of the 'kern' table defined both the version and nTables fields in the header as UInt16 values and not UInt32 values. 
                 * Use of the older format on OS X is discouraged (although AAT can sense an old kerning table and still make correct use of it). 
                 * Microsoft Windows still uses the older format for the 'kern' table and will not recognize the newer one. 
                 * Fonts targeted for OS X only should use the new format; fonts targeted for both OS X and Windows should use the old format.
                 * See: https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kern.html
                 */
                Kern table = new Kern()
                {
                    Version = reader.ReadUInt16(),
                };

                ushort numTables = reader.ReadUInt16();
                table.Tables = new KerningTable[numTables];
                for(int i = 0; i < numTables; i++)
                    table.Tables[i] = new KerningTable(reader, log, header);

                return table;
            }
        }
    }


}