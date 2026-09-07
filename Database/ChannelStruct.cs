using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Database
{
    internal class ChannelStruct
    {
        private List<FileStruct> FileList;
        private BlockingCollection<string> newMessages;
        public ChannelStruct()
        {
            FileList = new List<FileStruct>();
            newMessages = new BlockingCollection<string>();
        }

        public List<FileStruct> GetFile() { return FileList; }
        
        public void AddNewMessage(string message)
        {
            newMessages.Add(message);
        }
        public string GetNewMessage()
        {
            return newMessages.Take();
        }

        public void AddFile(string name, Guid sharedby)
        {
            FileStruct file = new FileStruct(name, sharedby);
            FileList.Add(file);
        }
    }
}
