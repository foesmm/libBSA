using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA.Format
{
    public enum ArchiveVersion : UInt32
    {
        Unknown,
        Oblivion = 0x67,
        Fallout3 = 0x68,
        SkyrimSE = 0x69,
        Fallout4 = 0x01
    }
}
