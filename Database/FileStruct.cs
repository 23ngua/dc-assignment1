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
        private string SharedBy;
        private string ChannelName;
        private byte[] FileBytes;
        public FileStruct(string name, string sharedBy, string channelName, byte[] fileBytes)
        {
            FileID = Guid.NewGuid();
            FileName = name;
            SharedBy = sharedBy;
            ChannelName = channelName;
            FileBytes = fileBytes;
        }
        public Guid GetFileID() { return FileID; }
        public string GetFileName() { return FileName; }
        public string GetSharedBy() { return SharedBy; }
        public string GetChannelName() { return ChannelName; }
        public byte[] GetFileBytes() { return FileBytes; }

    }
}
