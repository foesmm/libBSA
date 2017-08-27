using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Serialization;
using org.foesmm.libBSA.Format;
using System.Xml;
using System.Runtime.Serialization;

namespace org.foesmm.libBSA
{
    [Serializable]
    [XmlInclude(typeof(Fo3Archive))]
    [XmlRoot("BSA")]
    public abstract class BSA : IDisposable
    {
        public enum Game
        {
            Unknown,
            Morrowind,
            Oblivion,
            Fallout3,
            FalloutNewVegas,
            Skyrim,
            SkyrimSE,
            Fallout4
        }

        [XmlIgnore]
        public string Filename { get; set; }
        [XmlAttribute("signature")]
        public ArchiveSignature Signature { get; set; }
        [XmlAttribute("version")]
        public ArchiveVersion Version { get; set; }
        [XmlAttribute("flags")]
        public UInt32 Flags { get; set; }
        [XmlAttribute("fileflags")]
        public UInt32 FileFlags { get; set; }
        [XmlElement("Assets")]
        public BSAAssets<Fo3Folder> Folders { get { return _folders; } set { _folders = value; } }
        [XmlIgnore]
        public SortedDictionary<string, Fo3File> IndexFullPath { get; set; }
        [XmlIgnore]
        public SortedDictionary<string, Fo3File> IndexFileName { get; set; }
        [XmlAttribute("checksum", DataType = "hexBinary")]
        public byte[] Checksum { get; set; }
        [XmlIgnore]
        public bool IsModified { get; set; }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }

        public void SetStream(Stream stream)
        {
            Reader = new BinaryReader(stream);
        }

        [XmlIgnore]
        public BinaryReader Reader { get; set; }
        protected BSAAssets<Fo3Folder> _folders;

        public static IBSA Open(string filename)
        {
            var stream = new BufferedStream(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite), 8 * 1024 * 1024);

            var checksumStopwatch = Stopwatch.StartNew();

            byte[] checksum = MD5.Create().ComputeHash(stream);
            checksumStopwatch.Stop();
            Debug.WriteLine(
                "MD5 checksum {0}\nTook {1}",
                BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower(),
                checksumStopwatch.Elapsed
                );
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(stream);
            var signature = (ArchiveSignature)reader.ReadUInt32();
            var version = (ArchiveVersion)reader.ReadUInt32();

            if (File.Exists(filename + ".xml"))
            {
                IBSA archive = null;
                using (var xmlReader = new StreamReader(string.Format("{0}.xml", filename)))
                {
                    var serializer = new XmlSerializer(typeof(BSA));
                    try
                    {
                        archive = (IBSA)serializer.Deserialize(xmlReader);
                        archive.Filename = filename;
                        archive.SetStream(stream);
                    } catch (InvalidOperationException ioe)
                    {
                        
                    }
                }
                if (archive != null && ByteArrayCompare(checksum, archive.Checksum))
                {
                    return archive;
                }
                else
                {
                    Debug.WriteLine("Contents Changed, need recalculate");
                }
            }
            stream.Close();

            if (signature == ArchiveSignature.Oblivion && version == ArchiveVersion.Fallout3)
            {
                return new Fo3Archive(filename);
            }
            else
            {
                throw new UnsupportedArchiveException(signature, version);
            }
        }

        public static IBSA Create(Game version, string filename, string looseFilesDirectory)
        {
            switch (version)
            {
                case Game.FalloutNewVegas:
                    var archive = new Fo3Archive(filename);
                    archive.Signature = ArchiveSignature.Oblivion;
                    archive.Version = ArchiveVersion.Fallout3;
                    archive.AddFiles(looseFilesDirectory);
                    archive.RebuildHeader();
                    archive.WriteDescriptor();

                    return archive;
                default:
                    throw new UnsupportedArchiveException(string.Format("Archive creation for {0} is not supported", Enum.GetName(version.GetType(), version)));
            }
        }

        public void Dispose()
        {
            Reader.Close();
        }

        public BSA()
        {
            _folders = new BSAAssets<Fo3Folder>();
        }

        public BSA(string filename) : this()
        {
            Filename = filename;
            var stream = new BufferedStream(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite), 8 * 1024 * 1024);
            Reader = new BinaryReader(stream);

            RecalculateChecksum();
            try
            {
                Signature = (ArchiveSignature)Reader.ReadUInt32();
                Version = (ArchiveVersion)Reader.ReadUInt32();
            }
            catch (Exception e)
            {

            }
        }

        public void RecalculateChecksum()
        {
            Reader.BaseStream.Seek(0, SeekOrigin.Begin);
            var checksumStopwatch = Stopwatch.StartNew();
            Checksum = MD5.Create().ComputeHash(Reader.BaseStream);
            checksumStopwatch.Stop();
            Debug.WriteLine(
                "MD5 checksum {0}\nTook {1}",
                BitConverter.ToString(Checksum).Replace("-", string.Empty).ToLower(),
                checksumStopwatch.Elapsed
                );
            Reader.BaseStream.Seek(0, SeekOrigin.Begin);
        }

        public void WriteDescriptor()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineHandling = NewLineHandling.Entitize;
            settings.CloseOutput = true;

            using (var writer = XmlWriter.Create(new StreamWriter(string.Format("{0}.xml", Filename)), settings))
            {
                var serializer = new XmlSerializer(typeof(BSA));
                serializer.Serialize(writer, this);
                writer.Flush();
                writer.Close();
            }
        }
    }
}
