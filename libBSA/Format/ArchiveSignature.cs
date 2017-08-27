using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA.Format
{
    public enum ArchiveSignature : UInt32
    {
        Unknown,
        Morrowind = 0x00000100,
        Oblivion = 0x00415342,
        Fallout4 = 0x58445442
    }
}
