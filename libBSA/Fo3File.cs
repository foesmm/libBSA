using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Ionic.Zlib;
using System.Xml.Serialization;

namespace org.foesmm.libBSA
{
    [Serializable]
    [XmlRoot("File")]
    public class Fo3File : BSAAsset
    {
        protected UInt32 _size;
        [XmlAttribute("size")]
        public UInt32 Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (((Fo3Archive.BSArchiveFlag)value & Fo3Archive.BSArchiveFlag.FileInverseCompressed) == Fo3Archive.BSArchiveFlag.FileInverseCompressed)
                {
                    value = (UInt32)((Fo3Archive.BSArchiveFlag)value ^ Fo3Archive.BSArchiveFlag.FileInverseCompressed);
                    _invertCompression = true;
                }
                _size = value;
            }
        }
        [XmlAttribute("offset")]
        public UInt32 Offset;

        [XmlIgnore]
        public FileInfo File { get; private set; }
        [XmlIgnore]
        public bool Prefixed;
        [XmlIgnore]
        public bool ArchiveCompressed;
        [XmlIgnore]
        public string Path { get; set; }
        private bool _invertCompression;

        public bool Compressed => ArchiveCompressed ^ _invertCompression;

        [XmlIgnore]
        public BinaryReader Reader;
        [XmlAttribute("checksum", DataType = "hexBinary")]
        public byte[] Checksum { get; set; }

        public byte[] GetData()
        {
            return GetData(Reader);
        }

        public byte[] GetData(BinaryReader reader)
        {
            if (File != null)
            {
                return System.IO.File.ReadAllBytes(File.FullName);
            }

            byte[] buffer = new byte[Size];
            long outSize = buffer.Length;

            reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            if (Prefixed)
            {
                var len = reader.ReadByte();
                outSize -= len + 1;
                reader.BaseStream.Seek(len, SeekOrigin.Current);
            }
            if (Compressed)
            {
                outSize = reader.ReadUInt32();
                buffer = new byte[buffer.Length - 4];
            }
            reader.Read(buffer, 0, buffer.Length);

            if (Compressed)
            {
                using (var compressedStream = new MemoryStream(buffer))
                {
                    using (var stream = new ZlibStream(compressedStream, CompressionMode.Decompress))
                    {
                        var outBuffer = new byte[outSize];
                        stream.Read(outBuffer, 0, outBuffer.Length);
                        return outBuffer;
                    }
                }
            }

            return buffer;
        }

        public Fo3File()
        {

        }

        public Fo3File(FileInfo file) : this()
        {
            File = file;
            Name = file.Name.ToLower();
            Size = (UInt32)file.Length;
            using (var stream = new BufferedStream(file.OpenRead(), 8 * 1024 * 1024)) {
                Checksum = MD5.Create().ComputeHash(stream);
            }
        }
    }
}
