using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class Channel
    {
        private string ChannelName;
        private Guid ChannelID;
        private List<MessageStruct> MessageList;
        private List<FileStruct> FileList;
        private bool IsPrivateChannel;
        private List<uint> Members;

        public Channel(bool isPriv, string name)
        {
            IsPrivateChannel = true;
            ChannelName = name;
            ChannelID = Guid.NewGuid();
            MessageList = new List<MessageStruct>();
            FileList = new List<FileStruct>();
            Members = new List<uint>(2);
        }
        public Channel(string name)
        {
            IsPrivateChannel = false;
            ChannelName = name;
            ChannelID = Guid.NewGuid();
            MessageList = new List<MessageStruct>();
            FileList = new List<FileStruct>();
            Members = new List<uint>();
        }
        // Add Message
        // Add Member
        // Add File
        public bool isPrivateChannel() { return IsPrivateChannel; }
        public string GetChannelName() { return ChannelName; }
        public Guid GetChannelID() { return ChannelID; }
        public List<MessageStruct> GetMessageList() { return MessageList; }
        public List<FileStruct> GetFile() { return FileList; }
        public List<uint> GetMembers() { return Members; }
    }
}
