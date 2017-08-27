using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace org.foesmm.libBSA
{
    [Serializable]
    [XmlRoot("Folder")]
    public class Fo3Folder : BSAAsset
    {
        [XmlElement("Files")]
        public BSAAssets<Fo3File> Files { get; set; }
        public UInt32 FileCount => Files.Capacity;
        [XmlAttribute("offset")]
        public UInt32 Offset;


        public Fo3Folder()
        {
            Files = new BSAAssets<Fo3File>();
        }

        public Fo3Folder(string path) : this()
        {
            Name = path;
        }
    }
}
