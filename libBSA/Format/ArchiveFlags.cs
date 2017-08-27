using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA.Format
{
    [Flags]
    public enum ArchiveFlags : UInt32
    {
        PathNames = 0x0001,
        FileNames = 0x0002,
        Compressed = 0x0004,
        PrefixFileNames = 0x0100,
        FileInverseCompressed = 0x40000000,
    }
}
