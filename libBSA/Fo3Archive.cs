using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using org.foesmm.libBSA.Format;

namespace org.foesmm.libBSA
{
    public class Fo3Archive : BSA, IBSA
    {
        public Header HeaderData => _header;

        public struct Header
        {
            public UInt32 FolderRecordOffset;
            public BSArchiveFlag ArchiveFlags;
            public UInt32 FolderCount;
            public UInt32 FileCount;
            public UInt32 FolderNameLength;
            public UInt32 FileNameLength;
            public FileFlags FileFlags;

            public static Header Read(BinaryReader reader)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Begin);
                try
                {
                    return new Header()
                    {
                        FolderRecordOffset = reader.ReadUInt32(),
                        ArchiveFlags = (BSArchiveFlag)reader.ReadUInt32(),
                        FolderCount = reader.ReadUInt32(),
                        FileCount = reader.ReadUInt32(),
                        FolderNameLength = reader.ReadUInt32(),
                        FileNameLength = reader.ReadUInt32(),
                        FileFlags = (FileFlags)reader.ReadUInt32(),
                    };
                }
                catch (EndOfStreamException e)
                {
                    return new Header();
                }
            }

            internal void Write(BinaryWriter writer)
            {
                writer.Write(FolderRecordOffset);
                writer.Write((UInt32)ArchiveFlags);
                writer.Write(FolderCount);
                writer.Write(FileCount);
                writer.Write(FolderNameLength);
                writer.Write(FileNameLength);
                writer.Write((UInt32)FileFlags);
            }
        }

        [Flags]
        public enum BSArchiveFlag : UInt32
        {
            PathNames = 0x0001,
            FileNames = 0x0002,
            Compressed = 0x0004,
            PrefixFileNames = 0x0100,
            FileInverseCompressed = 0x40000000,
        }

        protected Header _header;

        public long FolderCount => _header.FolderCount;
        public long FileCount => _header.FileCount;

        protected HashSet<string> Extensions { get; set; }

        public void AddFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(file);
                AddFile(fi.DirectoryName.Remove(0, path.Length + 1), new Fo3File(fi));
            }
        }

        public void AddFile(string path, Fo3File file)
        {
            var folder = _folders.Get(BSAAsset.CalculateHash(path), new Fo3Folder(path));
            file.Path = folder.Name;
            folder.Files.Add(file);
        }

        public HashSet<string> GetAllExtensions()
        {
            var res = new HashSet<string>();

            foreach (var folder in _folders.Values)
            {
                foreach (var file in folder.Files.Values)
                {
                    res.Add(file.Name.Substring(file.Name.LastIndexOf('.') + 1));
                }
            }

            return res;
        }

        public void BuildIndex()
        {
            IndexFullPath = new SortedDictionary<string, Fo3File>();
            IndexFileName = new SortedDictionary<string, Fo3File>();
            foreach (var folder in _folders.Values)
            {
                foreach (var file in folder.Files.Values)
                {
                    file.Path = folder.Name;
                    IndexFullPath.Add(string.Format("{0}\\{1}", folder.Name, file.Name), file);
                    IndexFileName[BitConverter.ToString(file.Checksum)] = file;
                }
            }
        }

        public void RebuildHeader()
        {
            _header = new Header()
            {
                FolderRecordOffset = 36,
                FolderCount = (UInt32)_folders.Count,
                FileCount = (UInt32)_folders.Values.Sum(f => f.Files.Count),
                FolderNameLength = (UInt32)_folders.Values.Sum(f => f.Name.Length + 1),
                FileNameLength = (UInt32)_folders.Values.Sum(f => f.Files.Values.Sum(file => file.Name.Length + 1)),
                ArchiveFlags = GenerateArchiveFlags(),
                FileFlags = GenerateFilesFlag(),
            };
            Flags = (UInt32)_header.ArchiveFlags;
            FileFlags = (UInt32)_header.FileFlags;
        }

        public BSArchiveFlag GenerateArchiveFlags()
        {
            BSArchiveFlag res = 0;

            res |= BSArchiveFlag.FileNames;
            res |= BSArchiveFlag.PathNames;

            if (IsCompressed)
            {
                res |= BSArchiveFlag.Compressed;
            }

            return res;
        }

        public FileFlags GenerateFilesFlag()
        {
            FileFlags res = 0;

            foreach (var ext in Extensions)
            {
                    foreach (FileFlags type in Enum.GetValues(typeof(FileFlags)))
                    {
                        ExtensionsAttribute attribute = type.GetType()
                            .GetField(type.ToString())
                            .GetCustomAttributes(typeof(ExtensionsAttribute), false)
                            .SingleOrDefault() as ExtensionsAttribute;

                        if (attribute != null && attribute.IsValidFor(ext))
                        {
                            res |= type;
                        }
                    }
            }

            if (res == 0)
            {
                res |= Format.FileFlags.Misc;
            }

            return res;
        }

        public bool ContainsPathNames => (_header.ArchiveFlags & BSArchiveFlag.PathNames) == BSArchiveFlag.PathNames;

        public bool ContainsFileNames => (_header.ArchiveFlags & BSArchiveFlag.FileNames) == BSArchiveFlag.FileNames;

        [XmlIgnore]
        public bool IsCompressed
        {
            get => (_header.ArchiveFlags & BSArchiveFlag.Compressed) == BSArchiveFlag.Compressed;
            set
            {
                if (value)
                    _header.ArchiveFlags |= BSArchiveFlag.Compressed;
                else
                    _header.ArchiveFlags &= ~BSArchiveFlag.Compressed;
            }
        }

        [XmlIgnore]
        public bool IsNamePrefixedToData
        {
            get => (_header.ArchiveFlags & BSArchiveFlag.PrefixFileNames) == BSArchiveFlag.PrefixFileNames;
            set
            {
                if (value)
                    _header.ArchiveFlags |= BSArchiveFlag.PrefixFileNames;
                else
                    _header.ArchiveFlags &= ~BSArchiveFlag.PrefixFileNames;
            }
        }

        public BSArchiveFlag ArchiveFlags => _header.ArchiveFlags;

        public Fo3Archive() : base()
        {

        }

        public Fo3Archive(string filename) : base(filename)
        {
            _header = Header.Read(Reader);
            Flags = (UInt32)_header.ArchiveFlags;
            FileFlags = (UInt32)_header.FileFlags;

            Extensions = new HashSet<string>();

            if (File.Exists(Filename + ".xml"))
            {
                return;
            }

            UInt32 fileRecordsSize = _header.FolderCount + _header.FolderNameLength + 16 * _header.FileCount;

            var stopwatch = Stopwatch.StartNew();
            var folders = new List<Fo3Folder>();
            for (int folderIdx = 0; folderIdx < _header.FolderCount; folderIdx++)
            {
                var folder = new Fo3Folder();

                folder.NameHash = Reader.ReadUInt64();
                folder.Files.Capacity = Reader.ReadUInt32();
                folder.Offset = Reader.ReadUInt32();
                folders.Add(folder);
            }
            stopwatch.Stop();
            Debug.WriteLine("Processed folders in {0}", stopwatch.Elapsed);

            byte[] fileRecords = new byte[fileRecordsSize];
            Reader.Read(fileRecords, 0, fileRecords.Length);
            var fileRecordsStream = new BinaryReader(new MemoryStream(fileRecords));

            byte[] fileNamesBuffer = new byte[_header.FileNameLength];
            Reader.Read(fileNamesBuffer, 0, fileNamesBuffer.Length);
            string[] fileNames = Encoding.UTF8.GetString(fileNamesBuffer).Split('\0');

            int fileNameListPos = 0;
            UInt32 folderRecordOffsetBaseline = 36
                + 16 * _header.FolderCount
                + _header.FileNameLength;

            stopwatch = Stopwatch.StartNew();
            foreach (Fo3Folder folder in folders)
            {
                folder.Offset -= folderRecordOffsetBaseline;
                folder.Name = GetFolderName(fileRecords, (int)folder.Offset);
                Folders.Add(folder);

                long startOfFolderFileRecords = folder.Offset + folder.Name.Length + 2;

                for (UInt32 i = 0; i < folder.FileCount; i++)
                {
                    long fileRecordOffset = startOfFolderFileRecords + i * 16;
                    fileRecordsStream.BaseStream.Seek(fileRecordOffset, SeekOrigin.Begin);
                    var file = new Fo3File()
                    {
                        Reader = Reader,
                        NameHash = fileRecordsStream.ReadUInt64(),
                        Size = fileRecordsStream.ReadUInt32(),
                        Offset = fileRecordsStream.ReadUInt32(),
                        ArchiveCompressed = IsCompressed,
                        Prefixed = IsNamePrefixedToData,
                    };

                    file.Name = fileNames[fileNameListPos++];
                    file.Checksum = MD5.Create().ComputeHash(file.GetData());
                    Extensions.Add(file.Name.Substring(file.Name.LastIndexOf('.') + 1));
                    file.Path = folder.Name;
                    folder.Files.Add(file);
                }
            }
            stopwatch.Stop();
            Debug.WriteLine("Enumerating all files ({0}) and folders took: {1}", _header.FileCount, stopwatch.Elapsed);

            WriteDescriptor();

            Debug.WriteLine("Break");
        }

        public string GetFolderName(byte[] buffer, int offset)
        {
            int length = buffer[offset];
            return Encoding.UTF8.GetString(buffer, offset + 1, length - 1);
        }

        public void Save()
        {
            var writer = new BinaryWriter(Reader.BaseStream);
            writer.BaseStream.Seek(0, SeekOrigin.Begin);
            writer.Write((UInt32)Signature);
            writer.Write((UInt32)Version);
            _header.Write(writer);

            UInt32 fileRecordBlocksSize = _header.FolderCount + _header.FolderNameLength + _header.FileCount * 16;
            byte[] fileRecordBlocks = new byte[fileRecordBlocksSize];
            var fileRecordBlocksWriter = new BinaryWriter(new MemoryStream(fileRecordBlocks));
            byte[] fileNames = new byte[_header.FileNameLength];
            var fileNamesWriter = new BinaryWriter(new MemoryStream(fileNames));
            UInt32 startOfFileRecordBlock = 36 + _header.FolderCount * 16 + _header.FileNameLength;
            UInt32 fileDataOffset = startOfFileRecordBlock + fileRecordBlocksSize;

            UInt32 currFileRecordBlockPos = 0;
            writer.Flush();
            foreach (var folder in Folders.Values.OrderBy(f => f.NameHash))
            {
                folder.Offset = currFileRecordBlockPos;
                writer.Write(folder.NameHash);
                writer.Write(folder.Files.Count);
                writer.Write((UInt32)(startOfFileRecordBlock + currFileRecordBlockPos));

                fileRecordBlocksWriter.Write((byte)(folder.Name.Length + 1));
                fileRecordBlocksWriter.Write(folder.Name.ToCharArray());
                fileRecordBlocksWriter.Write('\0');
                currFileRecordBlockPos += (UInt32)folder.Name.Length + 2;

                foreach (Fo3File file in folder.Files.Values.OrderBy(f => f.NameHash))
                {
                    fileRecordBlocksWriter.Write(file.NameHash);
                    if (IsNamePrefixedToData)
                    {
                        string fullName = string.Format("{0}\\{1}", folder.Name, file.Name);
                        file.Size += (UInt32)(fullName.Length + 1);
                    }
                    fileRecordBlocksWriter.Write(file.Size);
                    file.Offset = fileDataOffset;
                    fileRecordBlocksWriter.Write(file.Offset);
                    currFileRecordBlockPos += 16;
                    fileDataOffset += file.Size;

                    fileNamesWriter.Write(file.Name.ToCharArray());
                    fileNamesWriter.Write('\0');
                }
            }

            fileRecordBlocksWriter.Close();
            fileNamesWriter.Close();

            writer.Write(fileRecordBlocks);
            writer.Write(fileNames);

            foreach (var folder in Folders.Values.OrderBy(f => f.NameHash))
            {
                foreach (Fo3File file in folder.Files.Values.OrderBy(f => f.NameHash))
                {
                    writer.Seek((int)file.Offset, SeekOrigin.Begin);
                    if (IsNamePrefixedToData)
                    {
                        string fullName = string.Format("{0}\\{1}", folder.Name, file.Name);
                        writer.Write((byte)(fullName.Length));
                        writer.Write(fullName.ToCharArray());
                    }
                    writer.Write(file.GetData());
                }
            }

            writer.Flush();
            RecalculateChecksum();

            writer.Close();

            WriteDescriptor();
        }
    }
}
