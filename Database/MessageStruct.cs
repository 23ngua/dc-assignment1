using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class MessageStruct
    {
        private uint SenderID;
        private string Message;
        public MessageStruct(uint id, string msg)
        {
            SenderID = id;
            Message = msg;
        }
        public uint GetSenderID() { return SenderID; }
        public string GetMessage() { return Message; }
    }
}
