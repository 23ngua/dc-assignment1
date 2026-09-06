using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class MessageStruct
    {
        private Guid SenderID;
        private string Message;
        public MessageStruct(Guid id, string msg)
        {
            SenderID = id;
            Message = msg;
        }
        public Guid GetSenderID() { return SenderID; }
        public string GetMessage() { return Message; }
    }
}
