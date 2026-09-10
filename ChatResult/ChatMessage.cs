using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary
{
    [DataContract]
    public class ChatMessage
    {

        [DataMember]
        public string SenderId { get; set; }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}
