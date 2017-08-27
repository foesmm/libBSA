using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.foesmm.libBSA
{
    public class ExtensionsAttribute : Attribute
    {
        private readonly string[] _extensions;

        public ExtensionsAttribute()
        {
            _extensions = null;
        }

        public ExtensionsAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        public bool IsValidFor(string filename)
        {
            if (_extensions == null) return true;

            foreach (var extension in _extensions)
            {
                return filename.EndsWith(extension);
            }

            return false;
        }
    }
}
