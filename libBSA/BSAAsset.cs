using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;

namespace org.foesmm.libBSA
{
    [DebuggerDisplay("{GetType()} Name = {Name}, NameHash = {NameHash}")]
    public abstract class BSAAsset
    {
        [XmlAttribute("namehash")]
        public UInt64 NameHash { get; set; }
        protected string _name;
        [XmlAttribute("name")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (NameHash == 0)
                {
                    if (this is Fo3Folder)
                    {
                        NameHash = CalculateHash(value, null);
                    } else
                    {
                        int index = value.LastIndexOf('.');
                        string path = value;
                        string ext = null;
                        if (index > -1)
                        {
                            ext = path.Substring(index);
                            path = path.Remove(index);
                        }
                        NameHash = CalculateHash(path, ext);
                    }
                }
            }
        }

        public static UInt64 CalculateHash(string path)
        {
            return CalculateHash(path, null);
        }

        public static UInt64 CalculateHash(string path, string ext)
        {
            UInt64 hash1 = 0;
            UInt32 hash2 = 0;
            UInt32 hash3 = 0;

            if (!string.IsNullOrEmpty(path))
            {
                hash1 = (UInt64)((byte)path[path.Length - 1]
                    + (path.Length << 16)
                    + ((byte)path[0] << 24)
                    );
                if (path.Length > 2)
                {
                    hash1 += (UInt64)((byte)path[path.Length - 2] << 8);

                    if (path.Length > 3)
                    {
                        hash2 = HashString(path.Substring(1, path.Length - 3));
                    }
                }
            }

            if (!string.IsNullOrEmpty(ext))
            {
                if (ext.Equals(".kf"))
                {
                    hash1 += 0x80;
                }
                else if (ext.Equals(".nif"))
                {
                    hash1 += 0x8000;
                }
                else if (ext.Equals(".dds"))
                {
                    hash1 += 0x8080;
                }
                else if (ext.Equals(".wav"))
                {
                    hash1 += 0x80000000;
                }

                hash3 = HashString(ext);
            }

            hash2 += hash3;
            hash1 += ((UInt64)hash2 << 32);

            return hash1;
        }

        public static UInt32 HashString(string str)
        {
            UInt32 hash = 0;
            for (int i = 0, len = str.Length; i < len; i++)
            {
                hash = 0x1003F * hash + (byte)str[i];
            }
            return hash;
        }
    }
}
