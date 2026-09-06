using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class FileStruct
    {
        private Guid FileID;
        private string FileName;
        private Guid SharedBy; 
        // NEED TO ADD FILE BYTE STREAM OR SOMETHING IDK, ASK JON
        public FileStruct(string name, Guid sharedby)
        {
            FileID = Guid.NewGuid();
            FileName = name;
            SharedBy = sharedby;
        }
        public Guid GetFileID() { return FileID; }
        public string GetFileName() { return FileName; }
        public Guid GetSharedBy() { return SharedBy; }

    }
}
