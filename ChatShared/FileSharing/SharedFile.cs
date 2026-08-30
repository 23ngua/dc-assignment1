using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChatShared
{
    public class SharedFile
    {
        [DataMember]
        public string FileID { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string SharedBy { get; set; }

        [DataMember]
        public string ChannelName { get; set; }
    }
}
