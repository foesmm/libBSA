using org.foesmm.libBSA.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA
{
    class UnsupportedArchiveException : Exception
    {
        public UnsupportedArchiveException(ArchiveSignature signature, ArchiveVersion version) : base(string.Format(
                "Unsupported archive with signature: {0} ({1})",
                Enum.GetName(typeof(ArchiveSignature), signature),
                Enum.GetName(typeof(ArchiveVersion), version)
                )) { }
        public UnsupportedArchiveException(string message) : base(message) { }
    }
}
