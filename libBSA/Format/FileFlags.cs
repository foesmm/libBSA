using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA.Format
{
    [Flags]
    public enum FileFlags : UInt32
    {
        [Extensions("nif")]
        Meshes = 1 << 0,
        [Extensions("dds")]
        Textures = 1 << 1,
        [Extensions("xml")]
        Menus = 1 << 2,
        [Extensions("wav")]
        Sounds = 1 << 3,
        [Extensions("mp3", "ogg")]
        Voices = 1 << 4,
        [Extensions("txt", "html", "bat", "scc")]
        Shaders = 1 << 5,
        [Extensions("spt")]
        Trees = 1 << 6,
        [Extensions("tex", "fnt")]
        Fonts = 1 << 7,
        Misc = 1 << 8,
        [Extensions("lip", "fuz")]
        Lip = 1 << 9,
        [Extensions("bik")]
        Bik = 1 << 10,
        [Extensions("jpg")]
        Jpg = 1 << 11,
        [Extensions("ogg")]
        Ogg = 1 << 12,
        [Extensions("gid", "pex")]
        GidPex = 1 << 13,
        Flag14 = 1 << 14,
        Flag15 = 1 << 15,
        Flag16 = 1 << 16,
        Flag17 = 1 << 17,
        Flag18 = 1 << 18,
        Flag19 = 1 << 19,
        Flag20 = 1 << 20,
        Flag21 = 1 << 21,
        Flag22 = 1 << 22,
        Flag23 = 1 << 23,
        Flag24 = 1 << 24,
        Flag25 = 1 << 25,
        Flag26 = 1 << 26,
        Flag27 = 1 << 27,
        Flag28 = 1 << 28,
        Flag29 = 1 << 29,
        Flag30 = 1 << 30,
    }
}
