using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChatShared.FileSharing
{
    [DataContract]
    public class SharedFileInfo
    {
        [DataMember] 
        public string FileID { get; set; } // unique id from server

        [DataMember] 
        public string FileName { get; set; }

        [DataMember] 
        public string SharedBy { get; set; }

        [DataMember] 
        public string ChannelName { get; set; }
    }
}
