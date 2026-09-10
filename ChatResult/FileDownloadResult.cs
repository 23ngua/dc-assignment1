using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary
{
    [DataContract]
    public class FileDownloadResult
    {
        [DataMember] 
        public bool Success { get; set; }

        [DataMember] 
        public string Message { get; set; }

        [DataMember] 
        public string FileName { get; set; }

        [DataMember] 
        public byte[] FileBytes { get; set; }
    }
}
