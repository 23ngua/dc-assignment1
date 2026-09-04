using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class FileStruct
    {
        private uint FileID;
        private string FileName;
        private uint SharedBy; 
        public FileStruct(uint id, string name, uint sharedby)
        {
            FileID = id;
            FileName = name;
            SharedBy = sharedby;
        }
        public uint GetFileID() { return FileID; }
        public string GetFileName() { return FileName; }
        public uint GetSharedBy() { return SharedBy; }

    }
}
